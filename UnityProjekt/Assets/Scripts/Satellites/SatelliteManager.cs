using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CesiumForUnity;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.TLE;
using Unity.Mathematics;
using UnityEngine;

namespace Satellites
{
    public class SatelliteManager : MonoBehaviour
    {
        [Header("TLE Source")]
        public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

        [Header("Prefabs & References")] public GameObject satellitePrefab;
        public Transform orbitParent;
        public CesiumGeoreference cesiumGeoreference;
        public CesiumZoomController zoomController;

        [Header("Movement Settings")] [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
        public float fovThreshold = 70f;

        [Tooltip("Wie lange es dauert, einen Abschnitt zu durchlaufen")]
        public float segmentDuration = 0.5f;

        [Tooltip("Bei welchem Fortschritt die nächste Position vorgeladen wird (0-1)")] [Range(0.1f, 0.9f)]
        public float lookAheadThreshold = 0.6f;

        [Header("Initialization")] [Tooltip("Verzögerung vor der Anzeige der Satelliten (Sekunden)")]
        public float initialDelay = 0.5f;

        private readonly List<SatelliteController> satellites = new();

        private const int MaxPositionCache = 50;
        private const int UpdateBatchSize = 250;
        private float TotalUpdateDelay = 1;
        private bool simulationStarted;

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

                // Stelle sicher, dass der Satellit eine gültige Position hat
                if (satellite.Positions.Count > 0)
                {
                    satellite.ResetPath();
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
                var provider =
                    new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(tleUrl));
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
                        var futureTime = satellite.GeneratePositions(ref nextTime, MaxPositionCache);

                        if (i == satellites.Count - 1)
                        {
                            nextTime = futureTime;
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
                    foreach (var satellite in satellites)
                    {
                        if (satellite == null) continue;

                        // Starte kontinuierliche Bewegung, falls noch nicht aktiv
                        if (satellite.continuousMovementActive) continue;

                        satellite.continuousMovementActive = true;
                        StartCoroutine(ContinuousSmoothMovementCoroutine(satellite));
                    }
                }
                else
                {
                    // Im Space-Modus: Direkte Positionierung in Batches
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        if (i >= satellites.Count || satellites[i] == null) continue;

                        // Stoppe kontinuierliche Bewegung falls aktiv
                        satellites[i].continuousMovementActive = false;

                        // Direkte Positionierung wenn Positionen vorhanden
                        if (satellites[i].Positions.Count > 0)
                        {
                            var nextPosition = satellites[i].Positions.Dequeue();
                            if (satellites[i].Renderer.isVisible) satellites[i].Anchor.longitudeLatitudeHeight = nextPosition;
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

            satellite.ResetPath(true, segmentDuration);

            while (satellite.continuousMovementActive)
            {
                // Aktuellster Zeitpunkt
                float currentTime = Time.time;

                // Alte Pfadpunkte entfernen
                while (satellite.Paths.Count > 2 && satellite.Paths[1].ArrivalTime < currentTime)
                {
                    satellite.Paths.RemoveAt(0);
                }

                // Wenn wir zu wenige Punkte haben oder nahe am letzten Punkt sind, neue Punkte laden
                if ((satellite.Paths.Count < 2) ||
                    (satellite.Paths.Count == 2 && (currentTime > satellite.Paths[0].ArrivalTime +
                        (satellite.Paths[1].ArrivalTime - satellite.Paths[0].ArrivalTime) * lookAheadThreshold)))
                {
                    if (satellite.Positions.Count > 0)
                    {
                        double3 newTargetPos = satellite.Positions.Dequeue();

                        // Neuen Pfadpunkt mit Zeitstempel hinzufügen
                        float arrivalTime = (satellite.Paths.Count > 0)
                            ? satellite.Paths[^1].ArrivalTime + segmentDuration
                            : currentTime + segmentDuration;

                        satellite.Paths.Add(new SatellitePathPoint(newTargetPos, arrivalTime));
                    }
                }

                // Wenn wir mindestens zwei Punkte haben, zwischen ihnen interpolieren
                if (satellite.Paths.Count >= 2)
                {
                    SatellitePathPoint p0 = satellite.Paths[0];
                    SatellitePathPoint p1 = satellite.Paths[1];

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
                if (satellite.Paths.Count == 0 && satellite.Positions.Count == 0)
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
}