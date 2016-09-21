using System;
using System.Linq;
using System.Threading;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Entities;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController
    {
        private State<PlayerBot> state;

        #region States

        private class GoToExit : State<PlayerBot>
        {
            public GoToExit(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter)
            {
                if (levelView.Monsters.Any())
                {
                    GoToState(() => new GoToMonster(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }

                if (levelView.HealthPacks.Any() && levelView.Player.Health < 100)
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }

                var exitLoc = GetExitLocation(levelView);
                var path = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (location, view) => location == exitLoc);
                return PathHelper.GetFirstTurn(path);
            }

            public override void GoToState(Func<State<PlayerBot>> getNewState)
            {
                Self.state = getNewState();
            }

            private static Location GetExitLocation(LevelView levelView)
            {
                for (var x = 0; x < levelView.Field.Width; x++)
                    for (var y = 0; y < levelView.Field.Height; y++)
                        if (levelView.Field[new Location(x, y)] == CellType.Exit)
                            return new Location(x, y);
                return default(Location);
            }
        }

        private class NeedHealth : State<PlayerBot>
        {
            public NeedHealth(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter)
            {
                if (!levelView.HealthPacks.Any() && levelView.Player.Health != 100)
                {
                    massageReporter.ReportMessage("Нужны жизни, но их нет");
                    return Turn.None;
                }

                if (levelView.Player.Health == 100)
                {
                    GoToState(() => new GoToMonster(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }


                var path = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (location, view) => view.HealthPacks.Any(hp => hp.Location == location));
                return PathHelper.GetFirstTurn(path);
            }

            public override void GoToState(Func<State<PlayerBot>> getNewState)
            {
                Self.state = getNewState();
            }
        }

        private class GoToMonster : State<PlayerBot>
        {
            public GoToMonster(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter)
            {
                if (levelView.Player.Health < 40 && levelView.HealthPacks.Any())
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }

                foreach (var monster in levelView.Monsters)
                {
                    if (levelView.Player.Location.IsInRange(monster.Location, 1))
                    {
                        GoToState(() => new Fight(Self));
                        return Self.state.MakeTurn(levelView, massageReporter);
                    }
                }

                if (!levelView.Monsters.Any())
                {
                    GoToState(() => new GoToExit(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }


                var path = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (location, view) => view.Monsters.Any(m => m.Location == location));
                
                return PathHelper.GetFirstTurn(path);
            }

            public override void GoToState(Func<State<PlayerBot>> getNewState)
            {
                Self.state = getNewState();
            }
        }

        private class Fight : State<PlayerBot>
        {
            public Fight(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter)
            {
                if (levelView.Player.Health < 40 && levelView.HealthPacks.Any())
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }

                foreach (var monster in levelView.Monsters)
                {
                    if (levelView.Player.Location.IsInRange(monster.Location, 1))
                    {
                        var offset = monster.Location - levelView.Player.Location;
                        return Turn.Attack(offset);
                    }
                }

                GoToState(() => new GoToMonster(Self));
                return Self.state.MakeTurn(levelView, massageReporter);
            }

            public override void GoToState(Func<State<PlayerBot>> getNewState)
            {
                Self.state = getNewState();
            }
        }

        #endregion
        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            Thread.Sleep(10);
            if (state == null)
                state = new GoToMonster(this);
            return state.MakeTurn(levelView, messageReporter);
        }
    }
}
