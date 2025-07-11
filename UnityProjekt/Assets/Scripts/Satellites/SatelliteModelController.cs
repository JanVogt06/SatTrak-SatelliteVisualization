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

        [Header("Space Mode")]
        [Tooltip("Größe der Kugel im Space-Modus")]
        public float sphereSize = 20000f;

        private GameObject _modelInstance;
        private GameObject _spaceSphere;
        private Material _spaceMaterial;
        private bool _lastMode;

        private bool _isISS;
        private bool _isSpecial;

        [Header("Highlight")]
        public Material highlightMaterial;         

        private GameObject _highlightShell;        

        void Start()
        {
            // Erstelle Space Sphere NICHT in Start, sondern erst wenn das Modell gesetzt wird
            if (_modelInstance != null)
                UpdateVisibility();
        }

        void Update()
        {
            if (!zoomController || !zoomController.targetCamera) return;
            bool isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
            if (isEarthMode == _lastMode) return;
            _lastMode = isEarthMode;
            UpdateVisibility();
        }

        public void SetHighlight(bool state)
        {
            // bei erstem Aufruf Hülle erzeugen
            if (_highlightShell == null) CreateHighlightShell();
            _highlightShell.SetActive(state);
        }

        private void CreateHighlightShell()
        {
            _highlightShell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _highlightShell.name = "HighlightShell";
            _highlightShell.transform.SetParent(transform, false);
            Destroy(_highlightShell.GetComponent<Collider>());

            // fester Durchmesser 10 × 10 × 10
            _highlightShell.transform.localScale = Vector3.one * 10f;

            var mr = _highlightShell.GetComponent<MeshRenderer>();
            mr.sharedMaterial = highlightMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.material.renderQueue = 3100;   // immer im Vordergrund

            _highlightShell.SetActive(false);
        }

        public bool SetModel(GameObject[] satelliteModelPrefabs, Material globalSpaceMaterial, 
                            bool isSpecial = false, GameObject specialModelPrefab = null)
        {
            _isSpecial = isSpecial;
            _isISS = false; // Wird später gesetzt wenn nötig

            GameObject modelToUse;

            // Wenn es ein spezielles Modell gibt
            if (_isSpecial && specialModelPrefab != null)
            {
                modelToUse = specialModelPrefab;
                
                // Prüfen ob es die ISS ist für Space-Mode Hervorhebung
                // Versuche verschiedene Wege, die Satellite-Komponente zu finden
                var satellite = GetComponent<Satellite>();
                if (satellite == null)
                    satellite = GetComponentInParent<Satellite>();
                if (satellite == null && transform.parent != null)
                    satellite = transform.parent.GetComponent<Satellite>();
                    
                if (satellite != null)
                {
                    if (satellite.IsISS)
                    {
                        _isISS = true;
                    }
                }
            }
            else
            {
                // Sonst zufälliges Modell
                if (!TryGetRandomModelPrefab(satelliteModelPrefabs, out modelToUse))
                    return false;
            }

            if (!TryApplyModel(modelToUse, globalSpaceMaterial))
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

            _spaceMaterial = globalSpaceMaterial;

            // Skaliere das gesamte Modell
            NormalizeSatelliteSize();

            // Erstelle/Update Space Sphere - WICHTIG: Nach dem _isISS gesetzt wurde!
            CreateOrUpdateSpaceSphere();

            return true;
        }

        private void CreateOrUpdateSpaceSphere()
        {
            // Wenn schon vorhanden, lösche alte Sphere
            if (_spaceSphere != null)
            {
                Destroy(_spaceSphere);
            }
            
            // Erstelle neue Space Sphere
            _spaceSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _spaceSphere.name = "SpaceSphere";
            _spaceSphere.transform.SetParent(transform);
            _spaceSphere.transform.localPosition = Vector3.zero;

            // NUR ISS größer machen
            float size = _isISS ? 50f : 5f;
            _spaceSphere.transform.localScale = Vector3.one * size;

            // Entferne Collider für Performance
            var collider = _spaceSphere.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            // Material setzen
            var renderer = _spaceSphere.GetComponent<MeshRenderer>();
            if (_isISS)  // NUR ISS bekommt gelbe Farbe
            {
                // Erstelle ein neues Material für die ISS
                Material issMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                issMaterial.color = Color.yellow;
                issMaterial.EnableKeyword("_EMISSION");
                issMaterial.SetColor("_EmissionColor", Color.yellow * 0.5f); // Leuchten
                renderer.sharedMaterial = issMaterial;
            }
            else if (_spaceMaterial != null)
            {
                renderer.sharedMaterial = _spaceMaterial;
            }
            else
            {
                // Fallback Material
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            }
            
            // Initial verstecken oder zeigen basierend auf aktuellem Modus
            bool isEarthMode = zoomController && zoomController.targetCamera &&
                               zoomController.targetCamera.fieldOfView < fovThreshold;
            _spaceSphere.SetActive(!isEarthMode);
        }

        private void CreateSpaceSphere()
        {
            CreateOrUpdateSpaceSphere();
        }

        private void NormalizeSatelliteSize()
        {        
            // ISS und Famous Satellites bekommen gleiche Größe
            float targetSize = (_isISS || _isSpecial) ? 100000f : 40000f;

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

        private void UpdateVisibility()
        {
            if (_modelInstance == null || _spaceSphere == null) return;

            bool isEarthMode = zoomController && zoomController.targetCamera &&
                               zoomController.targetCamera.fieldOfView < fovThreshold;

            // Einfach GameObject an/aus schalten
            _modelInstance.SetActive(isEarthMode);
            _spaceSphere.SetActive(!isEarthMode);
        }

        void OnDestroy()
        {
            if (_modelInstance != null)
                Destroy(_modelInstance);
            if (_spaceSphere != null)
                Destroy(_spaceSphere);
        }
    }
}