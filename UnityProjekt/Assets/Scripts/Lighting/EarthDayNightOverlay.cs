using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;

public class EarthDayNightOverlay : MonoBehaviour
{
    [Header("References")]
    public CesiumGeoreference georeference;
    public DayNightSystem dayNightSystem;
    
    [Header("Overlay Settings")]
    public Material overlayMaterial;
    [Range(0f, 1f)]
    public float shadowStrength = 0.9f;
    [Range(0.01f, 0.5f)]
    public float terminatorSoftness = 0.5f;
    
    [Header("Sphere Settings")]
    [Tooltip("Skalierungsfaktor für die Schattenkugel")]
    public float sphereScale = 1.05f; // Etwas größer als die Erde
    
    private GameObject shadowSphere;
    private Renderer sphereRenderer;
    
    void Start()
    {
        if (overlayMaterial == null)
        {
            Debug.LogError("EarthDayNightOverlay: Kein Material zugewiesen! Bitte DayNightOverlayMaterial zuweisen.");
            enabled = false;
            return;
        }
        
        CreateShadowSphere();
    }
    
    void CreateShadowSphere()
    {
        // Erstelle eine Kugel
        shadowSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shadowSphere.name = "Earth Shadow Overlay";
        shadowSphere.transform.SetParent(transform);
        
        // Position im Erdmittelpunkt
        if (georeference != null)
        {
            // Setze Position auf Erdmittelpunkt im Unity-Koordinatensystem
            var earthCenter = georeference.TransformEarthCenteredEarthFixedPositionToUnity(new double3(0, 0, 0));
            shadowSphere.transform.position = new Vector3((float)earthCenter.x, (float)earthCenter.y, (float)earthCenter.z);
        }
        else
        {
            shadowSphere.transform.position = Vector3.zero;
        }
        
        // Skaliere auf Erdgröße (Radius ~6371 km)
        float earthRadiusMeters = 6371000f * sphereScale;
        shadowSphere.transform.localScale = Vector3.one * earthRadiusMeters * 2f;
        
        // Hole Renderer
        sphereRenderer = shadowSphere.GetComponent<Renderer>();
        
        // Wende Material an
        sphereRenderer.material = overlayMaterial;
        sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sphereRenderer.receiveShadows = false;
        
        // Deaktiviere Collider
        Destroy(shadowSphere.GetComponent<Collider>());
        
        // Setze initiale Shader-Werte
        UpdateShaderProperties();
    }
    
    void Update()
    {
        if (dayNightSystem == null || sphereRenderer == null) return;
        
        // Update Position falls Georeference sich bewegt
        if (georeference != null)
        {
            var earthCenter = georeference.TransformEarthCenteredEarthFixedPositionToUnity(new double3(0, 0, 0));
            shadowSphere.transform.position = new Vector3((float)earthCenter.x, (float)earthCenter.y, (float)earthCenter.z);
        }
        
        UpdateShaderProperties();
    }
    
    void UpdateShaderProperties()
    {
        if (sphereRenderer == null || sphereRenderer.material == null) return;
        
        // Hole Sonnenrichtung
        Vector3 sunDirection = -dayNightSystem.sunLight.transform.forward;
        
        // Setze Shader-Properties
        sphereRenderer.material.SetVector("_SunDirection", sunDirection);
        sphereRenderer.material.SetFloat("_ShadowStrength", shadowStrength);
        sphereRenderer.material.SetFloat("_TerminatorSoftness", terminatorSoftness);
    }
    
    // Hilfsmethoden für Runtime-Anpassungen
    public void SetShadowStrength(float strength)
    {
        shadowStrength = Mathf.Clamp01(strength);
        UpdateShaderProperties();
    }
    
    public void SetTerminatorSoftness(float softness)
    {
        terminatorSoftness = Mathf.Clamp(softness, 0.01f, 0.5f);
        UpdateShaderProperties();
    }
    
    void OnDestroy()
    {
        if (shadowSphere != null)
            Destroy(shadowSphere);
    }
    
    // Debug-Visualisierung
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (dayNightSystem != null && dayNightSystem.sunLight != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 sunDir = -dayNightSystem.sunLight.transform.forward;
            
            if (shadowSphere != null)
            {
                // Zeige Sonnenrichtung
                Gizmos.DrawRay(shadowSphere.transform.position, sunDir * 10000000);
                
                // Zeige Terminator-Linie
                Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
                Vector3 perpendicular = Vector3.Cross(sunDir, Vector3.up).normalized;
                if (perpendicular.magnitude < 0.1f)
                    perpendicular = Vector3.Cross(sunDir, Vector3.right).normalized;
                    
                for (int i = 0; i < 360; i += 10)
                {
                    Quaternion rotation = Quaternion.AngleAxis(i, sunDir);
                    Vector3 point = rotation * perpendicular * shadowSphere.transform.localScale.x * 0.5f;
                    Gizmos.DrawLine(
                        shadowSphere.transform.position + point * 0.99f,
                        shadowSphere.transform.position + point * 1.01f
                    );
                }
            }
        }
    }
}