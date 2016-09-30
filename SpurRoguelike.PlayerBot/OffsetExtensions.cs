using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    internal static class OffsetExtensions
    {
        public static Offset Multiply(this Offset offset, int value)
        {
            return new Offset(offset.XOffset*value, offset.YOffset*value);
        }
    }
}
