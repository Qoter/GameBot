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
        private readonly PushdownAutomaton automatonOfBotState;

        private const int HighHealthLimit = 65;
        private const int LowHealthLimit = 50;
        private int endFightHealthLimit = HighHealthLimit;
        private bool hasBestItem = false;

        public PlayerBot()
        {
            automatonOfBotState = new PushdownAutomaton();
            automatonOfBotState.PushAction(Fight);
        }

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            return automatonOfBotState.CurrentAction.Invoke(levelView);
        }


        private Turn Equip(LevelView levelView)
        {
            var bestItem = FindBestItem(levelView.Items);

            ItemView playerItem;
            levelView.Player.TryGetEquippedItem(out playerItem);

            if (GetItemConst(bestItem) <= GetItemConst(playerItem))
            {
                hasBestItem = true;
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView);
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
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }
            if (!levelView.Monsters.Any())
            {
                automatonOfBotState.PopAction();
                automatonOfBotState.PushAction(MoveToExit);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (GetCountMonstersOnAttackRange(levelView, levelView.Player.Location) == 0) // all monsters on long distance
            {
                automatonOfBotState.PushAction(ApproachToMonster);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (levelView.Monsters.Count() == 1 && !hasBestItem)
            {
                automatonOfBotState.PushAction(Equip);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            return MakeFightTurn(levelView);
        }

        private static Turn MakeFightTurn(LevelView levelView)
        {
            var monsterInAttackRange = levelView.Monsters.First(m => levelView.Player.Location.IsInRange(m.Location, 1));
            var attackOffset = monsterInAttackRange.Location - levelView.Player.Location;
            return Turn.Attack(attackOffset);
        }

        private Turn ApproachToMonster(LevelView levelView)
        {
            if (GetCountMonstersOnAttackRange(levelView, levelView.Player.Location) > 0 || !levelView.Monsters.Any())
            {
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (levelView.Player.Health < endFightHealthLimit && levelView.HealthPacks.Any())
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            return MakeApproachTurn(levelView);
        }

        private static Turn MakeApproachTurn(LevelView levelView)
        {
            var pathToMonster = PathFinder.FindShortestPath(levelView, levelView.Player.Location, 
                location => GetCountMonstersOnAttackRange(levelView, location) > 0);

            if (pathToMonster != null)
                return PathFinder.GetFirstTurn(pathToMonster);

            Func<Location, bool> targerFuncWithItemsAndHealth =
                location =>
                    levelView.GetHealthPackAt(location).HasValue ||
                    levelView.GetItemAt(location).HasValue ||
                    levelView.Monsters.Any(m => m.Location.IsInRange(location, 1));

            var notSafyPath = PathFinder.FindShortestPath(levelView, levelView.Player.Location,targerFuncWithItemsAndHealth);
            return PathFinder.GetFirstTurn(notSafyPath);
        }

        private Turn CollectHealth(LevelView levelView)
        {
            if (levelView.Player.Health >= 100 || !levelView.HealthPacks.Any())
            {
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            return MakeCollectHealthTurn(levelView);
        }

        private static Turn MakeCollectHealthTurn(LevelView levelView)
        {
            var useNotSafyPath = levelView.Monsters.Count() <= 3;
            if (useNotSafyPath)
            {
                var path = PathFinder.FindShortestPath(levelView, levelView.Player.Location, loc => levelView.GetHealthPackAt(loc).HasValue);
                return PathFinder.GetFirstTurn(path);
            }

            var influenceMap = InfluenceMapGenerator.Generate(levelView);
            var pathToNearestHealth = PathFinder.FindShortestPathWithInfluenceMap(levelView, influenceMap, levelView.Player.Location, loc => levelView.GetHealthPackAt(loc).HasValue);
            return PathFinder.GetFirstTurn(pathToNearestHealth);
        }

        private Turn MoveToExit(LevelView levelView)
        {
            if (levelView.Monsters.Any())
            {
                hasBestItem = false;
                automatonOfBotState.PopAction();
                automatonOfBotState.PushAction(Fight);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (levelView.Player.Health != 100 && levelView.HealthPacks.Any())
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            return MakeMoveToExitTurn(levelView);
        }

        private static Turn MakeMoveToExitTurn(LevelView levelView)
        {
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
            return item.AttackBonus*2 + item.DefenceBonus*3;
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
