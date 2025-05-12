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
    
    private MeshRenderer meshRenderer;
    private Material[] originalMaterials;
    private bool lastMode = false; // false = space, true = earth
    
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        Debug.Log("SatelliteMaterialController: Start - MeshRenderer gefunden: " + (meshRenderer != null));
        
        if (meshRenderer)
        {
            originalMaterials = meshRenderer.sharedMaterials;
            Debug.Log("Original-Materialien: " + originalMaterials.Length);
            UpdateMaterial();
        }
    }
    
    void Update()
    {
        if (zoomController && zoomController.targetCamera)
        {
            float currentFOV = zoomController.targetCamera.fieldOfView;
            bool isEarthMode = currentFOV < fovThreshold;
            
            // Nur loggen wenn sich der Modus ändert
            if (isEarthMode != lastMode)
            {
                Debug.Log("Modus geändert: " + (isEarthMode ? "Earth" : "Space") + 
                          " (FOV: " + currentFOV + ", Threshold: " + fovThreshold + ")");
                lastMode = isEarthMode;
            }
            
            UpdateMaterial();
        }
        else
        {
            Debug.LogError("ZoomController oder Camera nicht gefunden!");
        }
    }
    
    void UpdateMaterial()
    {
        if (!zoomController || !zoomController.targetCamera || !meshRenderer)
            return;
            
        bool isEarthMode = zoomController.targetCamera.fieldOfView < fovThreshold;
        
        if (isEarthMode)
        {
            // Earth-Modus
            if (earthModeMaterials != null && earthModeMaterials.Length > 0)
            {
                meshRenderer.enabled = true;
                meshRenderer.materials = earthModeMaterials;
            }
        }
        else
        {
            // Space-Modus
            if (spaceMaterial != null)
            {
                meshRenderer.enabled = true;
                Material[] spaceMaterials = new Material[1];
                spaceMaterials[0] = spaceMaterial;
                meshRenderer.materials = spaceMaterials;
            }
            else
            {
                // Einfach ausblenden wenn kein Space-Material definiert ist
                meshRenderer.enabled = false;
            }
        }
    }
}