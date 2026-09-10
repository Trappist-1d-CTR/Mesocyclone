using Unity.Entities;
using Unity.Mathematics;

namespace Mesocyclone
{
    // TODO: Make more modular

    /// <summary>
    /// (really shitty) DOTS-compatable version of Animation Curves
    /// </summary>
    public struct BlobCurve
    {
        public BlobArray<float2> KeyFrames; // x = time, y = value. that's how animation curves go AFAIK
    }
}