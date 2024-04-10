namespace BrgContainer.Runtime
{
    using System.Diagnostics.CodeAnalysis;
    using Lod;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;
    using LODGroup = Lod.LODGroup;
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

        /// <summary>
        /// Adds a new batch to the BatchRendererGroupContainer.
        /// </summary>
        /// <param name="container">The BatchRendererGroupContainer to add the batch to.</param>
        /// <param name="maxInstanceCount">The maximum number of instances in the batch.</param>
        /// <param name="lodGroup">The lod group.</param>
        /// <param name="rendererDescription">The description of the renderer.</param>
        /// <returns>A BatchHandle struct representing the added batch.</returns>
        public static BatchHandle AddBatch(this BatchRendererGroupContainer container, int maxInstanceCount, ref LODGroup lodGroup, in RendererDescription rendererDescription)
        {
            var batchDescription = new BatchDescription(maxInstanceCount, Allocator.Persistent);
            return container.AddBatch(ref batchDescription, ref lodGroup, float3.zero, rendererDescription);
        }

        /// <summary>
        /// Adds a new batch to the BatchRendererGroupContainer.
        /// </summary>
        /// <param name="container">The BatchRendererGroupContainer to add the batch to.</param>
        /// <param name="maxInstanceCount">The maximum number of instances in the batch.</param>
        /// <param name="materialProperties">The array of material properties.</param>
        /// <param name="lodGroup">The lod group.</param>
        /// <param name="rendererDescription">The description of the renderer.</param>
        /// <returns>A BatchHandle struct representing the added batch.</returns>
        public static BatchHandle AddBatch(this BatchRendererGroupContainer container, int maxInstanceCount, NativeArray<MaterialProperty> materialProperties, ref LODGroup lodGroup, in RendererDescription rendererDescription)
        {
            var batchDescription = new BatchDescription(maxInstanceCount, materialProperties, Allocator.Persistent);
            return container.AddBatch(ref batchDescription, ref lodGroup, float3.zero, rendererDescription);
        }
    }
}