using System;
using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class SatelliteController : MonoBehaviour
{
    public Sgp4 OrbitPropagator;
    public Tle Tle;
    public Renderer Renderer { get; private set; }
    public CesiumGlobeAnchor Anchor { get; private set; }

    void Awake()
    {
        
    }

    public void Initialize(Tle tle)
    {
        Anchor = GetComponent<CesiumGlobeAnchor>();
        Renderer = GetComponent<Renderer>();
        Tle = tle;
        OrbitPropagator = new Sgp4(tle);
    }
}
