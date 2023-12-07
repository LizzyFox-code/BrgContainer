namespace Samples.Hello_World
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BrgContainer.Runtime;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    public struct SampleJob : IJobFor
    {
        [NativeDisableParallelForRestriction]
        public BatchInstanceDataBuffer InstanceDataBuffer;
        
        [ReadOnly]
        public NativeArray<float> NoiseOffsets;
        
        public Weights Weights;
        public float Time;
        public float DeltaTime;
        public float MaxDistance;
        public float Speed;
        public float RotationSpeed;
        public int Size;
        public float3 Goal;
        public int ObjectToWorldPropertyId;

        public void Execute(int index)
        {
            var objectToWorld = InstanceDataBuffer.ReadInstanceData<PackedMatrix>(index, ObjectToWorldPropertyId);
            var currentPosition = objectToWorld.GetPosition();
            var perceivedSize = Size - 1;

            var separation = float3.zero;
            var alignment = float3.zero;
            var cohesion = float3.zero;
            var tendency = math.normalizesafe(Goal - currentPosition) * Weights.TendencyWeight;

            for (var i = 0; i < Size; i++)
            {
                if (i == index)
                    continue;

                var otherObjectToWorld = InstanceDataBuffer.ReadInstanceData<PackedMatrix>(i, ObjectToWorldPropertyId);
                var otherPosition = otherObjectToWorld.GetPosition();

                // Perform separation
                separation += SeparationVector(currentPosition, otherPosition, MaxDistance);

                // Perform alignment
                alignment += otherObjectToWorld.GetForward();

                // Perform cohesion
                cohesion += otherPosition;
            }

            var avg = 1f / perceivedSize;
            alignment *= avg;
            cohesion *= avg;
            cohesion = math.normalizesafe(cohesion - currentPosition);
            
            var direction = separation +
                            Weights.AlignmentWeight * alignment +
                            cohesion +
                            Weights.TendencyWeight * tendency;

            var targetRotation = QuaternionBetween(objectToWorld.GetForward(), math.normalizesafe(direction));
            var finalRotation = objectToWorld.GetRotation();

            if (!targetRotation.Equals(objectToWorld.GetRotation()))
            {
                finalRotation = math.lerp(finalRotation.value, targetRotation.value, RotationSpeed * DeltaTime);
            }

            var pNoise = math.abs(noise.cnoise(new float2(Time, NoiseOffsets[index])) * 2f - 1f);
            var speedNoise = Speed * (1f + pNoise * Weights.NoiseWeight * 0.9f);
            var finalPosition = currentPosition + objectToWorld.GetForward() * speedNoise * DeltaTime;
            
            InstanceDataBuffer.SetTRS(index, finalPosition, finalRotation, new float3(1, 1, 1));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion QuaternionBetween(in float3 from, in float3 to) 
        {
            var cross = math.cross(from, to);

            var w = math.sqrt(math.lengthsq(from) * math.lengthsq(to)) + math.dot(from, to);
            return new quaternion(new float4(cross, w));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 SeparationVector(float3 current, float3 other, float maxDistance)
        {
            var diff = current - other;
            var mag = math.length(diff);
            var scalar = math.clamp(1 - mag / maxDistance, 0, 1);

            return diff * (scalar / mag);
        }
    }
}