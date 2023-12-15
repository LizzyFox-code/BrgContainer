namespace BrgContainer.Runtime
{
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;
    using UnityEngine.Rendering;

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct RendererDescription : IEquatable<RendererDescription>
    {
        /// <summary>
        /// Specifies how instances from the draw commands in this draw range cast shadows.
        /// </summary>
        public readonly ShadowCastingMode ShadowCastingMode;
        /// <summary>
        /// Indicates whether instances from draw commands in this draw range should receive shadows.
        /// </summary>
        public readonly bool ReceiveShadows;
        /// <summary>
        /// Indicates whether instances from the draw commands in this draw range render into cached shadow maps.
        /// </summary>
        public readonly bool StaticShadowCaster;
        /// <summary>
        /// The rendering layer mask to use for draw commands in this draw range.
        /// </summary>
        public readonly uint RenderingLayerMask;
        /// <summary>
        /// The layer to use for draw commands in this draw range.
        /// </summary>
        public readonly byte Layer;
        /// <summary>
        /// Specifies how to generate motion vectors in this draw range.
        /// </summary>
        public readonly MotionVectorGenerationMode MotionMode;

        public RendererDescription(ShadowCastingMode shadowCastingMode, bool receiveShadows, bool staticShadowCaster, uint renderingLayerMask, byte layer, MotionVectorGenerationMode motionMode)
        {
            ShadowCastingMode = shadowCastingMode;
            ReceiveShadows = receiveShadows;
            StaticShadowCaster = staticShadowCaster;
            RenderingLayerMask = renderingLayerMask;
            Layer = layer;
            MotionMode = motionMode;
        }

        public bool Equals(RendererDescription other)
        {
            return ShadowCastingMode == other.ShadowCastingMode && ReceiveShadows == other.ReceiveShadows && StaticShadowCaster == other.StaticShadowCaster && 
                   RenderingLayerMask == other.RenderingLayerMask && Layer == other.Layer && MotionMode == other.MotionMode;
        }

        public override bool Equals(object obj)
        {
            return obj is RendererDescription other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ShadowCastingMode, ReceiveShadows, StaticShadowCaster, RenderingLayerMask, Layer, MotionMode);
        }

        public static bool operator ==(RendererDescription left, RendererDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RendererDescription left, RendererDescription right)
        {
            return !left.Equals(right);
        }
    }
}