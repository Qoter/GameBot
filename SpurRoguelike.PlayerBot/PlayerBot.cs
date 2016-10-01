using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController
    {
        private readonly PushdownAutomaton automaton;

        private const int HighHealthLimit = 65;
        private const int LowHealthLimit = 50;
        private int endFightHealthLimit = HighHealthLimit;
        private bool hasBestItem = false;

        public PlayerBot()
        {
            automaton = new PushdownAutomaton();
            automaton.PushAction(Fight);
        }

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            return automaton.CurrentAction.Invoke(levelView);
        }

        private Turn Equip(LevelView levelView)
        {
            var bestItem = FindBestItem(levelView.Items);

            ItemView playerItem;
            levelView.Player.TryGetEquippedItem(out playerItem);

            if (GetItemConst(bestItem) <= GetItemConst(playerItem))
            {
                hasBestItem = true;
                automaton.PopAction();
                return automaton.CurrentAction.Invoke(levelView);
            }

            hasBestItem = false;
            var pathToBestItem = PathFinder.FindShortestPath(levelView, levelView.Player.Location, loc => loc == bestItem.Location);
            return PathFinder.GetFirstTurn(pathToBestItem);
        }

        private Turn Fight(LevelView levelView)
        {
            endFightHealthLimit = levelView.Monsters.Count() == 1 ? LowHealthLimit : HighHealthLimit;

            if (levelView.Player.Health < endFightHealthLimit && levelView.HealthPacks.Any())
            {
                automaton.PushAction(CollectHealth);
                return automaton.CurrentAction.Invoke(levelView);
            }
            if (!levelView.Monsters.Any())
            {
                automaton.PopAction();
                automaton.PushAction(MoveToExit);
                return automaton.CurrentAction.Invoke(levelView);
            }

            if (GetCountMonstersOnAttackRange(levelView, levelView.Player.Location) == 0) // all monsters on long distance
            {
                automaton.PushAction(FindMonster);
                return automaton.CurrentAction.Invoke(levelView);
            }

            if (levelView.Monsters.Count() == 1 && !hasBestItem)
            {
                automaton.PushAction(Equip);
                return automaton.CurrentAction.Invoke(levelView);
            }

            return ExecuteFight(levelView);
        }

        private static Turn ExecuteFight(LevelView levelView)
        {
            var monsterInAttackRange = levelView.Monsters.First(m => levelView.Player.Location.IsInRange(m.Location, 1));
            var attackOffset = monsterInAttackRange.Location - levelView.Player.Location;
            return Turn.Attack(attackOffset);
        }

        private Turn FindMonster(LevelView levelView)
        {
            if (GetCountMonstersOnAttackRange(levelView, levelView.Player.Location) > 0 || !levelView.Monsters.Any())
            {
                automaton.PopAction();
                return automaton.CurrentAction.Invoke(levelView);
            }

            if (levelView.Player.Health < endFightHealthLimit && levelView.HealthPacks.Any())
            {
                automaton.PushAction(CollectHealth);
                return automaton.CurrentAction.Invoke(levelView);
            }

            return ExecuteFindMonster(levelView);
        }

        private static Turn ExecuteFindMonster(LevelView levelView)
        {
            var pathToMonster = PathFinder.FindShortestPath(levelView, levelView.Player.Location, 
                location => IsLocationForAttack(levelView, location));

            if (pathToMonster != null)
                return PathFinder.GetFirstTurn(pathToMonster);
            var pathToHealthOrItem = PathFinder.FindShortestPath(levelView, levelView.Player.Location,
                location => levelView.GetHealthPackAt(location).HasValue ||levelView.GetItemAt(location).HasValue);
            return PathFinder.GetFirstTurn(pathToHealthOrItem);
        }

        private static bool IsLocationForAttack(LevelView levelView, Location location)
        {
            return GetCountMonstersOnAttackRange(levelView, location) > 0 &&
                   levelView.Field[location] != CellType.Wall &&
                   levelView.Field[location] != CellType.Trap &&
                   !levelView.GetItemAt(location).HasValue;
        }

        private Turn CollectHealth(LevelView levelView)
        {
            if (levelView.Player.Health > HighHealthLimit || !levelView.HealthPacks.Any())
            {
                automaton.PopAction();
                return automaton.CurrentAction.Invoke(levelView);
            }

            var influenceMap = InfluenceMap.CreateForm(levelView);
            var pathToNearestHealth = PathFinder.FindShortestPathWithInfluenceMap(levelView, influenceMap, levelView.Player.Location, loc => levelView.GetHealthPackAt(loc).HasValue);
            return PathFinder.GetFirstTurn(pathToNearestHealth);
        }


        private Turn MoveToExit(LevelView levelView)
        {
            if (levelView.Monsters.Any())
            {
                hasBestItem = false;
                automaton.PopAction();
                automaton.PushAction(Fight);
                return automaton.CurrentAction.Invoke(levelView);
            }

            if (levelView.Player.Health < HighHealthLimit && levelView.HealthPacks.Any())
            {
                automaton.PushAction(CollectHealth);
                return automaton.CurrentAction.Invoke(levelView);
            }

            var exitLocation = GetExitLocation(levelView);
            var pathToExit = PathFinder.FindShortestPath(levelView, levelView.Player.Location, loc => loc == exitLocation);
            return PathFinder.GetFirstTurn(pathToExit);
        }

        private static Location GetExitLocation(LevelView levelView)
        {
            return levelView.Field.GetAllLocations()
                .FirstOrDefault(loc => levelView.Field[loc] == CellType.Exit);
        }

        private static int GetItemConst(ItemView item)
        {
            return item.AttackBonus + item.DefenceBonus*2;
        }


        private static ItemView FindBestItem(IEnumerable<ItemView> items)
        {
            var bestItem = new ItemView();
            foreach (var item in items)
            {
                if (GetItemConst(bestItem) < GetItemConst(item))
                    bestItem = item;
            }
            return bestItem;
        }

        private static int GetCountMonstersOnAttackRange(LevelView levelView, Location location)
        {
            return levelView.Monsters.Count(m => location.IsInRange(m.Location, 1));
        }
    }
}
