using System;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    class MovementToTheExit : State
    {
        public MovementToTheExit()
        {

        }
        public override Turn Act(LevelView levelView)
        {
            throw new NotImplementedException();
        }
    }

    class NeedHealth : State
    {
        public NeedHealth()
        {
        }
        public override Turn Act(LevelView levelView)
        {
            throw new NotImplementedException();
        }
    }

    class MonsterProsecution : State
    {
        public MonsterProsecution()
        {
            
        }

        public override Turn Act(LevelView levelView)
        {
            throw new NotImplementedException();
        }

    }

    class Attack : State
    {
        public Attack()
        {

        }
        public override Turn Act(LevelView levelView)
        {
            throw new NotImplementedException();
        }
    }

    class NeedItem : State
    {
        public override Turn Act(LevelView levelView)
        {
            throw new NotImplementedException();
        }
    }

    class Escape : State
    {
        public override Turn Act(LevelView levelView)
        {
            throw new NotImplementedException();
        }
    }

    class PlayerBot2 : IPlayerController
    {
        private State currentState = new MonsterProsecution();
        public Turn MakeTurn(LevelView levelView, IMessageReporter messageReporter)
        {
            messageReporter.ReportMessage(currentState.GetType().Name);
            return currentState.MakeTurn(levelView, out currentState);
        }
    }
}
