using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Heatmap
{
    [BurstCompile]
    public struct HeatmapDensityJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> Vertices;      // vertex world positions
        [ReadOnly] public NativeArray<float3> Satellites;    // raw satellite world positions
        [ReadOnly] public float InfluenceRadiusSqr;
        [ReadOnly] public float MaxDensityCount;
        [ReadOnly] public float3 SphereCenter;
        [ReadOnly] public float SphereRadius;

        [WriteOnly] public NativeArray<Color> Colors;

        public void Execute(int index)
        {
            float3 vertex = Vertices[index];
            int count = 0;

            for (int i = 0; i < Satellites.Length; i++)
            {
                // Projektion direkt hier
                float3 direction = math.normalize(Satellites[i] - SphereCenter);
                float3 projected = SphereCenter + direction * SphereRadius;

                float distSqr = math.distancesq(projected, vertex);
                if (distSqr < InfluenceRadiusSqr)
                {
                    count++;
                }
            }

            float density = math.clamp((float)count / MaxDensityCount, 0f, 1f);
            Colors[index] = Color.Lerp(new Color(0.278f,1.0f,0.349f), new Color(0.0f,0.561f,0.055f), density);
        }
    }
}