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
            ShouldCalculateOrbit = name == "7646 STARLETTE";
        }

        public void Update()
        {
            if (ShouldCalculateOrbit) CalculateOrbit();
            else if (orbitGO)
            {
                Destroy(orbitGO);
            }
        }

        private void CalculateOrbit()
        {
            CreateOrbitGo();

            var positions = CalculateNextPositions(TimeSpan.FromHours(12), TimeSpan.FromMinutes(1));

            orbitRenderer.positionCount = positions.Count;
            orbitRenderer.SetPositions(positions.ToArray());
        }

        private void CreateOrbitGo()
        {
            if (orbitGO) return;
            orbitGO = new GameObject("OrbitPath");

            orbitRenderer = orbitGO.AddComponent<LineRenderer>();

            orbitRenderer.startWidth = 5000f;
            orbitRenderer.endWidth = 5000f;
            orbitRenderer.material = new Material(Shader.Find("Sprites/Default"));
            orbitRenderer.startColor = Color.cyan;
            orbitRenderer.endColor = Color.cyan;
        }

        public List<Vector3> CalculateNextPositions(TimeSpan until, TimeSpan stepSize)
        {
            var positions = new List<Vector3>();
            for (TimeSpan i = TimeSpan.Zero; i < until; i = i.Add(stepSize))
            {
                var pos = OrbitPropagator.FindPosition(SatelliteManager.Instance.CurrentSimulatedTime.Add(i))
                    .ToSphericalEcef();
                var position = math.mul(SatelliteManager.Instance.cesiumGeoreference.ecefToLocalMatrix,
                    new double4(pos.ToDouble(), 1.0)).xyz;
                positions.Add(position.ToVector());
            }

            return positions;
        }
    }
}