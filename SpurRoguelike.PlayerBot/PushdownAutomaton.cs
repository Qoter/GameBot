using System;
using System.Collections.Generic;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal class PushdownAutomaton
    {
        private readonly Stack<Func<LevelView, Turn>> stackOfAction = new Stack<Func<LevelView, Turn>>();

        public Func<LevelView, Turn> CurrentAction => stackOfAction.Peek();

        public void PushAction(Func<LevelView, Turn> getNextTurn)
        {
            stackOfAction.Push(getNextTurn);
        }

        public Func<LevelView, Turn> PopAction()
        {
            return stackOfAction.Pop();
        }
    }
}