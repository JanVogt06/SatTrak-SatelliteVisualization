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
        public Tle Tle;
        public Renderer Renderer { get; private set; }
        public CesiumGlobeAnchor Anchor { get; private set; }
        public List<SatellitePathPoint> Paths { get; } = new();
        public Queue<double3> Positions { get; } = new();
        public bool continuousMovementActive;

        public void Initialize(Tle tle)
        {
            Anchor = GetComponent<CesiumGlobeAnchor>();
            Renderer = GetComponent<Renderer>();
            Tle = tle;
            OrbitPropagator = new Sgp4(tle);
        }

        public void ResetPath(bool setCurrentPos = false, float segmentDuration = 0)
        {
            if (!Positions.Any())
                return;
            Paths.Clear();
            
            if (setCurrentPos)
                Paths.Add(new SatellitePathPoint(Anchor.longitudeLatitudeHeight, Time.time));
            
            var position = Positions.Dequeue();
            Anchor.longitudeLatitudeHeight = position;
            //Maybe just create new list
            Paths.Add(new SatellitePathPoint(position, Time.time + segmentDuration));

        }

        public DateTime GeneratePositions(ref DateTime nextTime, int maxPositionCache)
        {
            if (Positions.Count >= maxPositionCache)
            {
                return nextTime;
            }

            var positions = maxPositionCache - Positions.Count;

            DateTime futureTime = nextTime;
            for (int j = 0; j < positions; j++)
            {
                futureTime = futureTime.AddMinutes(1);
                EciCoordinate result = null;
                try
                {
                    result = OrbitPropagator.FindPosition(futureTime);
                }
                catch (DecayedException _)
                {
                    // Behandle abgestürzte Satelliten
                    continue;
                }
                catch (Exception _)
                {
                    continue;
                }

                var pos = result.ToGeodetic();
                var newPosition = new double3(pos.Longitude.Degrees, pos.Latitude.Degrees,
                    pos.Altitude * 1000);

                Positions.Enqueue(newPosition);
            }

            return futureTime;
        }
    }
}
