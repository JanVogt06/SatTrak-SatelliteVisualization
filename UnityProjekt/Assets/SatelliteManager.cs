using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using CesiumForUnity;
using Newtonsoft.Json;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using Unity.Mathematics;

public class SatelliteManager : MonoBehaviour
{
    [Header("TLE Source")] public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

    [Header("Prefabs & References")] public GameObject satellitePrefab;
    public Transform orbitParent;
    public CesiumGeoreference cesiumGeoreference;


    private readonly List<SatelliteController> satellites = new();
    private Dictionary<uint, Queue<double3>> satellitePositions = new();
    private const int MaxPositionCache = 50;
    private const int UpdateBatchSize = 250; // Größe des Updates pro Durchlauf
    private const int TotalUpdateDelay = 1; // Wartezeit in Sekunden zwischen den Updates


    void Start()
    {
        FetchTleData();
        StartCoroutine(PositionUpdateCoroutine());
        StartPositionCalculation();
    }

    void FetchTleData()
    {
        try
        {
            var provider = new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(tleUrl));
            var data = provider.GetTles();
            foreach (var tle in data.Values)
            {
                GameObject sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                sat.name = tle.NoradNumber + " " + tle.Name;
                var con = sat.GetComponent<SatelliteController>();
                con.Initialize(tle);
                satellites.Add(con);
                satellitePositions[con.Tle.NoradNumber] = new Queue<double3>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Parsing-Fehler: " + e.Message);
        }
    }

    private void StartPositionCalculation()
    {
        Task.Run(() =>
        {
            while (true)
            {
                DateTime nextTime = DateTime.Now;
                for (var i = 0; i < satellites.Count; i++)
                {
                    var satellite = satellites[i];
                    try
                    {
                        if (satellitePositions[satellite.Tle.NoradNumber].Count >= MaxPositionCache)
                        {
                            continue;
                        }

                        var positions = MaxPositionCache - satellitePositions[satellite.Tle.NoradNumber].Count;
                        
                        DateTime futureTime = nextTime; // 10 Minuten Schritt für jede Position
                        // Berechne die Positionen 10 Minuten weiter als den Startzeitpunkt
                        for (int j = 0; j < positions; j++)
                        {
                            futureTime = futureTime.AddMinutes(10);
                            EciCoordinate result = null;
                            try
                            {
                                result = satellite.OrbitPropagator.FindPosition(futureTime);
                            }
                            catch (DecayedException _)
                            {
                                Destroy(satellite);
                            }
                            catch (Exception _)
                            {
                                continue;
                            }

                            var pos = result.ToGeodetic();
                            var newPosition = new double3(pos.Longitude.Degrees, pos.Latitude.Degrees,
                                pos.Altitude * 1000);

                            satellitePositions[satellite.Tle.NoradNumber]
                                .Enqueue(newPosition); // Fügt die neue Position hinzu

                            if (i == satellites.Count - 1 && j == positions - 1)
                            {
                                nextTime = futureTime;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e);
                    }
                }

                Task.Delay(1000); // Berechnungen alle 100ms
            }
        });
    }

    private IEnumerator PositionUpdateCoroutine()
    {
        int startIndex = 0; // Startindex für den aktuellen Batch

        var delay = CalculateDelay(satellites.Count, TotalUpdateDelay);
        while (true)
        { 
            // Berechne den Endindex des aktuellen Batches
            int endIndex = Mathf.Min(startIndex + UpdateBatchSize, satellites.Count);

            // Aktualisiere die Satellitenpositionen im aktuellen Batch
            for (int i = startIndex; i < endIndex; i++)
            {
                // Wenn es Positionen zum Aktualisieren gibt
                if (satellitePositions[satellites[i].Tle.NoradNumber].Count > 0)
                {
                    var position =
                        satellitePositions[satellites[i].Tle.NoradNumber].Dequeue(); // Nächste Position holen

                    // Aktualisiere die Position des Satelliten
                    if (satellites[i].Renderer.isVisible)
                    {
                        satellites[i].Anchor.longitudeLatitudeHeight = position;
                    }
                    else
                    {
                        Debug.Log("Not visible");
                    }
                }
            }

            // Warte, bevor der nächste Batch verarbeitet wird
            yield return new WaitForSeconds(delay);

            // Wenn der aktuelle Batch abgeschlossen ist, gehe zum nächsten Batch
            startIndex = endIndex;

            // Wenn alle Satelliten aktualisiert wurden, setze den Startindex wieder auf 0
            if (startIndex >= satellites.Count)
            {
                startIndex = 0;
            }
        }
    }

    private float CalculateDelay(int totalItems, int targetIterationDuration)
    {
        if (UpdateBatchSize <= 0 || totalItems <= 0)
            return 0;

        int numberOfBatches = (int)Math.Ceiling((double)totalItems / UpdateBatchSize);
        return (float)targetIterationDuration / (float)numberOfBatches;
    }
}