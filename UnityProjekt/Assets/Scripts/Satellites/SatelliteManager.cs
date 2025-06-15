using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.SimpleSpinner;
using CesiumForUnity;
using DefaultNamespace;
using Heatmap;
using Satellites.SGP.Propagation;
using Satellites.SGP.TLE;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Serialization;

namespace Satellites
{
    public class SatelliteManager : MonoBehaviour
    {
        // --- Singleton ---
        public static SatelliteManager Instance { get; private set; }

        private const string TleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

        [Header("Prefabs & References")]
        public GameObject satellitePrefab;
        public CesiumGeoreference cesiumGeoreference;
        public GameObject satelliteParent;
        public HeatmapController heatmapController;
        public TimeSlider.TimeSlider time;


        [Header("Satellite Models")]
        [Tooltip("Liste der verfügbaren Satelliten-Modelle")]
        public GameObject[] satelliteModelPrefabs;

        [Tooltip("Spezielles Modell für die ISS")]
        public GameObject issModelPrefab;
        private readonly List<int> _tooNearIss = new() { 63520, 49044, 62030, 63129, 63204 };


        [Header("Materials")]
        [Tooltip("Material für Satelliten im Space-Modus")]
        public Material globalSpaceMaterial;

        public DoubleSlider.Scripts.DoubleSlider altitudeSlider;

        // --- Satellitenverwaltung ---
        private readonly List<Satellite> _satellites = new();
        public event Action<List<Satellite>> OnSatellitesLoaded;

        // --- Jobs & NativeArrays ---
        private TransformAccessArray _transformAccessArray;
        private NativeArray<Sgp4> _propagators;
        private NativeArray<Vector3> _currentPositions;
        private JobHandle _handle;

        public bool satellitesActive = true;

        // --- Unity Lifecycle ---
        void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;
        }

        void Start()
        {
            Debug.Log("SatelliteManager: Start");
            EnableGpuInstancing();
            FetchTleData();
            AllocateTransformAccessArray();
        }

        private void Update()
        {
            if (!satellitesActive)
            {
                return;
            }

            if (!_handle.IsCompleted) return;
            _handle.Complete();
            heatmapController.UpdateHeatmap(_currentPositions);

            var job = new MoveSatelliteJobParallelForTransform
            {
                CurrentTime = time.CurrentSimulatedTime,
                EcefToLocalMatrix = cesiumGeoreference.ecefToLocalMatrix,
                OrbitPropagator = _propagators,
                Positions = _currentPositions
            };

            _handle = job.ScheduleByRef(_transformAccessArray);
        }

        private void OnDestroy()
        {
            Debug.Log("[SatelliteManager] OnDestroy: Completing JobHandle before disposing NativeArrays.");
            _handle.Complete();

            if (_transformAccessArray.isCreated)
                _transformAccessArray.Dispose();

            if (_propagators.IsCreated)
                _propagators.Dispose();

            if (_currentPositions.IsCreated)
                _currentPositions.Dispose();
        }

        public void OnAltitudeSliderChanged(float min, float max)
        {
            foreach (var satellite in _satellites)
            {
                var timeInMinutes = (time.CurrentSimulatedTime - satellite.OrbitPropagator.Orbit.Epoch).TotalMinutes;
                var geoCoord = satellite.OrbitPropagator.FindPosition(timeInMinutes).ToGeodetic();
                if (geoCoord.Altitude < min || (!Mathf.Approximately(altitudeSlider._maxValue, max) && geoCoord.Altitude > max))
                    satellite.gameObject.SetActive(false);
                else if (!satellite.gameObject.activeSelf)
                    satellite.gameObject.SetActive(true);
            }
        }

        public List<string> GetSatelliteNames()
        {
            return _satellites.Select(s => s.gameObject.name).ToList();
        }

        public Satellite GetSatelliteByName(string name)
        {
            return _satellites.FirstOrDefault(s => s.gameObject.name == name);
        }

        public List<Satellite> GetAllSatellites()
        {
            return _satellites;
        }


        private void FetchTleData()
        {
            try
            {
                Debug.Log($"[SatelliteManager] Starte TLE-Download von: {TleUrl}");
        
                var provider = new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(TleUrl));
                var data = provider.GetTles();
        
                Debug.Log($"[SatelliteManager] TLE Data: Gefunden: {data.Count} Satelliten");
        
                if (data.Count == 0)
                {
                    Debug.LogError("[SatelliteManager] FEHLER: Keine TLE-Daten erhalten!");
                }

                int modelledSatellites = 0;
                foreach (var tle in data.Values)
                {
                    var modelApplied = CreateSatellite(tle);
                    if (modelApplied) modelledSatellites++;
                }

                Debug.Log($"Initialisiert: {_satellites.Count} Satelliten, {modelledSatellites} mit Modellen");
                OnSatellitesLoaded?.Invoke(_satellites);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SatelliteManager] FEHLER beim TLE-Download: {e.Message}\n{e.StackTrace}");
            }
        }

        private bool CreateSatellite(Tle tle)
        {
            if (_tooNearIss.Contains((int)tle.NoradNumber)) return true;
            var satelliteGo = Instantiate(satellitePrefab, satelliteParent.transform);
            var satellite = satelliteGo.GetComponent<Satellite>();

            // Übergebe ISS-Modell falls vorhanden
            var modelApplied = satellite.Init(tle, satelliteModelPrefabs, globalSpaceMaterial, issModelPrefab);
            _satellites.Add(satellite);
            return modelApplied;
        }

        private void EnableGpuInstancing()
        {
            EnableInstancingForPrefabs(satelliteModelPrefabs);
            EnableInstancingForMaterial(globalSpaceMaterial);
        }

        private void EnableInstancingForPrefabs(GameObject[] prefabs)
        {
            if (prefabs == null) return;
            Debug.LogWarning($"GPU Instancing Check: {prefabs.Length} Modell-Prefabs gefunden");

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                {
                    Debug.LogWarning("Prefab ist null!");
                    continue;
                }

                var renderer = prefab.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"Prefab {prefab.name} hat keinen MeshRenderer!");
                    continue;
                }

                foreach (var mat in renderer.sharedMaterials)
                {
                    EnableInstancingForMaterial(mat);
                }
            }
        }

        private void EnableInstancingForMaterial(Material mat)
        {
            if (mat == null)
            {
                Debug.LogWarning("Material ist null!");
                return;
            }

            if (!mat.enableInstancing)
            {
                mat.enableInstancing = true;
                Debug.LogWarning($"GPU Instancing für Material {mat.name} wurde aktiviert");
            }
            else
            {
                Debug.LogWarning($"GPU Instancing für Material {mat.name} ist bereits aktiviert");
            }
        }

        private void AllocateTransformAccessArray()
        {
            if (_satellites.Count == 0)
            {
                Debug.LogError("Keine Satelliten zum Initialisieren des TransformAccessArray");
                return;
            }

            Debug.Log($"Initialisiere TransformAccessArray mit {_satellites.Count} Satelliten");

            _propagators = new NativeArray<Sgp4>(_satellites.Count, Allocator.Persistent);
            _currentPositions = new NativeArray<Vector3>(_satellites.Count, Allocator.Persistent);

            var transforms = new Transform[_satellites.Count];
            for (int i = 0; i < _satellites.Count; i++)
            {
                transforms[i] = _satellites[i].transform;
                _propagators[i] = _satellites[i].OrbitPropagator;
            }

            _transformAccessArray = new TransformAccessArray(transforms);
            Debug.Log("TransformAccessArray erfolgreich initialisiert");
        }
    }
}