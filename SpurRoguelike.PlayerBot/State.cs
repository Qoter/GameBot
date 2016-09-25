using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{

    internal abstract class State
    {
        protected List<Func<LevelView, State>> Transits = new List<Func<LevelView, State>>();

        public abstract Turn Act(LevelView levelView);

        public Turn MakeTurn(LevelView view, out State nextState)
        { 
            var newState = Transits
                .Select(t => t(view))
                .FirstOrDefault(state => state != null);

            nextState = newState ?? this;
            return newState != null ? newState.Act(view) : Act(view);
        }
    }
}