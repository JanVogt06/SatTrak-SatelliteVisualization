using UnityEngine;

namespace Satellites
{
    public class SatelliteMaterialController : MonoBehaviour
    {
        [Header("Referenzen")]
        public CesiumZoomController zoomController;
    
        [Header("Einstellungen")]
        [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
        public float fovThreshold = 70f;
        
        [Header("Materials")]
        [Tooltip("Das komplexe Material, das nur im Earth-Modus verwendet wird")]
        private Material[] earthModeMaterials;
    
        [Tooltip("Einfaches/leeres Material für den Space-Modus")]
        private Material spaceMaterial;
    
        private MeshRenderer _meshRenderer;
        private bool lastMode = false; // false = space, true = earth
    
        void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        
            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
        
            UpdateMaterial();
        }
    
        void Update()
        {
            if (zoomController && zoomController.targetCamera)
            {
                float currentFOV = zoomController.targetCamera.fieldOfView;
                bool isEarthMode = currentFOV < fovThreshold;
            
                // Nur aktualisieren, wenn sich der Modus ändert
                if (isEarthMode != lastMode)
                {
                    // Ein einzelner Log pro Wechsel ist ausreichend
                    // Debug.Log($"Modus für {gameObject.name}: {(isEarthMode ? "Earth" : "Space")}");
                    lastMode = isEarthMode;
                    UpdateMaterial();
                }
            }
        }
        
        public bool Initialize(GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
        {
            return ApplyRandomModel(satelliteModelPrefabs, globalSpaceMaterial);
        }
    
        public void UpdateMaterial()
        {
            if (!zoomController || !zoomController.targetCamera || !_meshRenderer)
                return;
            
            bool isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
        
            if (isEarthMode)
            {
                // Earth-Modus
                if (earthModeMaterials != null && earthModeMaterials.Length > 0)
                {
                    _meshRenderer.enabled = true;
                    _meshRenderer.sharedMaterials = earthModeMaterials;
                    // Debug-Log entfernt
                }
            }
            else
            {
                // Space-Modus
                if (spaceMaterial != null)
                {
                    _meshRenderer.enabled = true;
                    Material[] spaceMaterials = new Material[1] { spaceMaterial };
                    _meshRenderer.sharedMaterials = spaceMaterials;
                    // Debug-Log entfernt
                }
                else
                {
                    _meshRenderer.enabled = false;
                }
            }
        }
        
        public bool ApplyRandomModel(GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial)
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
            {
                Debug.LogWarning("Keine Satelliten-Modelle konfiguriert!");
                return false;
            }

            int randomIndex = UnityEngine.Random.Range(0, prefabs.Length);
            prefab = prefabs[randomIndex];
            return prefab != null;
        }

        private bool TryApplyModel(GameObject modelPrefab, Material globalSpaceMaterial)
        {
            var tempModel = Instantiate(modelPrefab);
            var modelMeshFilter = tempModel.GetComponent<MeshFilter>();
            var modelMeshRenderer = tempModel.GetComponent<MeshRenderer>();
            if (modelMeshFilter == null || modelMeshRenderer == null || modelMeshFilter.sharedMesh == null)
                return false;

            CopyMeshAndMaterials(modelMeshFilter, modelMeshRenderer, globalSpaceMaterial);
            Destroy(tempModel);
            return true;
        }

        private void CopyMeshAndMaterials(MeshFilter modelMeshFilter, MeshRenderer modelMeshRenderer,
            Material globalSpaceMaterial)
        {
            var meshFilter = GetComponent<MeshFilter>() != null
                ? GetComponent<MeshFilter>()
                : gameObject.AddComponent<MeshFilter>();

            var meshRenderer = GetComponent<MeshRenderer>() != null
                ? GetComponent<MeshRenderer>()
                : gameObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = modelMeshFilter.sharedMesh;
            NormalizeSatelliteSize(modelMeshFilter.sharedMesh);

            earthModeMaterials = modelMeshRenderer.sharedMaterials;
            spaceMaterial = globalSpaceMaterial;
            meshRenderer.enabled = true;
            if (zoomController && zoomController.targetCamera)
                UpdateMaterial();
            else
                meshRenderer.materials = modelMeshRenderer.sharedMaterials;
        }

        private void NormalizeSatelliteSize(Mesh mesh)
        {
            // Zielgröße für alle Satelliten (anpassen nach Bedarf)
            float targetSize = 40000f;

            // Berechne die größte Dimension des aktuellen Meshes
            Bounds bounds = mesh.bounds;
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            // Berechne den Skalierungsfaktor
            float scaleFactor = targetSize / maxDimension;

            // Wende die Skalierung an
            gameObject.transform.localScale = Vector3.one * scaleFactor; // Semikolon hinzugefügt
        }
    }
}