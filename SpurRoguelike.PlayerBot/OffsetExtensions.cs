using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    public static class OffsetExtensions
    {
        public static Offset Multiply(this Offset offset, int value)
        {
            return new Offset(offset.XOffset*value, offset.YOffset*value);
        }
    }
}
