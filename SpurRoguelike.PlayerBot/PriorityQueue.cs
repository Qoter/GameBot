using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpurRoguelike.PlayerBot
{
    public class PriorityQueue<TKey> : IPriorityQueue<TKey>
    {
        public void Add(TKey key, int value)
        {
        }

        public void Update(TKey key, int value)
        {
        }

        public bool TryGetValue(TKey key, out int value)
        {
            value = 0;
            return false;
        }

        public Tuple<TKey, int> ExtractMinKey()
        {
            return null;
        }
    }
}
