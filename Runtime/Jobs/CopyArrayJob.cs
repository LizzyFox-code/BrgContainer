namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct CopyArrayJob<T> : IJob where T : unmanaged
    {
        public NativeArray<T> Source;
        public NativeArray<T> Destination;
        
        public void Execute()
        {
            NativeArray<T>.Copy(Source.AsReadOnly(), 0, Destination, 0, Source.Length);
        }
    }
}