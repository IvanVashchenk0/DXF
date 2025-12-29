using System;
using System.Collections.Generic;
using System.Linq;

public static class PolylineOrthogonalizer
{
    public readonly struct Pt
    {
        public readonly double X, Y;
        public Pt(double x, double y) { X = x; Y = y; }
        public static Pt operator +(Pt a, Pt b) => new Pt(a.X + b.X, a.Y + b.Y);
        public static Pt operator -(Pt a, Pt b) => new Pt(a.X - b.X, a.Y - b.Y);
        public static Pt operator *(Pt a, double s) => new Pt(a.X * s, a.Y * s);
        public double Len2() => X * X + Y * Y;
        public double Len() => Math.Sqrt(Len2());
    }

    private enum Ori { H, V }

    private readonly struct Run
    {
        public readonly Ori Orientation;
        public readonly double Const;    // y for H, x for V
        public readonly double A0, A1;   // along-axis min/max (x-range for H, y-range for V)
        public Run(Ori o, double c, double a0, double a1)
        {
            Orientation = o;
            Const = c;
            A0 = Math.Min(a0, a1);
            A1 = Math.Max(a0, a1);
        }
    }

    /// <summary>
    /// Main entry: turn a noisy outline into crisp right-angled outline.
    /// Assumes the desired edges are axis-aligned (horizontal/vertical).
    /// </summary>
    public static List<Pt> StraightenToRightAngles(
        IReadOnlyList<Pt> input,
        bool closed,
        double minStep = 1.0,     // drop points closer than this
        double rdpEps = 2.0,      // simplification tolerance
        double minEdgeLen = 2.0   // drop tiny edges after rebuild
    )
    {
        if (input == null || input.Count < 2) return input?.ToList() ?? new List<Pt>();

        // 1) de-jitter / remove near-duplicates
        var pts = FilterMinStep(input, minStep, closed);

        // 2) simplify with RDP
        pts = Rdp(pts, rdpEps, closed);

        if (pts.Count < 2) return pts;

        // 3) build H/V runs (merge consecutive same-orientation segments)
        var runs = BuildRuns(pts, closed);
        if (runs.Count < 2) return pts;

        // 4) rebuild corners by intersecting consecutive runs
        var rebuilt = IntersectionsToPolyline(runs, closed);

        // 5) cleanup: remove tiny edges, merge collinear
        rebuilt = RemoveTinyEdges(rebuilt, minEdgeLen, closed);
        rebuilt = MergeCollinear(rebuilt, closed);

        return rebuilt;
    }

    // -------------------- Core steps --------------------

    private static List<Pt> FilterMinStep(IReadOnlyList<Pt> input, double minStep, bool closed)
    {
        double min2 = minStep * minStep;
        var outPts = new List<Pt>(input.Count);

        Pt prev = input[0];
        outPts.Add(prev);

        for (int i = 1; i < input.Count; i++)
        {
            var p = input[i];
            if ((p - prev).Len2() >= min2)
            {
                outPts.Add(p);
                prev = p;
            }
        }

        // If closed and last is very close to first, remove last
        if (closed && outPts.Count > 2 && (outPts[^1] - outPts[0]).Len2() < min2)
            outPts.RemoveAt(outPts.Count - 1);

        return outPts;
    }

    // Ramer–Douglas–Peucker (works on open polyline; for closed we do a simple wrap trick)
    private static List<Pt> Rdp(List<Pt> pts, double eps, bool closed)
    {
        if (pts.Count < 3) return pts;

        if (!closed)
            return RdpOpen(pts, eps);

        // Closed: pick a stable split (farthest from first point) to "open" it.
        int split = 0;
        double best = -1;
        for (int i = 1; i < pts.Count; i++)
        {
            double d = (pts[i] - pts[0]).Len2();
            if (d > best) { best = d; split = i; }
        }

        var opened = new List<Pt>(pts.Count + 1);
        for (int i = split; i < pts.Count; i++) opened.Add(pts[i]);
        for (int i = 0; i < split; i++) opened.Add(pts[i]);

        // run RDP on opened
        var simp = RdpOpen(opened, eps);

        // rotate back
        // (we don't really need exact original indexing; just ensure closure)
        // Remove duplicate end if exists
        if (simp.Count > 2 && (simp[0] - simp[^1]).Len2() < 1e-18)
            simp.RemoveAt(simp.Count - 1);

        return simp;
    }

    private static List<Pt> RdpOpen(List<Pt> pts, double eps)
    {
        if (pts.Count < 3) return new List<Pt>(pts);
        int idx = -1;
        double dmax = 0;

        var a = pts[0];
        var b = pts[^1];

        for (int i = 1; i < pts.Count - 1; i++)
        {
            double d = DistPointToSegment(pts[i], a, b);
            if (d > dmax) { dmax = d; idx = i; }
        }

        if (dmax > eps)
        {
            var left = RdpOpen(pts.GetRange(0, idx + 1), eps);
            var right = RdpOpen(pts.GetRange(idx, pts.Count - idx), eps);
            left.RemoveAt(left.Count - 1);
            left.AddRange(right);
            return left;
        }

        return new List<Pt> { a, b };
    }

    private static double DistPointToSegment(Pt p, Pt a, Pt b)
    {
        var ab = b - a;
        double ab2 = ab.Len2();
        if (ab2 < 1e-18) return (p - a).Len();

        double t = ((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / ab2;
        t = Math.Max(0, Math.Min(1, t));
        var proj = new Pt(a.X + t * ab.X, a.Y + t * ab.Y);
        return (p - proj).Len();
    }

    private static List<Run> BuildRuns(List<Pt> pts, bool closed)
    {
        int n = pts.Count;
        int segCount = closed ? n : n - 1;
        if (segCount <= 0) return new List<Run>();

        var runs = new List<Run>();

        // helper to decide orientation
        Ori SegOri(Pt p0, Pt p1)
        {
            double dx = Math.Abs(p1.X - p0.X);
            double dy = Math.Abs(p1.Y - p0.Y);
            return (dx >= dy) ? Ori.H : Ori.V;
        }

        // start first run
        int start = 0;
        Ori curOri = SegOri(pts[0], pts[1 % n]);

        for (int i = 1; i < segCount; i++)
        {
            int i0 = i % n;
            int i1 = (i + 1) % n;
            var o = SegOri(pts[i0], pts[i1]);

            if (o != curOri)
            {
                runs.Add(FitRun(pts, start, i, curOri, closed));
                start = i;
                curOri = o;
            }
        }
        runs.Add(FitRun(pts, start, segCount, curOri, closed));

        // merge first/last if closed and same orientation
        if (closed && runs.Count >= 2 && runs[0].Orientation == runs[^1].Orientation)
        {
            var a = runs[^1];
            var b = runs[0];
            // const as median of both
            double c = Median(new[] { a.Const, b.Const });
            runs[0] = new Run(a.Orientation, c, a.A0, b.A1);
            runs.RemoveAt(runs.Count - 1);
        }

        return runs;
    }

    // Fit run to an axis-aligned line using median of constant coordinate
    private static Run FitRun(List<Pt> pts, int segStart, int segEnd, Ori ori, bool closed)
    {
        int n = pts.Count;

        // Collect all points in this run (segment endpoints)
        var runPts = new List<Pt>();
        int segCount = segEnd - segStart;

        for (int k = 0; k <= segCount; k++)
        {
            int idx = (segStart + k) % n;
            if (!closed && idx >= n) break;
            runPts.Add(pts[idx]);
        }

        if (runPts.Count < 2)
            runPts = new List<Pt> { pts[segStart % n], pts[(segStart + 1) % n] };

        if (ori == Ori.H)
        {
            double y = Median(runPts.Select(p => p.Y));
            double x0 = runPts.First().X;
            double x1 = runPts.Last().X;
            return new Run(Ori.H, y, x0, x1);
        }
        else
        {
            double x = Median(runPts.Select(p => p.X));
            double y0 = runPts.First().Y;
            double y1 = runPts.Last().Y;
            return new Run(Ori.V, x, y0, y1);
        }
    }

    private static List<Pt> IntersectionsToPolyline(List<Run> runs, bool closed)
    {
        var outPts = new List<Pt>(runs.Count);

        int m = runs.Count;
        int count = closed ? m : m - 1;

        for (int i = 0; i < count; i++)
        {
            var r0 = runs[i];
            var r1 = runs[(i + 1) % m];

            if (r0.Orientation == r1.Orientation)
            {
                // Should be merged earlier, but just skip
                continue;
            }

            Pt p = (r0.Orientation == Ori.V)
                ? new Pt(r0.Const, r1.Const)   // x from V, y from H
                : new Pt(r1.Const, r0.Const);

            outPts.Add(p);
        }

        if (!closed)
        {
            // for open polyline, also add an endpoint by projecting to first/last run
            // (often not needed for outlines; you can remove if you always process closed shapes)
        }

        return outPts;
    }

    private static List<Pt> RemoveTinyEdges(List<Pt> pts, double minEdgeLen, bool closed)
    {
        if (pts.Count < 3) return pts;
        double min2 = minEdgeLen * minEdgeLen;

        var outPts = new List<Pt>(pts.Count);
        outPts.Add(pts[0]);

        for (int i = 1; i < pts.Count; i++)
        {
            var p = pts[i];
            if ((p - outPts[^1]).Len2() >= min2)
                outPts.Add(p);
        }

        if (closed && outPts.Count > 2 && (outPts[0] - outPts[^1]).Len2() < min2)
            outPts.RemoveAt(outPts.Count - 1);

        return outPts;
    }

    private static List<Pt> MergeCollinear(List<Pt> pts, bool closed)
    {
        if (pts.Count < 3) return pts;

        bool IsCollinear(Pt a, Pt b, Pt c)
        {
            // since we expect axis-aligned, collinear if same X or same Y
            return (Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(b.X - c.X) < 1e-9) ||
                   (Math.Abs(a.Y - b.Y) < 1e-9 && Math.Abs(b.Y - c.Y) < 1e-9);
        }

        var outPts = new List<Pt>();
        int n = pts.Count;

        for (int i = 0; i < n; i++)
        {
            Pt prev = pts[(i - 1 + n) % n];
            Pt cur = pts[i];
            Pt next = pts[(i + 1) % n];

            if (!closed && (i == 0 || i == n - 1))
            {
                outPts.Add(cur);
                continue;
            }

            if (!IsCollinear(prev, cur, next))
                outPts.Add(cur);
        }

        return outPts;
    }

    private static double Median(IEnumerable<double> values)
    {
        var a = values.Where(v => !double.IsNaN(v) && !double.IsInfinity(v)).ToArray();
        if (a.Length == 0) return 0;
        Array.Sort(a);
        int mid = a.Length / 2;
        if (a.Length % 2 == 1) return a[mid];
        return 0.5 * (a[mid - 1] + a[mid]);
    }

    private static double Median(IEnumerable<double> vals, double fallback = 0)
    {
        var a = vals.ToArray();
        return a.Length == 0 ? fallback : Median(a);
    }
}
