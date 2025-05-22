using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.SimpleSpinner;
using CesiumForUnity;
using DefaultNamespace;
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

        [Header("Prefabs & References")] 
        public GameObject satellitePrefab;
        public CesiumGeoreference cesiumGeoreference;

        [Header("Simulation Time Settings")] 
        public DateTime simulationStartTime = DateTime.Now; // beliebiger Start
        public float timeMultiplier = 1f; // 60 = 1 Sekunde echte Zeit = 1 Minute simulierte Zeit

        [Header("Satellite Models")]
        [Tooltip("Liste der verfügbaren Satelliten-Modelle")]
        public GameObject[] satelliteModelPrefabs;

        public DateTime CurrentSimulatedTime { get; private set; }
        private double simulationTimeSeconds;
        private TransformAccessArray _transformAccessArray;

        private readonly List<SatelliteController> _satellites = new();
        private JobHandle _handle;
        private bool _multiplierChanged;
        private NativeArray<Sgp4> _propagators;
        
        void Start()
        {
            Debug.Log("SatelliteManager: Start");
            CurrentSimulatedTime = simulationStartTime;
            simulationTimeSeconds = 0.0;
            FetchTleData();
            AllocateTransformAccessArray();
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
                    // Basis-Satellit erstellen
                    var sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                    sat.name = tle.NoradNumber + " " + tle.Name;
                    var con = sat.GetComponent<SatelliteController>();
                    con.Initialize(tle);

                    // Zufälliges Modell auswählen und anhängen
                    ApplyRandomModel(sat);

                    _satellites.Add(con);
                }

                Debug.Log($"Initialisiert: {_satellites.Count} Satelliten");
            }
            catch (Exception e)
            {
                Debug.LogError("Parsing-Fehler: " + e.Message);
            }
        }
        
        private void ApplyRandomModel(GameObject satellite)
{
    if (satelliteModelPrefabs == null || satelliteModelPrefabs.Length == 0)
    {
        Debug.LogWarning("Keine Satelliten-Modelle konfiguriert!");
        return;
    }

    // Zufälliges Modell aus Array wählen
    int randomIndex = UnityEngine.Random.Range(0, satelliteModelPrefabs.Length);
    GameObject modelPrefab = satelliteModelPrefabs[randomIndex];

    // Mesh und MeshRenderer vom Satelliten finden oder erstellen wenn nicht vorhanden
    MeshFilter meshFilter = satellite.GetComponent<MeshFilter>();
    if (meshFilter == null)
    {
        meshFilter = satellite.AddComponent<MeshFilter>();
        Debug.Log($"MeshFilter zu {satellite.name} hinzugefügt");
    }
        
    MeshRenderer meshRenderer = satellite.GetComponent<MeshRenderer>();
    if (meshRenderer == null)
    {
        meshRenderer = satellite.AddComponent<MeshRenderer>();
        Debug.Log($"MeshRenderer zu {satellite.name} hinzugefügt");
    }

    // Prefab laden, um Mesh und Materialien zu extrahieren
    GameObject tempModel = Instantiate(modelPrefab);
    MeshFilter modelMeshFilter = tempModel.GetComponent<MeshFilter>();
    MeshRenderer modelMeshRenderer = tempModel.GetComponent<MeshRenderer>();

    if (modelMeshFilter != null && modelMeshRenderer != null)
    {
        // Mesh kopieren
        meshFilter.mesh = modelMeshFilter.sharedMesh;

        // Material-Controller suchen oder hinzufügen
        SatelliteMaterialController materialController = satellite.GetComponent<SatelliteMaterialController>();
        if (materialController == null)
        {
            materialController = satellite.AddComponent<SatelliteMaterialController>();
            Debug.Log($"SatelliteMaterialController zu {satellite.name} hinzugefügt");
        }

        // Zoom-Controller Referenz setzen
        CesiumZoomController zoomController = FindObjectOfType<CesiumZoomController>();
        materialController.zoomController = zoomController;

        // Earth-Mode Materialien setzen
        Material[] materials = modelMeshRenderer.sharedMaterials;
        materialController.earthModeMaterials = materials;
        
        // Materialien auch direkt dem Renderer zuweisen
        meshRenderer.materials = materials;

        // Space-Mode Material
        if (materialController.spaceMaterial == null)
        {
            Material spaceMaterial = new Material(Shader.Find("Standard"));
            spaceMaterial.color = Color.gray;
            materialController.spaceMaterial = spaceMaterial;
        }
    }
    else
    {
        Debug.LogError($"Modell {modelPrefab.name} hat keinen MeshFilter oder MeshRenderer");
    }

    // Temporäres Modell löschen
    Destroy(tempModel);
}

        private void AllocateTransformAccessArray()
        {
            _propagators = new NativeArray<Sgp4>(_satellites.Count, Allocator.Persistent);

            var transforms = new Transform[_satellites.Count];
            for (int i = 0; i < _satellites.Count; i++)
            {
                transforms[i] = _satellites[i].transform;
                _propagators[i] = _satellites[i].OrbitPropagator;
            }

            _transformAccessArray = new TransformAccessArray(transforms.ToArray());
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

            var job = new MoveSatelliteJobParallelForTransform
            {
                CurrentTime = CurrentSimulatedTime,
                EcefToLocalMatrix = cesiumGeoreference.ecefToLocalMatrix,
                OrbitPropagator = _propagators
            };

            _handle = job.ScheduleByRef(_transformAccessArray, _handle);
        }

        private void UpdateCurrentTime()
        {
            simulationTimeSeconds += Time.deltaTime * timeMultiplier;
            CurrentSimulatedTime = simulationStartTime.AddSeconds(simulationTimeSeconds);
        }

        private void OnDestroy()
        {
            _transformAccessArray.Dispose();
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