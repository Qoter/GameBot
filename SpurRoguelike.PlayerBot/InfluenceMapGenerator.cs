using System;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class InfluenceMapGenerator
    {
        private static int monsterInfluenceSeed = 16;
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
            var cell = view.Field[location];
            var baseInfluence = CalculateBaseInfluence(view, x, y);

            /*
            if (cell == CellType.Wall || cell == CellType.Exit || cell == CellType.Trap || view.Monsters.Any(m => m.Location == location))
            {
                return -1;
            }*/

            if (!baseInfluence.HasValue)
                return -1;

            return baseInfluence.Value + view.Monsters
                .Select(m => CalculateMonsterInfluence(view, m, location))
                .Aggregate(0, Intersect);
        }

        private static int? CalculateBaseInfluence(LevelView view, int x, int y)
        {
            var location = new Location(x, y);
            var cell = view.Field[location];
            if (cell == CellType.Wall || cell == CellType.Trap)
                return null;

            var rightWall = Enumerable.Range(0, 50)
                    .Select(deltaX => new Location(location.X + deltaX, location.Y))
                    .First(loc => view.Field[loc] == CellType.Wall);

            var leftWall = Enumerable.Range(0, 50)
                    .Select(deltaX => new Location(location.X - deltaX, location.Y))
                    .First(loc => view.Field[loc] == CellType.Wall);

            var upWall = Enumerable.Range(0, 50)
                    .Select(deltaY => new Location(location.X, location.Y - deltaY))
                    .First(loc => view.Field[loc] == CellType.Wall);

            var downWall = Enumerable.Range(0, 50)
                    .Select(deltaY => new Location(location.X, location.Y + deltaY))
                    .First(loc => view.Field[loc] == CellType.Wall);
            var walls = new[] { rightWall, leftWall, upWall, downWall };
            var offsets = walls.Select(wall => wall - location);

            var leftRight = leftWall - rightWall;
            var upDown = upWall - downWall;

            var min = Math.Min(leftRight.Size(), upDown.Size());

            int result;
            if (offsets.Any(o => o.Size() == 1))
                result = wallInfluenceSeed;
            else if (offsets.Any(o => o.Size() == 2))
                result = wallInfluenceSeed / 2;
            else if (offsets.Any(o => o.Size() == 3))
                result = wallInfluenceSeed / 4;
            else
                result = 1;

            if (min < 4)
                result += 100;

            return result;
        }

        private static int CalculateMonsterInfluence(LevelView levelView, PawnView monster, Location location)
        {
            var seed = levelView.Monsters.Count() == 1 ? 2 : monsterInfluenceSeed;
            if (monster.Location.IsInRange(location, 1))
            {
                return seed;
            }

            var offset = monster.Location - location;
            var range = offset.Size();//Math.Max(Math.Abs(offset.XOffset), Math.Abs(offset.YOffset));

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
