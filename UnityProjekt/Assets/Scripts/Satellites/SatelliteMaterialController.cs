using UnityEngine;

namespace Satellites
{
    public class SatelliteMaterialController : MonoBehaviour
    {
        [Header("Referenzen")] public CesiumZoomController zoomController;

        [Header("Einstellungen")] [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
        public float fovThreshold = 70f;

        private Material[] _earthModeMaterials;
        private Material _spaceMaterial;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private bool _lastMode;

        void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>() != null
                ? GetComponent<MeshRenderer>()
                : gameObject.AddComponent<MeshRenderer>();
            
            _meshFilter = GetComponent<MeshFilter>() != null
                ? GetComponent<MeshFilter>()
                : gameObject.AddComponent<MeshFilter>();
        }

        void Start()
        {
            UpdateMaterial();
        }

        void Update()
        {
            if (!zoomController || !zoomController.targetCamera) return;
            bool isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
            if (isEarthMode == _lastMode) return;
            _lastMode = isEarthMode;
            UpdateMaterial();
        }

        public bool SetModel(GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
        {
            if (!TryGetRandomModelPrefab(satelliteModelPrefabs, out var modelPrefab))
                return false;
            if (!TryApplyModel(modelPrefab, globalSpaceMaterial))
                return false;
            return true;
        }

        private bool TryGetRandomModelPrefab(GameObject[] prefabs, out GameObject prefab)
        {
            prefab = null;
            if (prefabs == null || prefabs.Length == 0)
                return false;
            int randomIndex = Random.Range(0, prefabs.Length);
            prefab = prefabs[randomIndex];
            return prefab != null;
        }

        private bool TryApplyModel(GameObject modelPrefab, Material globalSpaceMaterial)
        {
            var tempModel = Instantiate(modelPrefab);
            var modelMeshFilter = tempModel.GetComponent<MeshFilter>();
            var modelMeshRenderer = tempModel.GetComponent<MeshRenderer>();
            if (modelMeshFilter == null || modelMeshRenderer == null || modelMeshFilter.sharedMesh == null)
            {
                Destroy(tempModel);
                return false;
            }

            _meshFilter.mesh = modelMeshFilter.sharedMesh;
            NormalizeSatelliteSize(modelMeshFilter.sharedMesh);

            _earthModeMaterials = modelMeshRenderer.sharedMaterials;
            _spaceMaterial = globalSpaceMaterial;
            _meshRenderer.enabled = true;

            if (zoomController && zoomController.targetCamera)
                UpdateMaterial();
            else
                _meshRenderer.materials = modelMeshRenderer.sharedMaterials;

            Destroy(tempModel);
            return true;
        }

        private void UpdateMaterial()
        {
            if (!_meshRenderer)
                return;

            bool isEarthMode = zoomController && zoomController.targetCamera &&
                               zoomController.targetCamera.fieldOfView < fovThreshold;

            if (isEarthMode)
            {
                if (_earthModeMaterials == null || _earthModeMaterials.Length <= 0) return;
                _meshRenderer.enabled = true;
                _meshRenderer.sharedMaterials = _earthModeMaterials;
            }
            else
            {
                if (_spaceMaterial != null)
                {
                    _meshRenderer.enabled = true;
                    _meshRenderer.sharedMaterials = new[] { _spaceMaterial };
                }
                else
                {
                    _meshRenderer.enabled = false;
                }
            }
        }

        private void NormalizeSatelliteSize(Mesh mesh)
        {
            float targetSize = 40000f;
            Bounds bounds = mesh.bounds;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scaleFactor = targetSize / maxDimension;
            gameObject.transform.localScale = Vector3.one * scaleFactor;
        }
    }
}