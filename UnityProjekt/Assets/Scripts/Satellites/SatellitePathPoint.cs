using Unity.Mathematics;

namespace Satellites
{
    public class SatellitePathPoint
    {
        public double3 Position;
        public float ArrivalTime; // Wann der Punkt erreicht werden soll
        
        public SatellitePathPoint(double3 position, float arrivalTime)
        {
            Position = position;
            ArrivalTime = arrivalTime;
        }
    }
}