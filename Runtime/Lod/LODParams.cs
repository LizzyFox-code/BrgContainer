namespace BrgContainer.Runtime.Lod
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LODParams : IEquatable<LODParams>
    {
        public float DistanceScale;
        public float3 CameraPosition;
        public bool IsOrthographic;
        public float OrthoSize;

        public bool Equals(LODParams other)
        {
            return DistanceScale.Equals(other.DistanceScale) && CameraPosition.Equals(other.CameraPosition) && IsOrthographic == other.IsOrthographic && OrthoSize.Equals(other.OrthoSize);
        }

        public override bool Equals(object obj)
        {
            return obj is LODParams other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DistanceScale, CameraPosition, IsOrthographic, OrthoSize);
        }

        public static bool operator ==(LODParams left, LODParams right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LODParams left, LODParams right)
        {
            return !left.Equals(right);
        }

        public static LODParams CreateLODParams(LODParameters parameters)
        {
            LODParams lodParams;
            lodParams.CameraPosition = parameters.cameraPosition;
            lodParams.IsOrthographic = parameters.isOrthographic;
            lodParams.OrthoSize = parameters.orthoSize;
            lodParams.DistanceScale = CalculateLodDistanceScale(parameters.fieldOfView, QualitySettings.lodBias,
                lodParams.IsOrthographic, lodParams.OrthoSize);

            return lodParams;
        }
        
        private static float CalculateLodDistanceScale(float fieldOfView, float globalLodBias, bool isOrthographic, float orthoSize)
        {
            float distanceScale;
            if (isOrthographic)
            {
                distanceScale = 2.0f * orthoSize / globalLodBias;
            }
            else
            {
                var halfAngle = math.tan(math.radians(fieldOfView * 0.5F));
                distanceScale = 2.0f * halfAngle / globalLodBias;
            }

            return distanceScale;
        }
    }
}