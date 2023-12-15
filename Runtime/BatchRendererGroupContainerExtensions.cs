namespace BrgContainer.Runtime
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using UnityEngine;
#if ENABLE_IL2CPP
    using Il2Cpp;
#endif

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
#endif
    public static class BatchRendererGroupContainerExtensions
    {
        /// <summary>
        /// Add a new batch.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="maxInstanceCount"></param>
        /// <param name="materialProperties"></param>
        /// <param name="mesh"></param>
        /// <param name="subMeshIndex"></param>
        /// <param name="material"></param>
        /// <param name="rendererDescription"></param>
        /// <returns></returns>
        public static BatchHandle AddBatch(this BatchRendererGroupContainer container, int maxInstanceCount, NativeArray<MaterialProperty> materialProperties, 
            [NotNull]Mesh mesh, ushort subMeshIndex, [NotNull]Material material, in RendererDescription rendererDescription)
        {
            var batchDescription = new BatchDescription(maxInstanceCount, materialProperties, Allocator.Persistent);
            return container.AddBatch(ref batchDescription, mesh, subMeshIndex, material, rendererDescription);
        }

        /// <summary>
        /// Add a new batch.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="maxInstanceCount"></param>
        /// <param name="mesh"></param>
        /// <param name="subMeshIndex"></param>
        /// <param name="material"></param>
        /// <param name="rendererDescription"></param>
        /// <returns></returns>
        public static BatchHandle AddBatch(this BatchRendererGroupContainer container, int maxInstanceCount, [NotNull]Mesh mesh, 
            ushort subMeshIndex, [NotNull]Material material, in RendererDescription rendererDescription)
        {
            var batchDescription = new BatchDescription(maxInstanceCount, Allocator.Persistent);
            return container.AddBatch(ref batchDescription, mesh, subMeshIndex, material, rendererDescription);
        }
    }
}