using System;
using System.Collections.Generic;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PushdownAutomaton
    {
        private readonly Stack<Func<LevelView, IMessageReporter, Turn>> stackOfAction = new Stack<Func<LevelView, IMessageReporter, Turn>>();

        public Func<LevelView, IMessageReporter, Turn> CurrentAction => stackOfAction.Peek();

        public void PushAction(Func<LevelView, IMessageReporter, Turn> getNextTurn)
        {
            stackOfAction.Push(getNextTurn);
        }

        public Func<LevelView, IMessageReporter, Turn> PopAction()
        {
            return stackOfAction.Pop();
        }
    }
}