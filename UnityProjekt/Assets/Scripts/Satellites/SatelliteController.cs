using System;
using System.Collections.Generic;
using System.Linq;
using CesiumForUnity;
using DefaultNamespace;
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
        public List<Vector3> NextPositions { get; private set; } = new();

        public void Initialize(Tle tle, CesiumGeoreference cesiumGeoreference)
        {
            Renderer = GetComponent<Renderer>();
            OrbitPropagator = new Sgp4(tle);
            var pos = OrbitPropagator.FindPosition(DateTime.Now).ToSphericalEcef();
            var position = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(pos.ToDouble());
            transform.position = position.ToVector();
        }

        public double3 CalculatePosition(DateTime currentSimulatedTime)
        {
            var pos = OrbitPropagator.FindPosition(currentSimulatedTime).ToSphericalEcef();
            AddNextPosition(pos.ToVector());
            return pos.ToDouble();
        }

        private void AddNextPosition(Vector3 position)
        {
            // + 10 damit die letzten 10 Positionen auch gespeichert werden
            if (NextPositions.Count < SatelliteManager.NextPositionAmount + 10)
            {
                NextPositions.Add(position);
            }
            else
            {
                NextPositions.RemoveAt(0);
                NextPositions.Add(position);
            }
        }
    }
}
