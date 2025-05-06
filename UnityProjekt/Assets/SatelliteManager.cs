using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using CesiumForUnity;
using Newtonsoft.Json;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using Unity.Mathematics;

public class SatelliteManager : MonoBehaviour
{
    [Header("TLE Source")]
    public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?GROUP=active&FORMAT=TLE";

    [Header("Prefabs & References")]
    public GameObject satellitePrefab;
    public Transform orbitParent;
    public CesiumGeoreference cesiumGeoreference;

    [Header("UI")]
    // public InputField filterInput;
    public RectTransform listContainer;
    public GameObject listItemPrefab;


    private readonly Dictionary<string, string> satellites = new();

    void Start()
    {

        // if (filterInput != null)
        // {
        //     filterInput.onValueChanged.AddListener(OnFilterChanged);
        // }
        // else
        // {
        //     Debug.LogWarning("Filter InputField reference missing.");
        // }

        StartCoroutine(FetchTleData());
    }

    IEnumerator FetchTleData()
    {
        using UnityWebRequest www = UnityWebRequest.Get(tleUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API-Fehler: " + www.error);
        }
        else
        {
            try
            {
                var lines = www.downloadHandler.text.Split("\r\n");
                
                var tles = new List<Tle>();
                for (int i = 0; i < lines.Length; i+=3)
                {
                    try
                    {
                        var line = lines[i..(i + 3)];
                        var tle = Tle.ParseElements(line, true);
                        tles.Add(tle[0]);
                    }
                    catch (Exception e)
                    {
                        // ignored
                    }
                }

                foreach (var tle in tles)
                {
                    GameObject sat = Instantiate(satellitePrefab, cesiumGeoreference.transform);
                    sat.name = tle.NoradNumber + " " + tle.Name;
                    sat.GetComponent<SatelliteController>().Initialize(new Sgp4(tle));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Parsing-Fehler: " + e.Message);
            }
        }
    }

    // void AddSatellite(string name, string tle1, string tle2)
    // {
    //     if (satellites.ContainsKey(name)) return;
    //
    //     try
    //     {
    //         // Container for each satellite
    //         GameObject groupGO = new GameObject(name + "_Group");
    //         groupGO.transform.SetParent(orbitParent);
    //
    //         // Orbit LineRenderer
    //         GameObject lineGO = new GameObject("OrbitLine");
    //         lineGO.transform.SetParent(groupGO.transform);
    //         LineRenderer lr = lineGO.AddComponent<LineRenderer>();
    //         lr.positionCount = 180;
    //         lr.widthMultiplier = 0.02f;
    //         lr.material = new Material(Shader.Find("Sprites/Default"));
    //         lr.startColor = lr.endColor = Color.cyan;
    //
    //         // Satellite Object
    //         if (satellitePrefab != null)
    //         {
    //             GameObject satGO = Instantiate(satellitePrefab, Vector3.zero, Quaternion.identity, groupGO.transform);
    //             satGO.name = name;
    //
    //             // UI List Item
    //             if (listItemPrefab != null && listContainer != null)
    //             {
    //                 GameObject listItem = Instantiate(listItemPrefab, listContainer);
    //                 Toggle toggle = listItem.GetComponentInChildren<Toggle>();
    //                 Text label = listItem.GetComponentInChildren<Text>();
    //                 if (label != null) label.text = name;
    //                 if (toggle != null)
    //                 {
    //                     toggle.isOn = true;
    //                     toggle.onValueChanged.AddListener(visible => groupGO.SetActive(visible));
    //                 }
    //
    //                 // Create satellite entry
    //                 var tle = new Tle(name, tle1, tle2);
    //                 var sgp4 = new Sgp4(tle);
    //
    //                 satellites[name] = new SatelliteGroup
    //                 {
    //                     Propagator = sgp4,
    //                     Tle = tle,
    //                     SatelliteObject = satGO,
    //                     OrbitLine = lr,
    //                     GroupObject = groupGO,
    //                     ListItem = listItem
    //                 };
    //             }
    //             else
    //             {
    //                 Debug.LogError("ListItemPrefab or ListContainer reference missing");
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogError("SatellitePrefab reference missing");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.LogError($"Error adding satellite {name}: {ex.Message}");
    //     }
    // }
    //
    // IEnumerator UpdateSatellitesLoop()
    // {
    //     while (true)
    //     {
    //         DateTime nowUtc = DateTime.UtcNow;
    //
    //         foreach (var sat in satellites.Values)
    //         {
    //             if (sat.SatelliteObject == null || sat.Propagator == null) continue;
    //
    //             try
    //             {
    //                 // Get satellite position
    //                 var eci = sat.Propagator.FindPosition(nowUtc);
    //                 var ecef = new double3(eci.Position.X, eci.Position.Y, eci.Position.Z);
    //
    //                 double3 unityD = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
    //                 Vector3 unityPos = new Vector3((float)unityD.x, (float)unityD.y, (float)unityD.z);
    //                 sat.SatelliteObject.transform.position = unityPos;
    //
    //                 UpdateOrbitLine(sat, nowUtc);
    //             }
    //             catch (Exception ex)
    //             {
    //                 Debug.LogWarning($"Error updating satellite position: {ex.Message}");
    //             }
    //         }
    //
    //         yield return new WaitForSeconds(1f);
    //     }
    // }
    //
    // void UpdateOrbitLine(SatelliteGroup sat, DateTime referenceTime)
    // {
    //     if (sat.OrbitLine == null || sat.Propagator == null) return;
    //
    //     try
    //     {
    //         int segments = sat.OrbitLine.positionCount;
    //         // Calculate orbital period from the propagator
    //         double periodMinutes = CalculateOrbitalPeriod(sat);
    //
    //         for (int i = 0; i < segments; i++)
    //         {
    //             DateTime sampleTime = referenceTime.AddMinutes(periodMinutes * i / segments);
    //             var eci = sat.Propagator.FindPosition(sampleTime);
    //             var ecef = new double3(eci.Position.X, eci.Position.Y, eci.Position.Z);
    //
    //             double3 unityD = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
    //             sat.OrbitLine.SetPosition(i, new Vector3((float)unityD.x, (float)unityD.y, (float)unityD.z));
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Debug.LogWarning($"Error updating orbit line: {ex.Message}");
    //     }
    // }
    //
    // double CalculateOrbitalPeriod(SatelliteGroup sat)
    // {
    //     // Calculate orbital period in minutes
    //     // Using the formula: T = 2π * sqrt(a³/μ)
    //     // Where a is the semi-major axis and μ is Earth's gravitational parameter
    //     
    //     // Get mean motion in radians per minute
    //     double meanMotionRadPerMin = sat.Tle.MeanMotionRevPerDay / 60;
    //     
    //     // Calculate semi-major axis (a) in km
    //     double earthGravitationalParameter = 3.986004418e5; // km³/s²
    //     double a = Math.Pow(earthGravitationalParameter / Math.Pow(meanMotionRadPerMin / 60.0, 2), 1.0/3.0);
    //     
    //     // Calculate period in minutes
    //     return 2 * Math.PI * Math.Sqrt(Math.Pow(a, 3) / earthGravitationalParameter) / 60.0;
    // }
    //
    // void OnFilterChanged(string input)
    // {
    //     foreach (var (name, sat) in satellites)
    //     {
    //         bool visible = string.IsNullOrWhiteSpace(input) || name.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;
    //         if (sat.GroupObject != null) sat.GroupObject.SetActive(visible);
    //         if (sat.ListItem != null) sat.ListItem.SetActive(visible);
    //     }
    // }
    //
    // class SatelliteGroup
    // {
    //     public Sgp4 Propagator;
    //     public Tle Tle;
    //     public GameObject SatelliteObject;
    //     public LineRenderer OrbitLine;
    //     public GameObject GroupObject;
    //     public GameObject ListItem;
    // }
}