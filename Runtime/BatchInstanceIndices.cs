namespace BrgContainer.Runtime
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BatchInstanceIndices
    {
        public unsafe int* Indices;
    }
}