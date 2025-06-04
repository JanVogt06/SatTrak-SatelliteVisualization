using UnityEngine;
using System.Collections.Generic;

namespace Satellites
{
    public class SatelliteModelController : MonoBehaviour
    {
        [Header("Referenzen")] 
        public CesiumZoomController zoomController;

        [Header("Einstellungen")] 
        [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
        public float fovThreshold = 70f;

        private GameObject _modelInstance;
        private Material _spaceMaterial;
        private bool _lastMode;
        
        // Speichere Original-Materialien pro Renderer
        private Dictionary<MeshRenderer, Material[]> _originalMaterials = new Dictionary<MeshRenderer, Material[]>();

        void Start()
        {
            if (_modelInstance != null)
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
            // Lösche altes Modell falls vorhanden
            if (_modelInstance != null)
            {
                Destroy(_modelInstance);
                _originalMaterials.Clear();
            }
            
            // Instanziiere das komplette Modell als Child
            _modelInstance = Instantiate(modelPrefab, transform);
            _modelInstance.transform.localPosition = Vector3.zero;
            _modelInstance.transform.localRotation = Quaternion.identity;
            
            // Prüfe ob es Renderer gibt
            var renderers = _modelInstance.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                Destroy(_modelInstance);
                return false;
            }
            
            // Speichere Original-Materialien
            foreach (var renderer in renderers)
            {
                _originalMaterials[renderer] = renderer.sharedMaterials;
                
                // Falls keine Materialien vorhanden, erstelle Standard-Material
                if (renderer.sharedMaterials.Length == 0 || renderer.sharedMaterials[0] == null)
                {
                    renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                    _originalMaterials[renderer] = new Material[] { renderer.sharedMaterial };
                }
            }
            
            _spaceMaterial = globalSpaceMaterial;
            
            // Skaliere das gesamte Modell
            NormalizeSatelliteSize();
            
            return true;
        }

        private void NormalizeSatelliteSize()
        {
            float targetSize = 40000f; // Erhöhe auf 100000f wenn zu klein
            
            // Berechne Bounds über alle Renderer
            var renderers = _modelInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            
            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDimension > 0)
            {
                float scaleFactor = targetSize / maxDimension;
                _modelInstance.transform.localScale = Vector3.one * scaleFactor;
            }
        }

        private void UpdateMaterial()
        {
            if (_modelInstance == null) return;
            
            var renderers = _modelInstance.GetComponentsInChildren<MeshRenderer>();
            bool isEarthMode = zoomController && zoomController.targetCamera &&
                               zoomController.targetCamera.fieldOfView < fovThreshold;

            foreach (var renderer in renderers)
            {
                if (!_originalMaterials.ContainsKey(renderer)) continue;
                
                if (isEarthMode)
                {
                    // Restore Original-Materialien
                    renderer.sharedMaterials = _originalMaterials[renderer];
                    renderer.enabled = true;
                }
                else
                {
                    // Space-Modus
                    if (_spaceMaterial != null)
                    {
                        // Ersetze alle Materialien mit Space-Material
                        var materials = new Material[renderer.sharedMaterials.Length];
                        for (int i = 0; i < materials.Length; i++)
                        {
                            materials[i] = _spaceMaterial;
                        }
                        renderer.sharedMaterials = materials;
                        renderer.enabled = true;
                    }
                    else
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (_modelInstance != null)
            {
                Destroy(_modelInstance);
            }
            _originalMaterials.Clear();
        }
    }
}