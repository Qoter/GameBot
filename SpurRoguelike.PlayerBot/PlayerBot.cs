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


        public PlayerBot()
        {
            automatonOfBotState = new PushdownAutomaton();
            automatonOfBotState.PushAction(Fight);
        }

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            //Thread.Sleep(50);
            //var costs = InfluenceMapGenerator.Generate(levelView);
            //HtmlGenerator.WriteHtml(levelView, costs);
            if (Console.KeyAvailable)
            {
                //Stop for debug
            }
            messageReporter.ReportMessage(automatonOfBotState.CurrentAction.Method.Name);
            return automatonOfBotState.CurrentAction.Invoke(levelView, messageReporter);
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


            var monstersLocations = new HashSet<Location>(levelView.Monsters.Select(m => m.Location));
            var pathToNearestMonstar = PathHelper.FindShortestPath(levelView, levelView.Player.Location, (loc, _) => monstersLocations.Contains(loc));
            return PathHelper.GetFirstTurn(pathToNearestMonstar);
        }


        private Turn CollectHealth(LevelView levelView, IMessageReporter reporter)
        {
            if (levelView.Player.Health < EndCollectHealthLimit && levelView.HealthPacks.Any())
            {
                //collect health
                var healthLocations = new HashSet<Location>(levelView.HealthPacks.Select(hp => hp.Location));
                var cost = InfluenceMapGenerator.Generate(levelView);
                var pathToNearestHealth = PathHelper.FindShortestPathWithInfluenceMap(levelView, cost, levelView.Player.Location,
                    loc => healthLocations.Contains(loc));
                return PathHelper.GetFirstTurn(pathToNearestHealth);
            }

            automatonOfBotState.PopAction();
            return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
        }

        private Turn MoveToExit(LevelView levelView, IMessageReporter reporter)
        {
            //if (levelView.Monsters.Any())
            //{
            //    automatonOfBotState.PopAction();
            //    automatonOfBotState.PushAction(Fight);
            //    return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
            //}
            if (!levelView.HealthPacks.Any() || levelView.Player.Health == 100)
            {
                //MoveToExit
                var exitLocation = GetExitLocation(levelView);
                var pathToExit = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (loc, _) => loc == exitLocation);
                return PathHelper.GetFirstTurn(pathToExit);
            }

            //
            automatonOfBotState.PushAction(CollectHealth);
            return automatonOfBotState.CurrentAction.Invoke(levelView, reporter);
        }

        private Location GetExitLocation(LevelView levelView)
        {
            for (int x = 0; x < levelView.Field.Width; x++)
            {
                for (int y = 0; y < levelView.Field.Height; y++)
                {
                    if (levelView.Field[new Location(x, y)] == CellType.Exit)
                        return new Location(x, y);
                }
            }
            return default(Location);
        }
    }
}
