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

namespace Satellites
{
    public class SatelliteManager : MonoBehaviour
    {
        public static SatelliteManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;
        }

        [Header("TLE Source")]
        public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

        [Header("Prefabs & References")] public GameObject satellitePrefab;
        public CesiumGeoreference cesiumGeoreference;

        [Header("Simulation Time Settings")] public DateTime simulationStartTime = DateTime.Now; // beliebiger Start
        public float timeMultiplier = 1f; // 60 = 1 Sekunde echte Zeit = 1 Minute simulierte Zeit

        [Header("Satellite Models")] [Tooltip("Liste der verfügbaren Satelliten-Modelle")]
        public GameObject[] satelliteModelPrefabs;

        [Header("Materials")] [Tooltip("Material für Satelliten im Space-Modus")]
        public Material globalSpaceMaterial;

        [SerializeField] private HeatmapController _heatmapController;
        public DateTime CurrentSimulatedTime { get; private set; }
        private double simulationTimeSeconds;
        private TransformAccessArray _transformAccessArray;

        private readonly List<SatelliteController> _satellites = new();
        private JobHandle _handle;
        private bool _multiplierChanged;
        private NativeArray<Sgp4> _propagators;
        private NativeArray<Vector3> _currentPositions;

        void Start()
        {
            Debug.Log("SatelliteManager: Start");
            CurrentSimulatedTime = simulationStartTime;
            simulationTimeSeconds = 0.0;
            EnableGPUInstancingForAllMaterials();
            FetchTleData();
            AllocateTransformAccessArray();
        }

        void FetchTleData()
        {
            try
            {
                var provider =
                    new CachingRemoteTleProvider(true, TimeSpan.FromHours(12), "cacheTle.txt", new Uri(tleUrl));
                var data = provider.GetTles();
                Debug.Log($"TLE Data: Gefunden: {data.Count} Satelliten");

                int modelledSatellites = 0;
                foreach (var tle in data.Values)
                {
                    // Basis-Satellit erstellen
                    var sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                    sat.name = tle.NoradNumber + " " + tle.Name;
                    var con = sat.GetComponent<SatelliteController>();
                    con.Initialize(tle);

                    // Zufälliges Modell auswählen und anhängen
                    bool modelApplied = ApplyRandomModel(sat);
                    if (modelApplied) modelledSatellites++;

                    _satellites.Add(con);
                }

                Debug.Log($"Initialisiert: {_satellites.Count} Satelliten, {modelledSatellites} mit Modellen");
            }
            catch (Exception e)
            {
                Debug.LogError("Parsing-Fehler: " + e.Message);
            }
        }

        private bool ApplyRandomModel(GameObject satellite)
        {
            if (satelliteModelPrefabs == null || satelliteModelPrefabs.Length == 0)
            {
                Debug.LogWarning("Keine Satelliten-Modelle konfiguriert!");
                return false;
            }

            // Zufälliges Modell aus Array wählen
            int randomIndex = UnityEngine.Random.Range(0, satelliteModelPrefabs.Length);
            GameObject modelPrefab = satelliteModelPrefabs[randomIndex];

            // Mesh und MeshRenderer vom Satelliten finden oder erstellen wenn nicht vorhanden
            MeshFilter meshFilter = satellite.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = satellite.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = satellite.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = satellite.AddComponent<MeshRenderer>();
            }

            // Prefab laden, um Mesh und Materialien zu extrahieren
            GameObject tempModel = Instantiate(modelPrefab);
            MeshFilter modelMeshFilter = tempModel.GetComponent<MeshFilter>();
            MeshRenderer modelMeshRenderer = tempModel.GetComponent<MeshRenderer>();

            if (modelMeshFilter != null && modelMeshRenderer != null && modelMeshFilter.sharedMesh != null)
            {
                // Mesh kopieren
                meshFilter.mesh = modelMeshFilter.sharedMesh;

                // Material-Controller suchen oder hinzufügen
                SatelliteMaterialController materialController = satellite.GetComponent<SatelliteMaterialController>();
                if (materialController == null)
                {
                    materialController = satellite.AddComponent<SatelliteMaterialController>();
                }

                // Zoom-Controller Referenz setzen
                CesiumZoomController zoomController = FindObjectOfType<CesiumZoomController>();
                materialController.zoomController = zoomController;

                // Earth-Mode Materialien setzen
                Material[] materials = modelMeshRenderer.sharedMaterials;
                materialController.earthModeMaterials = materials;

                // Space-Material direkt zuweisen ohne Überprüfung und Logging
                materialController.spaceMaterial = globalSpaceMaterial;

                // Stelle sicher, dass der Renderer aktiviert ist
                meshRenderer.enabled = true;

                // Wende sofort das richtige Material an
                if (zoomController && zoomController.targetCamera)
                {
                    materialController.UpdateMaterial();
                }
                else
                {
                    // Fallback: Starte mit Earth-Materialien
                    meshRenderer.materials = materials;
                }

                Destroy(tempModel);
                return true;
            }
            else
            {
                Destroy(tempModel);
                return false;
            }
        }

        private void EnableGPUInstancingForAllMaterials()
        {
            int prefabCount = satelliteModelPrefabs?.Length ?? 0;
            Debug.LogWarning($"GPU Instancing Check: {prefabCount} Modell-Prefabs gefunden");

            // Für die Modell-Prefabs
            if (satelliteModelPrefabs != null)
            {
                foreach (GameObject prefab in satelliteModelPrefabs)
                {
                    if (prefab == null)
                    {
                        Debug.LogWarning("Prefab ist null!");
                        continue;
                    }

                    MeshRenderer renderer = prefab.GetComponent<MeshRenderer>();
                    if (renderer == null)
                    {
                        Debug.LogWarning($"Prefab {prefab.name} hat keinen MeshRenderer!");
                        continue;
                    }

                    Material[] mats = renderer.sharedMaterials;

                    foreach (Material mat in mats)
                    {
                        if (mat == null)
                        {
                            Debug.LogWarning("Material ist null!");
                            continue;
                        }

                        if (!mat.enableInstancing)
                        {
                            mat.enableInstancing = true;
                            Debug.LogWarning($"GPU Instancing für Material {mat.name} wurde aktiviert");
                        }
                    }
                }
            }

            // Für das Space-Material
            if (globalSpaceMaterial != null)
            {
                Debug.LogWarning(
                    $"Space Material: GPU Instancing ist {(globalSpaceMaterial.enableInstancing ? "bereits aktiviert" : "deaktiviert")}");

                if (!globalSpaceMaterial.enableInstancing)
                {
                    globalSpaceMaterial.enableInstancing = true;
                    Debug.LogWarning("GPU Instancing für globales Space-Material wurde aktiviert");
                }
            }
            else
            {
                Debug.LogWarning("Globales Space-Material ist nicht zugewiesen!");
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

        public void OnTimeMultiplierChanged(float value)
        {
            _multiplierChanged = true;
            timeMultiplier = value;
            _multiplierChanged = false;
        }

        private void Update()
        {
            UpdateCurrentTime();
            if (!_handle.IsCompleted) return;
            _handle.Complete();
            _heatmapController.UpdateHeatmap(_currentPositions);

            var job = new MoveSatelliteJobParallelForTransform
            {
                CurrentTime = CurrentSimulatedTime,
                EcefToLocalMatrix = cesiumGeoreference.ecefToLocalMatrix,
                OrbitPropagator = _propagators,
                Positions = _currentPositions
            };

            _handle = job.ScheduleByRef(_transformAccessArray);
        }

        private void UpdateCurrentTime()
        {
            simulationTimeSeconds += Time.deltaTime * timeMultiplier;
            CurrentSimulatedTime = simulationStartTime.AddSeconds(simulationTimeSeconds);
        }

        private void OnDestroy()
        {
            if (_transformAccessArray.isCreated)
                _transformAccessArray.Dispose();

            if (_propagators.IsCreated)
                _propagators.Dispose();
        }

        public List<string> GetSatelliteNames()
        {
            return _satellites.Select(s => s.gameObject.name).ToList();
        }

        public SatelliteController GetSatelliteByName(string name)
        {
            return _satellites.FirstOrDefault(s => s.gameObject.name == name);
        }
    }
}