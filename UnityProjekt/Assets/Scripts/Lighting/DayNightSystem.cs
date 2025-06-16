using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.UI;

public class DayNightSystem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Das Directional Light, das als Sonne fungiert")]
    public Light sunLight;
    
    [Tooltip("Referenz zum TimeSlider für die aktuelle Zeit")]
    public TimeSlider.TimeSlider timeSlider;
    
    [Tooltip("Optional: Material der Erde für Shader-Effekte")]
    public Material earthMaterial;
    
    [Header("Sun Settings")]
    [Tooltip("Intensität des Sonnenlichts")]
    public float sunIntensity = 1.3f;
    
    [Tooltip("Farbe des Sonnenlichts")]
    public Color sunColor = new Color(1f, 0.95f, 0.8f);
    
    [Header("Ambient Settings")]
    [Tooltip("Ambiente Beleuchtung bei Tag")]
    public Color dayAmbientColor = new Color(0.5f, 0.6f, 0.7f);
    
    [Tooltip("Ambiente Beleuchtung bei Nacht")]
    public Color nightAmbientColor = new Color(0.05f, 0.05f, 0.1f);
    
    [Header("Visual Effects")]
    [Tooltip("Zeigt einen visuellen Terminator (Tag/Nacht-Grenze)")]
    public bool showTerminator = true;
    
    [Tooltip("GameObject für den Terminator-Effekt")]
    public GameObject terminatorPlane;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Astronomische Konstanten
    private const float OBLIQUITY = 23.44f; // Neigung der Erdachse in Grad
    private const float DAYS_PER_YEAR = 365.25f;
    
    void Start()
    {
        // Falls nicht zugewiesen, versuche Komponenten zu finden
        if (sunLight == null)
            sunLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            
        if (timeSlider == null)
            timeSlider = FindObjectOfType<TimeSlider.TimeSlider>();
            
        if (sunLight == null || timeSlider == null)
        {
            Debug.LogError("DayNightSystem: Fehlende Referenzen!");
            enabled = false;
            return;
        }
        
        // Erstelle Terminator-Visualisierung falls gewünscht
        if (showTerminator && terminatorPlane == null)
            CreateTerminatorPlane();
    }
    
    void Update()
    {
        if (timeSlider == null) return;
        
        // Berechne Sonnenposition basierend auf der simulierten Zeit
        Vector3 sunDirection = CalculateSunDirection(timeSlider.CurrentSimulatedTime);
        
        // Setze die Lichtrichtung (Sonne scheint in die negative Richtung)
        sunLight.transform.rotation = Quaternion.LookRotation(-sunDirection);
        
        // Setze Lichtintensität und Farbe
        sunLight.intensity = sunIntensity;
        sunLight.color = sunColor;
        
        // Update Ambiente Beleuchtung basierend auf lokaler "Tageszeit"
        UpdateAmbientLighting(sunDirection);
        
        // Update Shader falls vorhanden
        if (earthMaterial != null)
        {
            earthMaterial.SetVector("_SunDirection", sunDirection);
            earthMaterial.SetFloat("_DayNightBlend", 0.1f); // Weicher Übergang
        }
        
        // Update Terminator-Visualisierung
        if (showTerminator && terminatorPlane != null)
            UpdateTerminator(sunDirection);
            
        // Debug-Anzeige
        if (showDebugInfo)
            ShowDebugInfo(sunDirection);
    }
    
    Vector3 CalculateSunDirection(DateTime currentTime)
    {
        // Berechne Tage seit Frühlingsäquinoktium (21. März)
        DateTime equinox = new DateTime(currentTime.Year, 3, 21);
        double daysSinceEquinox = (currentTime - equinox).TotalDays;
        
        // Berechne Position der Sonne in der Ekliptik
        double eclipticLongitude = (360.0 / DAYS_PER_YEAR) * daysSinceEquinox;
        double eclipticLongitudeRad = eclipticLongitude * Mathf.Deg2Rad;
        
        // Berechne Deklination der Sonne (vereinfachte Formel)
        double declination = OBLIQUITY * Math.Sin(eclipticLongitudeRad);
        double declinationRad = declination * Mathf.Deg2Rad;
        
        // Berechne Stundenwinkel basierend auf der Tageszeit
        double hourAngle = (currentTime.TimeOfDay.TotalHours - 12.0) * 15.0; // 15° pro Stunde
        double hourAngleRad = hourAngle * Mathf.Deg2Rad;
        
        // Konvertiere zu kartesischen Koordinaten
        // X: Ost-West (positiv = Ost)
        // Y: Oben-Unten (positiv = Norden)
        // Z: Nord-Süd (positiv = Süd)
        float x = (float)(Math.Cos(declinationRad) * Math.Sin(hourAngleRad));
        float y = (float)(Math.Sin(declinationRad));
        float z = (float)(Math.Cos(declinationRad) * Math.Cos(hourAngleRad));
        
        return new Vector3(x, y, z).normalized;
    }
    
    void UpdateAmbientLighting(Vector3 sunDirection)
    {
        // Berechne wie "hoch" die Sonne steht (1 = Zenit, -1 = Nadir)
        float sunHeight = Vector3.Dot(sunDirection, Vector3.up);
        
        // Mappe auf 0-1 Bereich (0 = Nacht, 1 = Tag)
        float dayAmount = Mathf.Clamp01((sunHeight + 0.2f) / 0.4f);
        
        // Interpoliere zwischen Nacht- und Tagfarben
        RenderSettings.ambientLight = Color.Lerp(nightAmbientColor, dayAmbientColor, dayAmount);
        
        // Optional: Passe auch Nebel an
        RenderSettings.fogColor = Color.Lerp(
            nightAmbientColor * 0.5f, 
            dayAmbientColor * 0.8f, 
            dayAmount
        );
    }
    
    void CreateTerminatorPlane()
    {
        // Erstelle eine Ebene die den Terminator visualisiert
        terminatorPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        terminatorPlane.name = "Terminator";
        
        // Skaliere auf Erdgröße (ca. 12.000 km Durchmesser)
        terminatorPlane.transform.localScale = new Vector3(15000000, 15000000, 1);
        
        // Erstelle ein semi-transparentes Material
        Material terminatorMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        terminatorMat.color = new Color(0, 0, 0, 0.3f); // Halbtransparentes Schwarz
        terminatorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        terminatorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        terminatorMat.SetInt("_ZWrite", 0);
        terminatorMat.renderQueue = 3000;
        
        terminatorPlane.GetComponent<Renderer>().material = terminatorMat;
        
        // Deaktiviere Schatten
        terminatorPlane.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        // Entferne Collider
        Destroy(terminatorPlane.GetComponent<Collider>());
    }
    
    void UpdateTerminator(Vector3 sunDirection)
    {
        if (terminatorPlane == null) return;
        
        // Positioniere die Ebene im Erdmittelpunkt
        terminatorPlane.transform.position = Vector3.zero;
        
        // Rotiere die Ebene so, dass sie senkrecht zur Sonne steht
        // Die Normale der Ebene zeigt zur Sonne
        terminatorPlane.transform.rotation = Quaternion.LookRotation(sunDirection, Vector3.up);
        
        // Verschiebe die Ebene leicht in Richtung Sonne, um nur die Nachtseite abzudecken
        terminatorPlane.transform.position += sunDirection * 1000; // 1000m Offset
    }
    
    void ShowDebugInfo(Vector3 sunDirection)
    {
        // Zeichne Sonnenrichtung
        Debug.DrawRay(Vector3.zero, sunDirection * 10000000, Color.yellow);
        
        // Zeige Info in der Konsole
        DateTime current = timeSlider.CurrentSimulatedTime;
        Debug.Log($"Zeit: {current:yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"Sonnenrichtung: {sunDirection}");
        Debug.Log($"Sonnenhöhe: {Vector3.Dot(sunDirection, Vector3.up):F2}");
    }
    
    // Hilfsmethode: Berechne lokale Sonnenposition für einen Ort
    public float GetLocalSunElevation(double latitude, double longitude, DateTime time)
    {
        Vector3 sunDir = CalculateSunDirection(time);
        
        // Konvertiere geographische Koordinaten zu Vektor
        float latRad = (float)(latitude * Mathf.Deg2Rad);
        float lonRad = (float)(longitude * Mathf.Deg2Rad);
        
        Vector3 locationVector = new Vector3(
            Mathf.Cos(latRad) * Mathf.Cos(lonRad),
            Mathf.Sin(latRad),
            Mathf.Cos(latRad) * Mathf.Sin(lonRad)
        );
        
        // Berechne Winkel zwischen Standort und Sonne
        float elevation = Vector3.Dot(locationVector, sunDir);
        return Mathf.Asin(elevation) * Mathf.Rad2Deg;
    }
    
    // Hilfsmethode: Ist es Tag an einem bestimmten Ort?
    public bool IsDay(double latitude, double longitude, DateTime time)
    {
        return GetLocalSunElevation(latitude, longitude, time) > -6f; // -6° = Bürgerliche Dämmerung
    }
}