using CesiumForUnity;
using DefaultNamespace;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Satellites
{
    public struct MoveSatelliteJobParallelForTransform : IJobParallelForTransform
    {
        public NativeArray<double3> Positions;
        public double4x4 EcefToLocalMatrix;

        public void Execute(int index, TransformAccess transform)
        {
            var position = math.mul(EcefToLocalMatrix, new double4(Positions[index], 1.0)).xyz;
            transform.position = position.ToVector();
        }
    }
}