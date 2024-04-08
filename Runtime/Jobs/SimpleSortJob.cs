namespace BrgContainer.Runtime.Jobs
{
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, CompileSynchronously = true, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)]
    internal struct SimpleSortJob<T, TComparer> : IJob where T : unmanaged where TComparer : unmanaged, IComparer<T>
    {
        public NativeArray<T> Array;
        public TComparer Comparer;
        
        public void Execute()
        {
            Array.Sort(Comparer);
        }
    }
}