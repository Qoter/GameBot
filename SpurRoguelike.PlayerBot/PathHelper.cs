using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    internal class PathHelper
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
                    .Where(l => (IsPassable(l, levelView) && !levelView.GetHealthPackAt(l).HasValue) || isTarget(l))
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

        public static List<Location> FindShortestPathWithInfluenceMap(LevelView levelView, int[,] influenceMap, Location from, Func<Location, bool> isTarget)
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
                var currentLocation = notOpened.Min;
                notOpened.Remove(currentLocation);

                if (currentLocation.Priority == int.MaxValue)
                    return null;
                if (isTarget(currentLocation.Location))
                    return CreatePath(from, currentLocation.Location, previous);

                foreach (var adjacentLocation in GetAdjacentLocations(currentLocation.Location, levelView).Where(l => IsPassable(l, levelView) || isTarget(l)))
                {
                    var currentDistance = currentLocation.Priority + influenceMap[adjacentLocation.X, adjacentLocation.Y];
                    if (!previous.ContainsKey(adjacentLocation) || currentDistance < distances[adjacentLocation])
                    {
                        notOpened.Remove(new LocationWithPriority(adjacentLocation, distances[adjacentLocation]));
                        distances[adjacentLocation] = currentDistance;
                        notOpened.Add(new LocationWithPriority(adjacentLocation, currentDistance));
                        previous[adjacentLocation] = currentLocation.Location;
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

        private static IEnumerable<Location> GetAdjacentLocations(Location location, LevelView levelView)
        {
            return Offset.StepOffsets.Select(offset => location + offset);
        }

        private static bool IsPassable(Location location, LevelView levelView)
        {
            if (levelView.GetMonsterAt(location).HasValue)
                return false;

            if (levelView.GetItemAt(location).HasValue)
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
