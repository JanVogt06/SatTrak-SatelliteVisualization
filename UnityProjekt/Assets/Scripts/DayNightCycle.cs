using System;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine.Serialization;

public class DayNightCycle : MonoBehaviour
{
    [Header("Sun Light")]
    [Tooltip("Das Directional Light das die Sonne repräsentiert")]
    public Light sunLight;
    
    [Header("Visual Settings")]
    [Tooltip("Intensität des Sonnenlichts am Tag")]
    public float dayIntensity = 25f;
    
    [Tooltip("Intensität des Sonnenlichts in der Nacht")]
    public float nightIntensity = 5f;
    
    [Tooltip("Farbe des Sonnenlichts am Tag")]
    public Color dayColor = new Color(1f, 0.95f, 0.8f);
    
    [Tooltip("Farbe des Sonnenlichts in der Nacht")]
    public Color nightColor = new Color(0.3f, 0.4f, 0.7f);
    
    [Header("Atmosphere")]
    [Tooltip("Ambient Light für Tag")]
    public Color dayAmbient = new Color(0.8f, 0.9f, 1f); 
    
    [Tooltip("Ambient Light für Nacht")]
    public Color nightAmbient = new Color(0.3f, 0.3f, 0.5f);
    
    [Header("References")] 
    public CesiumGeoreference georeference;
    public TimeSlider.TimeSlider time;
    
    // Private Variablen
    private Vector3 earthCenter;
    
    void Start()
    {
        if (sunLight == null)
        {
            // Finde oder erstelle Sun Light
            sunLight = FindObjectOfType<Light>();
            if (sunLight == null || sunLight.type != LightType.Directional)
            {
                GameObject sunGO = new GameObject("Sun Light");
                sunLight = sunGO.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }
            
            // Verstärke das globale Licht
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.3f, 0.4f);
            RenderSettings.ambientIntensity = 1.5f;
        }
        
        // Finde Erdmittelpunkt
        if (georeference != null)
        {
            var ecef = georeference.TransformEarthCenteredEarthFixedPositionToUnity(new Unity.Mathematics.double3(0, 0, 0));
            earthCenter = new Vector3((float)ecef.x, (float)ecef.y, (float)ecef.z);
        }
    }
    
    void Update()
    {
        UpdateSunPosition();
        UpdateLighting();
    }
    
    void UpdateSunPosition()
    {
        // Berechne Sonnenposition basierend auf Zeit und Jahreszeit
        float hourAngle = (time.CurrentSimulatedTime.Hour - 12f) * 15f; // 15 Grad pro Stunde
        
        // Deklination der Sonne (vereinfacht)
        float declination = 23.45f * Mathf.Sin((360f * (time.CurrentSimulatedTime.DayOfYear - 81f) / 365f) * Mathf.Deg2Rad);
        
        // Setze Sonnenrichtung
        Quaternion hourRotation = Quaternion.Euler(0, -hourAngle, 0);
        Quaternion declinationRotation = Quaternion.Euler(-declination, 0, 0);
        
        sunLight.transform.rotation = hourRotation * declinationRotation;
        
        // Richte die Sonne zum Erdmittelpunkt aus (falls Georeference vorhanden)
        if (georeference != null)
        {
            Vector3 sunDirection = sunLight.transform.forward;
            sunLight.transform.position = earthCenter - sunDirection * 150000000f; // 150 Millionen km (AU)
            sunLight.transform.LookAt(earthCenter);
        }
    }
    
    void UpdateLighting()
    {
        // Berechne wie viel "Tag" es ist (0 = Nacht, 1 = Tag)
        float sunAngle = Vector3.Dot(sunLight.transform.forward, Vector3.down);
        float dayAmount = Mathf.Clamp01((sunAngle + 0.3f) / 0.6f); // Smooth transition
        
        // Interpoliere Licht-Intensität
        sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dayAmount);
        
        // Interpoliere Licht-Farbe
        sunLight.color = Color.Lerp(nightColor, dayColor, dayAmount);
        
        // Setze Ambient Light
        RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, dayAmount);
        
        // Optional: Fog für Atmosphäre
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(nightAmbient * 0.5f, dayAmbient * 0.8f, dayAmount);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.00001f;
    }
    
    // Hilfsmethoden für externe Scripte
    public bool IsDay()
    {
        return time.CurrentSimulatedTime.Hour >= 6f && time.CurrentSimulatedTime.Hour <= 18f;
    }
    
    public float GetDayProgress()
    {
        return time.CurrentSimulatedTime.Hour / 24f;
    }
}