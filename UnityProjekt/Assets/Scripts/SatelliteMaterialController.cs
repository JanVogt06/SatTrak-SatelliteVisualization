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
        Debug.Log("SatelliteMaterialController: Start - MeshRenderer gefunden: " + (_meshRenderer != null));
        
        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            Debug.Log("MeshRenderer wurde hinzugefügt");
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
                Debug.Log("Modus geändert: " + (isEarthMode ? "Earth" : "Space") + 
                          " (FOV: " + currentFOV + ", Threshold: " + fovThreshold + ")");
                lastMode = isEarthMode;
                UpdateMaterial();
            }
        }
    }
    
    public void UpdateMaterial()
    {
        if (!zoomController || !zoomController.targetCamera || !_meshRenderer)
        {
            Debug.LogError("Fehlende Komponenten für UpdateMaterial");
            return;
        }
            
        bool isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
        
        if (isEarthMode)
        {
            // Earth-Modus
            if (earthModeMaterials != null && earthModeMaterials.Length > 0)
            {
                _meshRenderer.enabled = true;
                _meshRenderer.sharedMaterials = earthModeMaterials;
                Debug.Log($"Earth-Materialien angewendet auf {gameObject.name} (Anzahl: {earthModeMaterials.Length})");
            }
            else
            {
                Debug.LogError($"Keine Earth-Materialien für {gameObject.name}");
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
                Debug.Log($"Space-Material angewendet: {spaceMaterial.name} auf {gameObject.name}");
            }
            else
            {
                Debug.LogError($"Kein Space-Material für {gameObject.name}");
                _meshRenderer.enabled = false;
            }
        }
    }
}