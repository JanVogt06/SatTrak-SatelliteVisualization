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

        public void Execute(int index, TransformAccess transform)
        {
            transform.position = Positions[index].ToVector();
        }
    }
}