using System;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class InfluenceMapGenerator
    {
        private const int MonsterInfluenceSeed = 10;
        public static int[,] Generate(LevelView levelView)
        {
            var influenceMap = new int[levelView.Field.Width, levelView.Field.Height];
            for (var x = 0; x < influenceMap.GetLength(0); x++)
            {
                for (var y = 0; y < influenceMap.GetLength(1); y++)
                {
                    influenceMap[x, y] = Math.Max(1, CalculeteInfluence(levelView, x, y));
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
            var cell = view.Field[location];

            if (cell == CellType.Wall || cell == CellType.Exit || cell == CellType.Trap || view.Monsters.Any(m => m.Location == location))
            {
                return -1;
            }

            if (!view.Monsters.Any())
                return 1;
            return view.Monsters
                .Select(m => CalculateMonsterInfluence(m, location))
                .Aggregate(Intersect);
        }

        private static int CalculateMonsterInfluence(PawnView monster, Location location)
        {
            if (monster.Location.IsInRange(location, 1))
            {
                return MonsterInfluenceSeed;
            }

            var offset = monster.Location - location;
            var range = offset.Size();//Math.Max(Math.Abs(offset.XOffset), Math.Abs(offset.YOffset));

            var value = MonsterInfluenceSeed;
            for (var i = 0; i < range - 1; i++)
            {
                value = Reduce(value);
            }

            return value;
        }

        private static int Reduce(int value)
        {
            return value/2;
        }

        private static int Intersect(int value1, int value2)
        {
            return value1 + value2;
        }
    }
}
