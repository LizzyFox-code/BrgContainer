namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BatchGroupDrawRange
    {
        /// <summary>
        /// Begin index in the draw command array 
        /// </summary>
        public int Begin;
        /// <summary>
        /// The global offset for this batch group
        /// </summary>
        public int IndexOffset;
        /// <summary>
        /// Draw command count for this batch group
        /// </summary>
        public int Count;
        /// <summary>
        /// The index offset of visible indices array
        /// </summary>
        public int VisibleIndexOffset;
    }
}