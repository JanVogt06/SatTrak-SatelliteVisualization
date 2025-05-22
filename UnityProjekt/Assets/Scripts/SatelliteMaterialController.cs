using UnityEngine;
using CesiumForUnity;

public class SatelliteMaterialController : MonoBehaviour
{
    [Header("Referenzen")]
    public CesiumZoomController zoomController;
    
    [Header("Materials")]
    [Tooltip("Das komplexe Material, das nur im Earth-Modus verwendet wird")]
    public Material[] earthModeMaterials;
    
    [Tooltip("Einfaches/leeres Material für den Space-Modus")]
    public Material spaceMaterial;
    
    [Header("Einstellungen")]
    [Tooltip("FOV-Schwellenwert zum Umschalten zwischen den Modi")]
    public float fovThreshold = 70f;
    
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
}