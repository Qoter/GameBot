using SpurRoguelike.Core.Primitives;
using System.Collections.Generic;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public static class LevelViewExtnensions
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
    }
}
