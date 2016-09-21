using System;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal abstract class State<T>
    {
        protected State(T self)
        {
            Self = self;
        }

        public abstract Turn MakeTurn(LevelView levelView, IMessageReporter massageReporter);
        public abstract void GoToState(Func<State<T>> getNewState);

        protected T Self;
    }
}