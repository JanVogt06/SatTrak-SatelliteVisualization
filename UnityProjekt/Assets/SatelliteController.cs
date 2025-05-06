using System;
using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using Unity.Mathematics;
using UnityEngine;

public class SatelliteController : MonoBehaviour
{
    private Sgp4 _orbitPropagator;

    private CesiumGlobeAnchor anchor;

    void Awake()
    {
        anchor = GetComponent<CesiumGlobeAnchor>();
    }

    public void Initialize(Sgp4 propagator)
    {
        _orbitPropagator = propagator;
        StartCoroutine(UpdateSatellitesCoroutine());
    }
    
    private IEnumerator UpdateSatellitesCoroutine()
    {
        while (true)
        {
            if (_orbitPropagator == null) yield return new WaitForSeconds(1);

            var result = _orbitPropagator.FindPosition(DateTime.UtcNow);
            var pos = result.ToGeodetic();
            anchor.longitudeLatitudeHeight = new double3(pos.Longitude.Degrees, pos.Latitude.Degrees, pos.Altitude * 3);
            yield return new WaitForSeconds(1);
        }
    }
}
