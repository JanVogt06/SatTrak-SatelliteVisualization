using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.SimpleSpinner;
using CesiumForUnity;
using DefaultNamespace;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.TLE;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Satellites
{
    public class SatelliteManager : MonoBehaviour
    {
        [Header("TLE Source")]
        public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

        [Header("Prefabs & References")] public GameObject satellitePrefab;
        public CesiumGeoreference cesiumGeoreference;

        [Header("Simulation Time Settings")] public DateTime simulationStartTime = DateTime.Now; // beliebiger Start
        public float timeMultiplier = 10f; // 60 = 1 Sekunde echte Zeit = 1 Minute simulierte Zeit
        public static int NextPositionAmount = 50;

        private DateTime currentSimulatedTime;
        private double simulationTimeSeconds;
        private TransformAccessArray _transformAccessArray;

        private readonly List<SatelliteController> _satellites = new();
        private JobHandle _handle;
        private readonly Queue<MoveSatelliteJobParallelForTransform> jobs = new();
        private MoveSatelliteJobParallelForTransform _currentJob;
        public GameObject spinner;
        private bool _multiplierChanged = false;
        private SatelliteController satelliteExample;

        void Start()
        {
            Debug.Log("SatelliteManager: Start");
            currentSimulatedTime = simulationStartTime;
            simulationTimeSeconds = 0.0;
            FetchTleData();
            satelliteExample = _satellites.Single(sat => sat.name == "25544 ISS (ZARYA)");
            AllocateTransformAccessArray();
            StartPositionGeneration();
        }

        private void StartPositionGeneration()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (_multiplierChanged)
                        continue;
                    GeneratePositions();
                }
            });
        }

        private void GeneratePositions()
        {
            if (jobs.Count >= NextPositionAmount)
                return;

            var positions = new NativeArray<double3>(_satellites.Count, Allocator.Persistent);
            // Schleife über Satelliten, nur gültige verwenden
            for (var i = 0; i < _satellites.Count; i++)
            {
                try
                {
                    positions[i] = _satellites[i].CalculatePosition(currentSimulatedTime, cesiumGeoreference.ecefToLocalMatrix);
                }
                catch
                {
                    Debug.LogWarning($"Satellit {_satellites[i].name} konnte nicht berechnet werden.");
                }
            }

            var job = new MoveSatelliteJobParallelForTransform
            {
                Positions = positions
            };

            jobs.Enqueue(job);
        }

        // Angepasste FetchTleData mit Option zum Verstecken
        void FetchTleData()
        {
            try
            {
                var provider =
                    new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(tleUrl));
                var data = provider.GetTles();
                Debug.Log($"TLE Data: Gefunden: {data.Count} Satelliten");

                foreach (var tle in data.Values)
                {
                    var sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                    sat.name = tle.NoradNumber + " " + tle.Name;
                    var con = sat.GetComponent<SatelliteController>();
                    con.Initialize(tle, cesiumGeoreference);

                    _satellites.Add(con);
                }

                Debug.Log($"Initialisiert: {_satellites.Count} Satelliten");
            }
            catch (Exception e)
            {
                Debug.LogError("Parsing-Fehler: " + e.Message);
            }
        }

        private void AllocateTransformAccessArray()
        {
            var transforms = _satellites.Select(sat => sat.transform).ToArray();
            _transformAccessArray = new TransformAccessArray(transforms.ToArray());
        }

        public void OnTimeMultiplierChanged(float value)
        {
            _multiplierChanged = true;
            timeMultiplier = value;
            spinner.SetActive(true);
            if (!_handle.IsCompleted)
            {
                _handle.Complete();
                _currentJob.Positions.Dispose();
            }
            ClearCurrentPositions();
            spinner.SetActive(false);
            _multiplierChanged = false;
        }

        private void ClearCurrentPositions()
        {
            jobs.Clear();
            _satellites.ForEach(sat => sat.NextPositions.Clear());
            GeneratePositions();
        }

        private void Update()
        {
            if (!_handle.IsCompleted || jobs.Count == 0 || _multiplierChanged) return;
            _handle.Complete();
            _currentJob.Positions.Dispose();

            // Simulationszeit updaten
            simulationTimeSeconds += Time.deltaTime * timeMultiplier;
            currentSimulatedTime = simulationStartTime.AddSeconds(simulationTimeSeconds);
            satelliteExample.DrawOrbit();

            _currentJob = jobs.Dequeue();

            _handle = _currentJob.ScheduleByRef(_transformAccessArray, _handle);
        }

        private void OnDestroy()
        {
            _transformAccessArray.Dispose();
        }
    }
}