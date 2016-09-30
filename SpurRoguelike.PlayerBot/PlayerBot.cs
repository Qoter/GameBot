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
        private const int EndFightHealthLimit = 65;
        private const int EndCollectHealthLimit = 100;

        private bool hasBestItem = false;


        public PlayerBot()
        {
            automatonOfBotState = new PushdownAutomaton();
            automatonOfBotState.PushAction(Fight);
        }

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            if (Console.KeyAvailable)
            {
                //Stop for debug
            }
            //messageReporter.ReportMessage(automatonOfBotState.CurrentAction.Method.Name);
            return automatonOfBotState.CurrentAction.Invoke(levelView, messageReporter);
        }

        private int GetItemConst(ItemView item)
        {
            return item.AttackBonus*5 + item.DefenceBonus*2;
        }


        private ItemView FindBestItem(IEnumerable<ItemView> items)
        {
            var bestItem = new ItemView();
            foreach (var item in items)
            {
                if (GetItemConst(bestItem) < GetItemConst(item))
                    bestItem = item;
            }
            return bestItem;
        }

        private Turn Equip(LevelView levelView, IMessageReporter messageReporter)
        {
            var bestItem = FindBestItem(levelView.Items);

            ItemView playerItem;
            levelView.Player.TryGetEquippedItem(out playerItem);

            if (GetItemConst(bestItem) <= GetItemConst(playerItem))
            {
                hasBestItem = true;
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView, messageReporter);
            }

            hasBestItem = false;
            var pathToBestItem = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                loc => loc == bestItem.Location);
            return PathHelper.GetFirstTurn(pathToBestItem);
        }

        private Turn Fight(LevelView levelView, IMessageReporter reporter)
        {
            if (levelView.Player.Health < EndFightHealthLimit && levelView.HealthPacks.Any()) // Collect health if health low
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            }

            if (!levelView.Monsters.Any()) // Move to exit if no monsters
            {
                automatonOfBotState.PopAction();
                automatonOfBotState.PushAction(MoveToExit);
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            }

            if (levelView.Monsters.All(m => !levelView.Player.Location.IsInRange(m.Location, 1))) // all monsters on long distance
            {
                automatonOfBotState.PushAction(ApproachToMonster);
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            }

            if (levelView.Monsters.Count() == 1 && !hasBestItem)
            {
                automatonOfBotState.PushAction(Equip);
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            }

            //fight
            var monsterInAttackRange = levelView.Monsters.First(m => levelView.Player.Location.IsInRange(m.Location, 1));
            var attackOffset = monsterInAttackRange.Location - levelView.Player.Location;
            return Turn.Attack(attackOffset);
        }

        private Turn ApproachToMonster(LevelView levelView, IMessageReporter reporter)
        {
            if (levelView.Monsters.Any(m => levelView.Player.Location.IsInRange(m.Location, 1)) || !levelView.Monsters.Any())
            {
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            }

            if (levelView.Player.Health < EndFightHealthLimit && levelView.HealthPacks.Any())
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            }

            var pathToNearestMonstar = PathHelper.FindShortestPath(levelView, levelView.Player.Location, loc => levelView.GetMonsterAt(loc).HasValue);
            return PathHelper.GetFirstTurn(pathToNearestMonstar);
        }


        private Turn CollectHealth(LevelView levelView, IMessageReporter reporter)
        {
            if (levelView.Player.Health < EndCollectHealthLimit && levelView.HealthPacks.Any())
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
            return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
        }

        private Turn MoveToExit(LevelView levelView, IMessageReporter reporter)
        {
            if (levelView.Monsters.Any())
            {
                //Fighth if move from previous level
                hasBestItem = false;
                automatonOfBotState.PopAction();
                automatonOfBotState.PushAction(Fight);
                return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
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
            return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
        }

        private Location GetExitLocation(LevelView levelView)
        {
            return levelView.Field.GetAllLocations()
                .FirstOrDefault(loc => levelView.Field[loc] == CellType.Exit);
        }
    }
}
