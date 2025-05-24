using System;
using System.Collections.Generic;
using CesiumForUnity;
using DefaultNamespace;
using Unity.Mathematics;
using UnityEngine;
using Satellites.SGP;
using Satellites.SGP.Propagation;
using Satellites.SGP.TLE;
using UnityEngine.Serialization;

namespace Satellites
{
    public class SatelliteController : MonoBehaviour
    {
        public Sgp4 OrbitPropagator;
        public Tle Tle;
        public bool shouldCalculateOrbit;
        private GameObject _orbitGo;
        private LineRenderer _orbitRenderer;

        public void Initialize(Tle tle)
        {
            name = tle.NoradNumber + " " + tle.Name;
            OrbitPropagator = new Sgp4(tle);
            Tle = tle;
            shouldCalculateOrbit = name == "7646 STARLETTE";
        }

        public void Update()
        {
            if (shouldCalculateOrbit) CalculateOrbit();
            else if (_orbitGo)
            {
                Destroy(_orbitGo);
            }
        }

        private void CalculateOrbit()
        {
            CreateOrbitGo();

            var positions = CalculateNextPositions(TimeSpan.FromHours(12), TimeSpan.FromMinutes(1));

            _orbitRenderer.positionCount = positions.Count;
            _orbitRenderer.SetPositions(positions.ToArray());
        }

        private void CreateOrbitGo()
        {
            if (_orbitGo) return;
            _orbitGo = new GameObject("OrbitPath");

            _orbitRenderer = _orbitGo.AddComponent<LineRenderer>();

            _orbitRenderer.startWidth = 5000f;
            _orbitRenderer.endWidth = 5000f;
            _orbitRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _orbitRenderer.startColor = Color.cyan;
            _orbitRenderer.endColor = Color.cyan;
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