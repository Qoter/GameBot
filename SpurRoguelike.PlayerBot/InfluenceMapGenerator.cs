using System;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class InfluenceMapGenerator
    {
        private static int monsterInfluenceSeed = 25;

        public static int[,] Generate(LevelView levelView)
        {
            var influenceMap = new int[levelView.Field.Width, levelView.Field.Height];
            for (var x = 0; x < influenceMap.GetLength(0); x++)
            {
                for (var y = 0; y < influenceMap.GetLength(1); y++)
                {
                    influenceMap[x, y] = CalculeteInfluence(levelView, x, y);
                }
            }
            return influenceMap;
        }

        private static int CalculeteInfluence(LevelView view, int x, int y)
        {
            var location = new Location(x, y);

            if (!IsPassable(view, location))
                return int.MaxValue;

            var baseInfluence = CalculateBaseInfluence(view, location);

            return baseInfluence + view.Monsters
                .Select(m => CalculateMonsterInfluence( m, location))
                .Aggregate(0, (i, j) => i + j);
        }

        public static int CalculateBaseInfluence(LevelView levelView, Location location)
        {
            var multipliers = new[] {1, 3, 5};
            var aroundLocations = Offset.StepOffsets
                .SelectMany(offset => multipliers.Select(x => offset.Multiply(x)))
                .Select(offset => location + offset);
            return aroundLocations.Count(loc => !IsPassable(levelView, loc))*3 + 1;
        }

        private static bool IsPassable(LevelView levelView, Location location)
        {
            if (levelView.Field.IsOutOfRange(location))
                return false;

            return levelView.Field[location] != CellType.Wall &&
                   levelView.Field[location] != CellType.Trap &&
                   !levelView.GetItemAt(location).HasValue;

        }

        private static int CalculateMonsterInfluence(PawnView monster, Location location)
        {
            if (monster.Location.IsInRange(location, 1))
                return monsterInfluenceSeed;

            var offset = monster.Location - location;
            return monsterInfluenceSeed/(int) Math.Pow(2, offset.Size());
        }
    }
}
