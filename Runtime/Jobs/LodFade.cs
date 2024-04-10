namespace BrgContainer.Runtime.Jobs
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LodFade
    {
        public float Value;
        public int Lod;
    }
}