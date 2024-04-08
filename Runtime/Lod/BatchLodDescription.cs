namespace BrgContainer.Runtime.Lod
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BatchLodDescription
    {
        public readonly float4 LodDistances;
        public readonly int LodCount;

        public float this[int index] => LodDistances[index];

        public BatchLodDescription(float4 distances, int lodCount)
        {
            LodDistances = distances;
            LodCount = lodCount;
        }
    }
}