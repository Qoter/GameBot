using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class CostGenerator
    {
        private const int Seed = 10;
        public static int[,] Generate(LevelView levelView)
        {
            var costs = new int[levelView.Field.Width, levelView.Field.Height];
            for (int x = 0; x < costs.GetLength(0); x++)
            {
                for (int y = 0; y < costs.GetLength(1); y++)
                {
                    costs[x, y] = Math.Max(1, CalculateCost(levelView, x, y));
                }
            }
            return costs;
        }

        private static int CalculateCost(LevelView view, int x, int y)
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
            var offset = monster.Location - location;
            var range = Math.Max(Math.Abs(offset.XOffset), Math.Abs(offset.YOffset));

            var value = Seed;
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
