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
        private readonly Metadata meta;
        private const int EndFightHealthLimit = 72;
        private const int EndCollectHealthLimit = 100;

        private class Metadata
        {
            public bool HealthPackExists = false;
        }

        public PlayerBot()
        {
            meta = new Metadata();
            automatonOfBotState = new PushdownAutomaton();
            automatonOfBotState.PushAction(Fight);
        }

        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            //Thread.Sleep(100);
            //HtmlGenerator.WriteHtml(levelView, new Dictionary<Location, int>());
            return automatonOfBotState.CurrentAction.Invoke(levelView);
        }


        private Turn Fight(LevelView levelView)
        {
            if (levelView.Player.Health < EndFightHealthLimit) // Collect health if health low
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (!levelView.Monsters.Any()) // Move to exit if no monsters
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

            //fight
            var monsterInAttackRange = levelView.Monsters.First(m => levelView.Player.Location.IsInRange(m.Location, 1));
            var attackOffset = monsterInAttackRange.Location - levelView.Player.Location;
            return Turn.Attack(attackOffset);
        }

        private Turn ApproachToMonster(LevelView levelView)
        {
            if (levelView.Monsters.Any(m => levelView.Player.Location.IsInRange(m.Location, 1)) || !levelView.Monsters.Any())
            {
                automatonOfBotState.PopAction();
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }

            if (levelView.Player.Health < EndFightHealthLimit)
            {
                automatonOfBotState.PushAction(CollectHealth);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }


            //todo approach
            var monstersLocations = new HashSet<Location>(levelView.Monsters.Select(m => m.Location));
            var pathToNearestMonstar = PathHelper.FindShortestPath(levelView, levelView.Player.Location, (loc, _) => monstersLocations.Contains(loc));
            return PathHelper.GetFirstTurn(pathToNearestMonstar);
        }


        private Turn CollectHealth(LevelView levelView)
        {
            if (levelView.Player.Health < EndCollectHealthLimit && levelView.HealthPacks.Any())
            {
                //collect health
                var healthLocations = new HashSet<Location>(levelView.HealthPacks.Select(hp => hp.Location));
                var pathToNearestHealth = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (loc, _) => healthLocations.Contains(loc));
                return PathHelper.GetFirstTurn(pathToNearestHealth);
            }

            if (!levelView.HealthPacks.Any())
            {
                meta.HealthPackExists = false;
            }

            automatonOfBotState.PopAction();
            return automatonOfBotState.CurrentAction.Invoke(levelView);
        }

        private Turn MoveToExit(LevelView levelView)
        {
            if (levelView.Monsters.Any())
            {
                automatonOfBotState.PopAction();
                automatonOfBotState.PushAction(Fight);
                return automatonOfBotState.CurrentAction.Invoke(levelView);
            }
            if (!meta.HealthPackExists || levelView.Player.Health == 100)
            {
                //MoveToExit
                var exitLocation = GetExitLocation(levelView);
                var pathToExit = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (loc, _) => loc == exitLocation);
                return PathHelper.GetFirstTurn(pathToExit);
            }

            //
            automatonOfBotState.PushAction(CollectHealth);
            return automatonOfBotState.CurrentAction.Invoke(levelView);
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
