using System;
using System.Collections.Generic;
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
                    .Where(l => !previous.ContainsKey(l))
                    .Where(l => !levelView.GetHealthPackAt(l).HasValue);

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

        public static List<Location> FindShortestPathWithInfluenceMap(LevelView levelView, int[,] influenceMap, Location from, Func<Location, bool> isTarget)
        {
            var forOpen = new HashSet<Location>();
            var dist = new Dictionary<Location, int>();
            var prev = new Dictionary<Location, Location>();

            for (int x = 0; x < levelView.Field.Width; x++)
            {
                for (int y = 0; y < levelView.Field.Height; y++)
                {
                    var v = new Location(x, y);
                    if (IsPassable(v, levelView) || isTarget(v))
                    {
                        dist[v] = int.MaxValue;
                        prev[v] = default(Location);
                        forOpen.Add(v);
                    }
                }
            }

            dist[from] = 0;

            while (forOpen.Any())
            {
                var u = GetLocationWithMinDist(forOpen, dist);
                forOpen.Remove(u);

                foreach (var v in GetAdjacentLocations(u, levelView).Where(l => IsPassable(l, levelView) || isTarget(l)))
                {
                    if (influenceMap[v.X, v.Y] == -1)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    if (dist[u] == int.MaxValue)
                    {
                        dist[v] = influenceMap[v.X, v.Y];
                        prev[v] = u;
                    }
                    else
                    {
                        var alt = dist[u] + influenceMap[v.X, v.Y];
                        if (alt < dist[v])
                        {
                            dist[v] = alt;
                            prev[v] = u;
                        }
                    }

                }

                if (isTarget(u))
                {
                    return CreatePath(from, u, prev);
                }
            }

            return null;
        }

        private static Location GetLocationWithMinDist(HashSet<Location> q, Dictionary<Location, int> dist)
        {
            var minLoc = q.First();
            foreach (var location in q)
            {
                if (dist[location] < dist[minLoc])
                    minLoc = location;
            }
            return minLoc;
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
