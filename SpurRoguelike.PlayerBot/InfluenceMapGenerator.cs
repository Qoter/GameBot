using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class InfluenceMapGenerator
    {
        private static int monsterInfluenceSeed = 32;
        private static int wallInfluenceSeed = 20;

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

            foreach (var item in levelView.Items)
            {
                influenceMap[item.Location.X, item.Location.Y] = influenceMap[item.Location.X, item.Location.Y] + 100;
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
                .Aggregate(0, Intersect);
        }

        public static int CalculateBaseInfluence(LevelView levelView, Location location)
        {
            var around = Offset.AttackOffsets.SelectMany(offset => new[] {location + offset, location + offset + offset});
            return around.Count(loc => !IsPassable(levelView, loc)) * 10 + 1;
        }

        public static bool IsPassable(LevelView levelView, Location location)
        {
            if (location.X < 0 ||
                location.Y < 0 ||
                location.X >= levelView.Field.Width ||
                location.Y >= levelView.Field.Height)
                return false;

            return levelView.Field[location] != CellType.Wall;

        }

        private static int CalculateMonsterInfluence(LevelView levelView, PawnView monster, Location location)
        {
            var seed = levelView.Monsters.Count() == 1 ? 2 : monsterInfluenceSeed;
            if (monster.Location.IsInRange(location, 1))
            {
                return seed;
            }

            var offset = monster.Location - location;

            var range = offset.Size();

            var value = seed;
            for (var i = 0; i < range - 1; i++)
            {
                value = Reduce(value);
            }

            return value;
        }

        private static int Reduce(int value)
        {
            return value / 2;
        }

        private static int Intersect(int value1, int value2)
        {
            return value1 + value2;
        }
    }
}
