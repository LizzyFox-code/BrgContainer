namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public struct BatchLodDescription
    {
        private unsafe fixed float m_LodDistances[FixedBatchLodRendererData.Count];
        public readonly int LodCount;

        public unsafe float this[int index]
        {
            get => m_LodDistances[index];
            set => m_LodDistances[index] = value;
        }

        public unsafe BatchLodDescription(int lodCount)
        {
            LodCount = lodCount;

            for (var i = 0; i < FixedBatchLodRendererData.Count; i++)
            {
                m_LodDistances[i] = float.PositiveInfinity;
            }
        }
    }
}