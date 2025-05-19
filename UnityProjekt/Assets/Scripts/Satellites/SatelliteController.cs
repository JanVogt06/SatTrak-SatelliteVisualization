using System;
using System.Collections.Generic;
using System.Linq;
using CesiumForUnity;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Exception;
using SGPdotNET.Propagation;
using SGPdotNET.TLE;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Satellites
{
    public class SatelliteController : MonoBehaviour
    {
        public Sgp4 OrbitPropagator;
        public Renderer Renderer { get; private set; }

        public void Initialize(Tle tle, CesiumGeoreference cesiumGeoreference)
        {
            Renderer = GetComponent<Renderer>();
            OrbitPropagator = new Sgp4(tle);
            var pos = OrbitPropagator.FindPosition(DateTime.Now).ToSphericalEcef();
            var position = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(new double3(pos.X * 1000, pos.Y * 1000,
                pos.Z * 1000));
            transform.position = new Vector3((float)position.x, (float)position.y, (float)position.z);
        }
    }
}
