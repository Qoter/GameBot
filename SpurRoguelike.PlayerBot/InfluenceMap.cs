using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal class InfluenceMap
    {
        public int this[Location location] => GetInfluenceValue(location);

        private readonly Dictionary<Location, int> influenceCache;
        private readonly LevelView levelView;
        private readonly int monsterInfluenceSeed;
        private readonly int arenaFighterInfluenceSeed;
        private readonly int wallInfluenceSeed;
        private FieldView Field => levelView.Field;

        public InfluenceMap(LevelView levelView)
        {
            influenceCache = new Dictionary<Location, int>();
            this.levelView = levelView;
            monsterInfluenceSeed = 32;
            arenaFighterInfluenceSeed = 8;
            wallInfluenceSeed = CalculateMagicWallInfluenceSeed(Field);
        }

        public static InfluenceMap CreateForm(LevelView levelView)
        {
            return new InfluenceMap(levelView);
        }

        public int GetInfluenceValue(Location location)
        {
            if (influenceCache.ContainsKey(location))
                return influenceCache[location];

            var influenceValue = CalculateInfluence(location);
            influenceCache[location] = influenceValue;
            return influenceValue;
        }

        private static int CalculateMagicWallInfluenceSeed(FieldView field)
        {
            var emptyCellsCount = field.GetCellsOfType(CellType.Empty).Count();
            var factor = Math.Max(0, emptyCellsCount - 1000) / 80 + 1;
            factor = Math.Min(10, factor);
            var inversedFactor = 10 - factor;
            return inversedFactor * inversedFactor;
        }

        private int CalculateInfluence(Location location)
        {
            if (levelView.Field.IsOutOfRange(location))
                return -1;

            var cellType = Field[location];

            if (cellType == CellType.Wall || 
                cellType == CellType.Trap || 
                cellType == CellType.Exit || 
                levelView.GetItemAt(location).HasValue)
                return -1;

            if (levelView.GetHealthPackAt(location).HasValue)
                return 1 + CalculateMonstersInfluence(location, monsterInfluenceSeed);

            var seedForMonsters = levelView.Monsters.Count() == 1 ? arenaFighterInfluenceSeed : monsterInfluenceSeed;
            return CalculeateBaseInfluence(location) + CalculateMonstersInfluence(location, seedForMonsters);
        }

        private int CalculateMonstersInfluence(Location location, int seed)
        {
            return levelView.Monsters
                .Select(monster => CalculateMonsterInfluence(monster, location, seed))
                .Sum();
        }

        private int CalculateMonsterInfluence(PawnView monster, Location location, int influenceSeed)
        {
            if (levelView.GetHealthPackAt(location).HasValue)
                return 0;

            if (monster.Location.IsInRange(location, 1))
               return influenceSeed;

            var offsetToMonster = monster.Location - location;
            return influenceSeed / (int) Math.Pow(2, offsetToMonster.Size() - 1);
        }

        private int CalculeateBaseInfluence(Location location)
        {
            return Enumerable.Range(1, 5)
                .Select(distance => CalculateBaseInfluenceOnDistance(location, distance))
                .Sum() + 1;
        }

        private int CalculateBaseInfluenceOnDistance(Location location, int distance)
        {
            var wallsCount = Offset.StepOffsets
                .Select(offset => location + offset.Multiply(distance))
                .Count(loc => !levelView.Field.IsOutOfRange(loc) && levelView.Field[loc] == CellType.Wall);

            return wallsCount*wallInfluenceSeed/(int) Math.Pow(2, distance - 1);
        }

    }
}
