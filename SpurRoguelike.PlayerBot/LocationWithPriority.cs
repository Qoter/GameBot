using System;
using SpurRoguelike.Core.Primitives;

namespace SpurRoguelike.PlayerBot
{
    internal class LocationWithPriority : IComparable<LocationWithPriority>
    {
        public readonly Location Location;
        public readonly int Priority;

        public LocationWithPriority(Location location, int priority)
        {
            Location = location;
            Priority = priority;
        }

        public int CompareTo(LocationWithPriority other)
        {
            var priorityRes = Priority.CompareTo(other.Priority);
            if (priorityRes != 0)
                return priorityRes;

            return Location.GetHashCode().CompareTo(other.Location.GetHashCode());
        }
    }
}