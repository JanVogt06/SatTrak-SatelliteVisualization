using System;
using System.Collections.Generic;
using CesiumForUnity;
using DefaultNamespace;
using Unity.Mathematics;
using UnityEngine;
using Satellites.SGP;
using Satellites.SGP.Propagation;
using Satellites.SGP.TLE;

namespace Satellites
{
    public class SatelliteController : MonoBehaviour
    {
        public Sgp4 OrbitPropagator;
        public Tle Tle;
        public bool ShouldCalculateOrbit;
        private GameObject orbitGO;
        private LineRenderer orbitRenderer;

        public void Initialize(Tle tle)
        {
            OrbitPropagator = new Sgp4(tle);
            Tle = tle;
        }

        public void Update()
        {
            if (ShouldCalculateOrbit) CalculateOrbit();
            else if (orbitGO)
            {
                Destroy(orbitGO);
            }
        }

        public void CalculateOrbit()
        {
            if (!orbitGO)
            {
                orbitGO = new GameObject("OrbitPath");

                orbitRenderer = orbitGO.AddComponent<LineRenderer>();

                orbitRenderer.startWidth = 5000f;
                orbitRenderer.endWidth = 5000f;
                orbitRenderer.material = new Material(Shader.Find("Sprites/Default"));
                orbitRenderer.startColor = Color.cyan;
                orbitRenderer.endColor = Color.cyan;
            }

            var positions = new List<Vector3>();
            for (TimeSpan i = TimeSpan.Zero; i < TimeSpan.FromHours(12); i = i.Add(TimeSpan.FromMinutes(1)))
            {
                var pos = OrbitPropagator.FindPosition(SatelliteManager.Instance.CurrentSimulatedTime.Add(i))
                    .ToSphericalEcef();
                var position = math.mul(SatelliteManager.Instance.cesiumGeoreference.ecefToLocalMatrix,
                    new double4(pos.ToDouble(), 1.0)).xyz;
                positions.Add(position.ToVector());
            }

            orbitRenderer.positionCount = positions.Count;
            orbitRenderer.SetPositions(positions.ToArray());
        }
    }
}