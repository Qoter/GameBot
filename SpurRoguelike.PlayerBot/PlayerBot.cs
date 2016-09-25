/*
using System;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController
    {
        private State<PlayerBot> state;

        #region States

        private static bool AttackManyMonsters(LevelView view)
        {
            return view.Monsters.Count(m => view.Player.Location.IsInRange(m.Location, 1)) > 1;
        }

        private class MovementToTheExit : State<PlayerBot>
        {
            public MovementToTheExit(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter)
            {
                if (levelView.Monsters.Any())
                {
                    GoToState(() => new FindMonster(Self));
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
                    GoToState(() => new FindMonster(Self));
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

        private class FindMonster : State<PlayerBot>
        {
            public FindMonster(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter)
            {
                if (levelView.Player.Health < 70 && levelView.HealthPacks.Any())
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }

                if (AttackManyMonsters(levelView) && levelView.Player.Health != 100)
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }

                foreach (var monster in levelView.Monsters)
                {
                    if (levelView.Player.Location.IsInRange(monster.Location, 1))
                    {
                        GoToState(() => new Attack(Self));
                        return Self.state.MakeTurn(levelView, massageReporter);
                    }
                }

                if (!levelView.Monsters.Any())
                {
                    GoToState(() => new MovementToTheExit(Self));
                    return Self.state.MakeTurn(levelView, massageReporter);
                }


               var monsterLoc = levelView.Monsters.Select((m, i) => new {m = m, i  = i}).Min(m => Tuple.Create(GetDistance(m.m.Location, levelView.Player.Location),m.i, m.m.Location)).Item3;

                var path = PathHelper.FindShortestPath(levelView, levelView.Player.Location,
                    (location, view) => location == monsterLoc);
                
                return PathHelper.GetFirstTurn(path);
            }

            public override void GoToState(Func<State<PlayerBot>> getNewState)
            {
                Self.state = getNewState();
            }
        }

        private static double GetDistance(Location l1, Location l2)
        {
            return Math.Sqrt((l1.X - l2.X)*(l1.X - l2.X) + (l1.Y - l2.Y)*(l1.Y - l2.Y));
        }

        private class Attack : State<PlayerBot>
        {
            public Attack(PlayerBot self) : base(self)
            {
            }

            public override Turn MakeTurn(LevelView view, IMessageReporter massageReporter)
            {
                if (view.Player.Health < 70 && view.HealthPacks.Any())
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(view, massageReporter);
                }

                if (AttackManyMonsters(view) && view.Player.Health != 100)
                {
                    GoToState(() => new NeedHealth(Self));
                    return Self.state.MakeTurn(view, massageReporter);
                }

                foreach (var monster in view.Monsters)
                {
                    if (view.Player.Location.IsInRange(monster.Location, 1))
                    {
                        var offset = monster.Location - view.Player.Location;
                        return Turn.Attack(offset);
                    }
                }

                GoToState(() => new FindMonster(Self));
                return Self.state.MakeTurn(view, massageReporter);
            }

            public override void GoToState(Func<State<PlayerBot>> getNewState)
            {
                Self.state = getNewState();
            }
        }

        #endregion
        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            if (state == null)
                state = new FindMonster(this);
            return state.MakeTurn(levelView, messageReporter);
        }
    }
}
*/
