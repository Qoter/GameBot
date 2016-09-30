using System;
using System.Collections.Generic;
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

            if (!PathHelper.IsPassable(location, view))
                return -1;

            var baseInfluence = CalculateBaseInfluence(view, location);

            return baseInfluence + view.Monsters
                .Select(m => CalculateMonsterInfluence(view, m, location))
                .Aggregate(0, (i, j) => i + j);
        }

        public static int CalculateBaseInfluence(LevelView levelView, Location location)
        {
            var around = Offset.StepOffsets.SelectMany(offset => new[]
            {
                location + offset,
                location + offset + offset + offset,
                location +  offset + offset + offset + offset + offset
            });
            return around.Count(loc => !IsPassable(levelView, loc))*3 + 1;
        }

        public static bool IsPassable(LevelView levelView, Location location)
        {
            if (location.X < 0 ||
                location.Y < 0 ||
                location.X >= levelView.Field.Width ||
                location.Y >= levelView.Field.Height)
                return false;

            return levelView.Field[location] != CellType.Wall && levelView.Field[location] != CellType.Trap &&
                   !levelView.GetItemAt(location).HasValue;

        }

        private static int CalculateMonsterInfluence(LevelView levelView, PawnView monster, Location location)
        {
            if (monster.Location.IsInRange(location, 1))
                return monsterInfluenceSeed;

            var offset = monster.Location - location;
            return monsterInfluenceSeed/(int) Math.Pow(2, offset.Size());
        }
    }
}
