using SpurRoguelike.Core.Primitives;
using System.Collections.Generic;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal static class FieldViewExtensions
    {
        public static IEnumerable<Location> GetAllLocations(this FieldView field)
        {
            for (var x = 0; x < field.Width; x++)
            {
                for (var y = 0; y < field.Height; y++)
                {
                    yield return new Location(x, y);
                }
            }
        }

        public static bool IsOutOfRange(this FieldView field, Location location)
        {
            return location.X < 0 || location.Y < 0 || location.X >= field.Width || location.Y >= field.Height;
        }
    }
}
