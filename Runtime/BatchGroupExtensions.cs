namespace BrgContainer.Runtime
{
    using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
    using Il2Cpp;
#endif

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    public static class BatchGroupExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWindowCount(this BatchGroup batchGroup)
        {
            return GetWindowCount(batchGroup, batchGroup.InstanceCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWindowCount(this BatchGroup batchGroup, int instanceCount)
        {
            var description = batchGroup.m_BatchDescription;
            return (instanceCount + description.MaxInstancePerWindow - 1) /
                   description.MaxInstancePerWindow;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetInstanceCountPerWindow(this BatchGroup batchGroup, int subBatchIndex)
        {
            var description = batchGroup.m_BatchDescription;
            var batchCount = GetWindowCount(batchGroup);
            if (subBatchIndex >= batchCount)
                return 0;

            if(subBatchIndex == batchCount - 1)
                return description.MaxInstancePerWindow - (batchCount * description.MaxInstancePerWindow - batchGroup.InstanceCount);

            return description.MaxInstancePerWindow;
        }
    }
}