using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simplified orthogonal snapping algorithm using 1D clustering.
/// Much faster than RDP for orthogonal-only shapes.
/// </summary>
public static class OrthoSnapper
{
    public readonly struct Pt
    {
        public readonly double X, Y;
        public Pt(double x, double y) { X = x; Y = y; }
    }

    // Main entry point
    public static List<Pt> SnapToOrthogonalLevels(
        IReadOnlyList<Pt> input,
        bool closed,
        double minStep = 1.0,          // drop near-duplicates
        double clusterTol = 2.0        // how close values must be to be "same level"
    )
    {
        if (input == null || input.Count < 2) return input?.ToList() ?? new List<Pt>();

        var pts = FilterMinStep(input, minStep, closed);

        // Learn "true" X/Y levels from the data
        var xLevels = Cluster1D(pts.Select(p => p.X), clusterTol);
        var yLevels = Cluster1D(pts.Select(p => p.Y), clusterTol);

        // Snap every point
        var snapped = pts.Select(p => new Pt(
            NearestLevel(p.X, xLevels),
            NearestLevel(p.Y, yLevels)
        )).ToList();

        // Cleanup: remove consecutive duplicates, collinear
        snapped = RemoveConsecutiveDuplicates(snapped, closed);
        snapped = RemoveCollinear(snapped, closed);

        // Final: ensure closure isn't duplicated
        if (closed && snapped.Count > 2 && Same(snapped[0], snapped[^1]))
            snapped.RemoveAt(snapped.Count - 1);

        return snapped;
    }

    // --- Helpers ---

    private static List<Pt> FilterMinStep(IReadOnlyList<Pt> input, double minStep, bool closed)
    {
        double min2 = minStep * minStep;
        var outPts = new List<Pt>(input.Count);
        var prev = input[0];
        outPts.Add(prev);

        for (int i = 1; i < input.Count; i++)
        {
            var p = input[i];
            double dx = p.X - prev.X, dy = p.Y - prev.Y;
            if (dx * dx + dy * dy >= min2)
            {
                outPts.Add(p);
                prev = p;
            }
        }

        if (closed && outPts.Count > 2)
        {
            var last = outPts[^1];
            var first = outPts[0];
            double dx = last.X - first.X, dy = last.Y - first.Y;
            if (dx * dx + dy * dy < min2) outPts.RemoveAt(outPts.Count - 1);
        }

        return outPts;
    }

    // Simple 1D clustering by sorting and grouping within tolerance
    private static List<double> Cluster1D(IEnumerable<double> values, double tol)
    {
        var a = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                      .OrderBy(v => v).ToArray();
        if (a.Length == 0) return new List<double>();

        var clusters = new List<List<double>>();
        var cur = new List<double> { a[0] };

        for (int i = 1; i < a.Length; i++)
        {
            if (Math.Abs(a[i] - cur[^1]) <= tol) cur.Add(a[i]);
            else { clusters.Add(cur); cur = new List<double> { a[i] }; }
        }
        clusters.Add(cur);

        // Use median (robust) as the level value
        return clusters.Select(Median).ToList();
    }

    private static double NearestLevel(double v, List<double> levels)
    {
        double best = levels[0];
        double bestD = Math.Abs(v - best);
        for (int i = 1; i < levels.Count; i++)
        {
            double d = Math.Abs(v - levels[i]);
            if (d < bestD) { bestD = d; best = levels[i]; }
        }
        return best;
    }

    private static List<Pt> RemoveConsecutiveDuplicates(List<Pt> pts, bool closed)
    {
        if (pts.Count < 2) return pts;
        var outPts = new List<Pt> { pts[0] };
        for (int i = 1; i < pts.Count; i++)
            if (!Same(pts[i], outPts[^1])) outPts.Add(pts[i]);

        if (closed && outPts.Count > 2 && Same(outPts[0], outPts[^1]))
            outPts.RemoveAt(outPts.Count - 1);

        return outPts;
    }

    private static List<Pt> RemoveCollinear(List<Pt> pts, bool closed)
    {
        if (pts.Count < 3) return pts;
        int n = pts.Count;
        var outPts = new List<Pt>(n);

        bool IsCollinear(Pt a, Pt b, Pt c)
        {
            // axis-aligned collinear check
            return (Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(b.X - c.X) < 1e-9) ||
                   (Math.Abs(a.Y - b.Y) < 1e-9 && Math.Abs(b.Y - c.Y) < 1e-9);
        }

        for (int i = 0; i < n; i++)
        {
            if (!closed && (i == 0 || i == n - 1))
            {
                outPts.Add(pts[i]);
                continue;
            }

            var prev = pts[(i - 1 + n) % n];
            var cur = pts[i];
            var next = pts[(i + 1) % n];

            if (!IsCollinear(prev, cur, next))
                outPts.Add(cur);
        }

        return outPts;
    }

    private static bool Same(Pt a, Pt b) => Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9;

    private static double Median(List<double> values)
    {
        values.Sort();
        int mid = values.Count / 2;
        if (values.Count % 2 == 1) return values[mid];
        return 0.5 * (values[mid - 1] + values[mid]);
    }
}

