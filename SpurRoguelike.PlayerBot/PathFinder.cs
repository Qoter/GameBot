using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal class PathFinder
    {
        public static List<Location> FindShortestPath(LevelView levelView, Location from, Func<Location, bool> isTarget)
        { 
            var queue = new Queue<Location>();
            queue.Enqueue(from);
            var previous = new Dictionary<Location, Location> {[from] = default(Location)};

            while (queue.Any())
            {
                var currentLocation = queue.Dequeue();
                var nextLocations = GetAdjacentLocations(currentLocation, levelView)
                    .Where(l => IsPassable(l, levelView) || isTarget(l))
                    .Where(l => !previous.ContainsKey(l));

                foreach (var nextLocation in nextLocations)
                {
                    previous[nextLocation] = currentLocation;
                    queue.Enqueue(nextLocation);

                    if (isTarget(nextLocation))
                        return CreatePath(from, nextLocation, previous);
                }
            }
            return null;
        }

        public static List<Location> FindShortestPathWithInfluenceMap(LevelView levelView, InfluenceMap influenceMap, Location from, Func<Location, bool> isTarget)
        {
            var notOpened = new SortedSet<LocationWithPriority>();
            var distances = new Dictionary<Location, int>();
            var previous = new Dictionary<Location, Location> {[from] = default(Location)};

            foreach (var location in levelView.Field.GetAllLocations())
            {
                if ((IsPassable(location, levelView) || isTarget(location)) && location != from)
                {
                    distances[location] = int.MaxValue;
                    notOpened.Add(new LocationWithPriority(location, int.MaxValue));
                }
            }

            distances[from] = 0;
            notOpened.Add(new LocationWithPriority(from, 0));

            while (notOpened.Any())
            {
                var toOpen = notOpened.Min;
                notOpened.Remove(toOpen);

                if (toOpen.Priority == int.MaxValue)
                     return null;
                if (isTarget(toOpen.Location))
                    return CreatePath(from, toOpen.Location, previous);

                foreach (var adjacentLocation in GetAdjacentLocations(toOpen.Location, levelView).Where(l => IsPassable(l, levelView) || isTarget(l)))
                {
                    var currentDistance = toOpen.Priority + influenceMap[adjacentLocation];
                    if (!previous.ContainsKey(adjacentLocation) || currentDistance < distances[adjacentLocation])
                    {
                        notOpened.Remove(new LocationWithPriority(adjacentLocation, distances[adjacentLocation]));
                        distances[adjacentLocation] = currentDistance;
                        notOpened.Add(new LocationWithPriority(adjacentLocation, currentDistance));
                        previous[adjacentLocation] = toOpen.Location;
                    }
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

        public static Turn GetFirstTurn(List<Location> path)
        {
            if (path == null || path.Count < 2)
                 return Turn.None;
            var stepOffset = path[1] - path[0];
            return Turn.Step(stepOffset);
        }

        private static IEnumerable<Location> GetAdjacentLocations(Location location, LevelView levelView)
        {
            return Offset.StepOffsets.Select(offset => location + offset);
        }

        private static bool IsPassable(Location location, LevelView levelView)
        {
            var isObject = levelView.GetHealthPackAt(location).HasValue ||
                           levelView.GetMonsterAt(location).HasValue ||
                           levelView.GetItemAt(location).HasValue;

            var isEmpty = levelView.Field[location] == CellType.Empty ||
                          levelView.Field[location] == CellType.PlayerStart;

            return !isObject && isEmpty;
        }
    }
}
