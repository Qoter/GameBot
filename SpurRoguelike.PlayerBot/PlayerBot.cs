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
        private int endFightHealthLimit = 65;
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
            var pathToBestItem = PathHelper.FindShortestPath(levelView, levelView.Player.Location, loc => loc == bestItem.Location);
            return PathHelper.GetFirstTurn(pathToBestItem);
        }

        private Turn Fight(LevelView levelView)
        {
            endFightHealthLimit = levelView.Monsters.Count() == 1 ? 50 : 65;

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

            if (levelView.Monsters.All(m => !levelView.Player.Location.IsInRange(m.Location, 1))) // all monsters on long distance
            {
                automatonOfBotState.PushAction(ApproachToMonster);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (levelView.Monsters.Count() == 1 && !hasBestItem)
            {
                automatonOfBotState.PushAction(Equip);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            //fight
            var monsterInAttackRange = levelView.Monsters.First(m => levelView.Player.Location.IsInRange(m.Location, 1));
            var attackOffset = monsterInAttackRange.Location - levelView.Player.Location;
            return Turn.Attack(attackOffset);
        }

        public int GetCountMonstersOnAttackRange(LevelView levelView)
        {
            return levelView.Monsters.Count(m => levelView.Player.Location.IsInRange(m.Location, 1));
        }

        private Turn ApproachToMonster(LevelView levelView)
        {
            if (levelView.Monsters.Any(m => levelView.Player.Location.IsInRange(m.Location, 1)) || !levelView.Monsters.Any())
            {
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (levelView.Player.Health < endFightHealthLimit && levelView.HealthPacks.Any())
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            var pathToNearestMonstar = PathHelper.FindShortestPath(levelView, levelView.Player.Location, loc => levelView.Monsters.Any(m => m.Location.IsInRange(loc, 1))) ??
                                       PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                loc =>
                    levelView.GetHealthPackAt(loc).HasValue || levelView.GetItemAt(loc).HasValue ||
                    levelView.Monsters.Any(m => m.Location.IsInRange(loc, 1)));
            return PathHelper.GetFirstTurn(pathToNearestMonstar);
        }


        private Turn CollectHealth(LevelView levelView)
        {
            if (levelView.Player.Health < 100 && levelView.HealthPacks.Any())
            {
                //collect health
                if (levelView.Monsters.Count() <= 3)
                {
                    var path = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                        loc => levelView.GetHealthPackAt(loc).HasValue);
                    return PathHelper.GetFirstTurn(path);
                }
                var cost = InfluenceMapGenerator.Generate(levelView);
                var pathToNearestHealth = PathHelper.FindShortestPathWithInfluenceMap(levelView, cost, levelView.Player.Location,
                    loc => levelView.GetHealthPackAt(loc).HasValue);
                return PathHelper.GetFirstTurn(pathToNearestHealth);
            }

            automatonOfBotState.PopAction();
            return automatonOfBotState.CurrentAction.Invoke(levelView);
        }

        private Turn MoveToExit(LevelView levelView)
        {
            if (levelView.Monsters.Any())
            {
                //Fight if move from previous level
                hasBestItem = false;
                automatonOfBotState.PopAction();
                automatonOfBotState.PushAction(Fight);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }
            if (!levelView.HealthPacks.Any() || levelView.Player.Health == 100)
            {
                //MoveToExit
                var exitLocation = GetExitLocation(levelView);
                var pathToExit = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    loc => loc == exitLocation);
                return PathHelper.GetFirstTurn(pathToExit);
            }

            automatonOfBotState.PushAction(CollectHealth);
            return automatonOfBotState.CurrentAction.Invoke(levelView);
        }

        private static Location GetExitLocation(LevelView levelView)
        {
            return levelView.Field.GetAllLocations()
                .FirstOrDefault(loc => levelView.Field[loc] == CellType.Exit);
        }

        private static int GetItemConst(ItemView item)
        {
            return item.AttackBonus * 2 + item.DefenceBonus * 3;
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
    }
}
