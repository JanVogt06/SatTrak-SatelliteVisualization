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
    [Header("TLE Source")]
    public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

    [Header("Prefabs & References")]
    public GameObject satellitePrefab;
    public Transform orbitParent;
    public CesiumGeoreference cesiumGeoreference;
    public CesiumZoomController zoomController;
    
    [Header("Movement Settings")]
    [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
    public float fovThreshold = 70f;
    [Tooltip("Wie lange es dauert, einen Abschnitt zu durchlaufen")]
    public float segmentDuration = 0.5f;
    [Tooltip("Bei welchem Fortschritt die nächste Position vorgeladen wird (0-1)")]
    [Range(0.1f, 0.9f)]
    public float lookAheadThreshold = 0.6f;
    
    [Header("Initialization")]
    [Tooltip("Verzögerung vor der Anzeige der Satelliten (Sekunden)")]
    public float initialDelay = 0.5f;

    private readonly List<SatelliteController> satellites = new();
    private Dictionary<uint, Queue<double3>> satellitePositions = new();
    private Dictionary<uint, bool> continuousMovementActive = new();
    private Dictionary<uint, List<SatellitePathPoint>> activePaths = new();
    
    private const int MaxPositionCache = 50;
    private const int UpdateBatchSize = 250;
    private float TotalUpdateDelay = 1;
    private bool simulationStarted = false;

    // Struktur für die Satelliten-Pfadpunkte mit Zeitstempel
    private class SatellitePathPoint
    {
        public double3 Position;
        public float ArrivalTime; // Wann der Punkt erreicht werden soll
        
        public SatellitePathPoint(double3 position, float arrivalTime)
        {
            Position = position;
            ArrivalTime = arrivalTime;
        }
    }

    void Start()
    {
        Debug.Log("SatelliteManager: Start");
        
        // Verzögere den gesamten Start, um genug Zeit für Positionsberechnung zu haben
        StartCoroutine(DelayedStart());
    }
    
    private IEnumerator DelayedStart()
    {
        // Alles wird vorbereitet, aber Satelliten bleiben unsichtbar
        FetchTleData(false);
        StartPositionCalculation();
        
        // Warte, damit die Positionsberechnung Zeit hat
        yield return new WaitForSeconds(initialDelay);
        
        // Satelliten anzeigen und Bewegung starten
        ShowAllSatellites();
        StartCoroutine(SpaceModeUpdateCoroutine());
        simulationStarted = true;
    }
    
    // Zeige alle Satelliten an
    private void ShowAllSatellites()
    {
        foreach (var satellite in satellites)
        {
            if (satellite == null) continue;
            
            uint noradNumber = satellite.Tle.NoradNumber;
            
            // Stelle sicher, dass der Satellit eine gültige Position hat
            if (satellitePositions.ContainsKey(noradNumber) && 
                satellitePositions[noradNumber].Count > 0)
            {
                var position = satellitePositions[noradNumber].Dequeue();
                satellite.Anchor.longitudeLatitudeHeight = position;
                
                if (activePaths.ContainsKey(noradNumber))
                {
                    activePaths[noradNumber].Clear();
                    activePaths[noradNumber].Add(new SatellitePathPoint(position, Time.time));
                }
            }
            
            // Aktiviere Renderer
            if (satellite.Renderer != null)
            {
                satellite.Renderer.enabled = true;
            }
        }
    }
    
    public void OnSliderValueChanged(float newValue)
    {
        TotalUpdateDelay = newValue;
    }
    
    // Angepasste FetchTleData mit Option zum Verstecken
    void FetchTleData(bool visible = true)
    {
        try
        {
            var provider = new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(tleUrl));
            var data = provider.GetTles();
            Debug.Log($"TLE Data: Gefunden: {data.Count} Satelliten");
            
            foreach (var tle in data.Values)
            {
                GameObject sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                sat.name = tle.NoradNumber + " " + tle.Name;
                var con = sat.GetComponent<SatelliteController>();
                con.Initialize(tle);
                
                // Verstecke Satelliten bis sie bereit sind
                if (con.Renderer != null && !visible)
                {
                    con.Renderer.enabled = false;
                }
                
                satellites.Add(con);
                satellitePositions[con.Tle.NoradNumber] = new Queue<double3>();
                continuousMovementActive[con.Tle.NoradNumber] = false;
                activePaths[con.Tle.NoradNumber] = new List<SatellitePathPoint>();
            }
            
            Debug.Log($"Initialisiert: {satellites.Count} Satelliten");
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
                        
                        DateTime futureTime = nextTime;
                        for (int j = 0; j < positions; j++)
                        {
                            futureTime = futureTime.AddMinutes(1);
                            EciCoordinate result = null;
                            try
                            {
                                result = satellite.OrbitPropagator.FindPosition(futureTime);
                            }
                            catch (DecayedException _)
                            {
                                // Behandle abgestürzte Satelliten
                                continue;
                            }
                            catch (Exception _)
                            {
                                continue;
                            }

                            var pos = result.ToGeodetic();
                            var newPosition = new double3(pos.Longitude.Degrees, pos.Latitude.Degrees,
                                pos.Altitude * 1000);

                            satellitePositions[satellite.Tle.NoradNumber].Enqueue(newPosition);

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

                Task.Delay((int)(TotalUpdateDelay * 1000));
            }
        });
    }

    private IEnumerator SpaceModeUpdateCoroutine()
    {
        int startIndex = 0;
        int updateCount = 0;
        
        // Warte, bis die Simulation wirklich gestartet wurde
        yield return new WaitUntil(() => simulationStarted);
        
        while (true)
        { 
            updateCount++;
            if (updateCount % 100 == 0)
            {
                Debug.Log($"Position Update: Frame {updateCount}");
            }
            
            var delay = CalculateDelay(satellites.Count, TotalUpdateDelay);
            int endIndex = Mathf.Min(startIndex + UpdateBatchSize, satellites.Count);
            
            bool isEarthMode = false;
            if (zoomController != null && zoomController.targetCamera != null)
            {
                isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
            }

            // Modus-basierte Verarbeitung
            if (isEarthMode)
            {
                for (int i = 0; i < satellites.Count; i++)
                {
                    if (satellites[i] == null) continue;
                    
                    uint noradNumber = satellites[i].Tle.NoradNumber;
                    
                    // Starte kontinuierliche Bewegung, falls noch nicht aktiv
                    if (!continuousMovementActive.ContainsKey(noradNumber) || 
                        !continuousMovementActive[noradNumber])
                    {
                        continuousMovementActive[noradNumber] = true;
                        StartCoroutine(ContinuousSmoothMovementCoroutine(satellites[i]));
                    }
                }
            }
            else
            {
                // Im Space-Modus: Direkte Positionierung in Batches
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i >= satellites.Count || satellites[i] == null) continue;
                    
                    uint noradNumber = satellites[i].Tle.NoradNumber;
                    
                    // Stoppe kontinuierliche Bewegung falls aktiv
                    continuousMovementActive[noradNumber] = false;
                    
                    // Direkte Positionierung wenn Positionen vorhanden
                    if (satellitePositions.ContainsKey(noradNumber) && 
                        satellitePositions[noradNumber].Count > 0)
                    {
                        var nextPosition = satellitePositions[noradNumber].Dequeue();
                        satellites[i].Anchor.longitudeLatitudeHeight = nextPosition;
                    }
                }
                
                // Batch-Index-Update nur für Space-Modus
                startIndex = endIndex;
                if (startIndex >= satellites.Count)
                {
                    startIndex = 0;
                }
            }

            yield return new WaitForSeconds(delay);
        }
    }
    
    // Methode für nahtlose Bewegung mit Look-ahead
    private IEnumerator ContinuousSmoothMovementCoroutine(SatelliteController satellite)
    {
        // Warte, bis die Simulation wirklich gestartet wurde
        if (!simulationStarted)
            yield return new WaitUntil(() => simulationStarted);
            
        uint noradNumber = satellite.Tle.NoradNumber;
        List<SatellitePathPoint> path = activePaths[noradNumber];
        
        // Initialisiere aktuelle Position als Startpunkt
        double3 currentPosition = satellite.Anchor.longitudeLatitudeHeight;
        
        // Startpunkt zum Pfad hinzufügen
        path.Clear();
        path.Add(new SatellitePathPoint(currentPosition, Time.time));
        
        // Füge sofort einen zweiten Punkt hinzu, wenn verfügbar
        if (satellitePositions[noradNumber].Count > 0)
        {
            double3 nextPos = satellitePositions[noradNumber].Dequeue();
            path.Add(new SatellitePathPoint(nextPos, Time.time + segmentDuration));
        }
        
        while (continuousMovementActive[noradNumber])
        {
            // Aktuellster Zeitpunkt
            float currentTime = Time.time;
            
            // Alte Pfadpunkte entfernen
            while (path.Count > 2 && path[1].ArrivalTime < currentTime)
            {
                path.RemoveAt(0);
            }
            
            // Wenn wir zu wenige Punkte haben oder nahe am letzten Punkt sind, neue Punkte laden
            if ((path.Count < 2) || 
                (path.Count == 2 && (currentTime > path[0].ArrivalTime + (path[1].ArrivalTime - path[0].ArrivalTime) * lookAheadThreshold)))
            {
                if (satellitePositions[noradNumber].Count > 0)
                {
                    double3 newTargetPos = satellitePositions[noradNumber].Dequeue();
                    
                    // Neuen Pfadpunkt mit Zeitstempel hinzufügen
                    float arrivalTime = (path.Count > 0) ? 
                                      path[path.Count-1].ArrivalTime + segmentDuration : 
                                      currentTime + segmentDuration;
                                      
                    path.Add(new SatellitePathPoint(newTargetPos, arrivalTime));
                }
            }
            
            // Wenn wir mindestens zwei Punkte haben, zwischen ihnen interpolieren
            if (path.Count >= 2)
            {
                SatellitePathPoint p0 = path[0];
                SatellitePathPoint p1 = path[1];
                
                // Berechne, wie weit wir zwischen den beiden Punkten sind (0-1)
                float segmentDuration = p1.ArrivalTime - p0.ArrivalTime;
                float segmentProgress = (currentTime - p0.ArrivalTime) / segmentDuration;
                segmentProgress = Mathf.Clamp01(segmentProgress);
                
                // Verwende SmoothStep für gleichmäßige Beschleunigung/Verlangsamung
                float smoothProgress = Mathf.SmoothStep(0f, 1f, segmentProgress);
                
                // Berechne interpolierte Position
                double3 interpolatedPosition = new double3(
                    Mathf.Lerp((float)p0.Position.x, (float)p1.Position.x, smoothProgress),
                    Mathf.Lerp((float)p0.Position.y, (float)p1.Position.y, smoothProgress),
                    Mathf.Lerp((float)p0.Position.z, (float)p1.Position.z, smoothProgress)
                );
                
                // Setze Position
                satellite.Anchor.longitudeLatitudeHeight = interpolatedPosition;
            }
            
            // Wenn wir keine Punkte haben und auch keine in der Queue, warten
            if (path.Count == 0 && satellitePositions[noradNumber].Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                yield return null;
            }
        }
    }

    private float CalculateDelay(int totalItems, float targetIterationDuration)
    {
        if (UpdateBatchSize <= 0 || totalItems <= 0)
            return 0;

        int numberOfBatches = (int)Math.Ceiling((double)totalItems / UpdateBatchSize);
        return (float)targetIterationDuration / (float)numberOfBatches;
    }
}