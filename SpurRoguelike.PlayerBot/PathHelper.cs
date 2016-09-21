using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal class PathHelper
    {
        public static List<Location> FindShortestPath(LevelView levelView, Location from, Func<Location, LevelView, bool> isTarget)
        { 
            var queue = new Queue<Location>();
            queue.Enqueue(from);
            var previous = new Dictionary<Location, Location> {[from] = default(Location)};

            while (queue.Any())
            {
                var currentLocation = queue.Dequeue();
                var nextLocations = GetAdjacentLocations(currentLocation, levelView)
                    .Where(l => IsPassable(l, levelView) || isTarget(l, levelView))
                    .Where(l => !previous.ContainsKey(l));

                foreach (var nextLocation in nextLocations)
                {
                    previous[nextLocation] = currentLocation;
                    queue.Enqueue(nextLocation);

                    if (isTarget(nextLocation, levelView))
                        return CreatePath(from, nextLocation, previous);
                }
            }
            return null;
        }

        private static List<Location> CreatePath(Location from, Location to, Dictionary<Location, Location> previous)
        {
            var path = new List<Location> { to };
            while (path.Last() != from)
            {
                path.Add(previous[path.Last()]);
            }
            path.Reverse();
            return path;
        }

        public static IEnumerable<Location> GetAdjacentLocations(Location location, LevelView levelView)
        {
            return Offset.StepOffsets.Select(offset => location + offset);
        }

        public static bool IsPassable(Location location, LevelView levelView)
        {
            if (levelView.Monsters.Any(m => m.Location == location))
                return false;

            return levelView.Field[location] == CellType.Empty ||
                   levelView.Field[location] == CellType.PlayerStart;
        }

        public static Turn GetFirstTurn(List<Location> path)
        {
            if (path == null || path.Count < 2)
                return Turn.None;
            var stepOffset = path[1] - path[0];
            return Turn.Step(stepOffset);
        }
    }
}
