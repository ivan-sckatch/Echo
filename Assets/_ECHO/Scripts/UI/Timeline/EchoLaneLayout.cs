using System;
using System.Collections.Generic;
namespace EchoGame.UI
{
    // Pure interval partitioning; neither validates saves nor changes recordings.
    public static class EchoLaneLayout
    {
        public readonly struct Placement
        {
            public readonly EchoUIData Echo;
            public readonly int Lane;
            public Placement(EchoUIData echo, int lane) { Echo = echo; Lane = lane; }
        }
        public static List<Placement> Arrange(IReadOnlyList<EchoUIData> echoes)
        {
            var sorted = new List<EchoUIData>();
            foreach (var e in echoes)
                if (!float.IsNaN(e.Start) && !float.IsInfinity(e.Start) &&
                    !float.IsNaN(e.End) && !float.IsInfinity(e.End) && e.End > e.Start) sorted.Add(e);
            sorted.Sort((a, b) => { int c = a.Start.CompareTo(b.Start); return c != 0 ? c : a.Id.CompareTo(b.Id); });
            var ends = new SortedSet<(float End, int Lane)>();
            var result = new List<Placement>(sorted.Count);
            int count = 0;
            foreach (var e in sorted)
            {
                int lane;
                if (ends.Count > 0 && ends.Min.End <= e.Start)
                { var free = ends.Min; ends.Remove(free); lane = free.Lane; }
                else lane = count++;
                ends.Add((e.End, lane)); result.Add(new Placement(e, lane));
            }
            return result;
        }
    }
}
