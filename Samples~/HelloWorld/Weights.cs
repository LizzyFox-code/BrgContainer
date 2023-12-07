namespace Samples.Hello_World
{
    using System;
    using System.Runtime.InteropServices;

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Weights
    {
        public float TendencyWeight;
        public float AlignmentWeight;
        public float NoiseWeight;

        public static Weights Default() 
        {
            return new Weights 
            {
                TendencyWeight   = 1,
                AlignmentWeight  = 1,
                NoiseWeight      = 1
            };
        }
    }
}