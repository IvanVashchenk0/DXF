using System;
using System.Collections.Generic;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

/// <summary>
/// Integration layer between netDxf and PolylineOrthogonalizer.
/// Handles loading DXF files, processing polylines, and saving results.
/// </summary>
public static class DxfOrthogonalizer
{
    /// <summary>
    /// Orthogonalizes all closed polylines on a specific layer.
    /// </summary>
    public static int OrthogonalizeAllClosedPolylinesOnLayer(
        DxfDocument doc,
        string layerName,
        double minStep = 1.0,
        double rdpEps = 3.0,
        double minEdgeLen = 2.0,
        bool onlyClosed = true)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("Layer name cannot be empty", nameof(layerName));

        var layer = doc.Layers.FirstOrDefault(l => l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase));
        if (layer == null)
        {
            Console.WriteLine($"Warning: Layer '{layerName}' not found in DXF file.");
            return 0;
        }

        int processedCount = 0;

        // Process Polyline2D entities (LWPOLYLINE)
        foreach (var poly2D in doc.Entities.Polylines2D)
        {
            if (poly2D.Layer != null && poly2D.Layer.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase))
            {
                if (!onlyClosed || poly2D.IsClosed)
                {
                    if (OrthogonalizePolyline2D(poly2D, minStep, rdpEps, minEdgeLen))
                        processedCount++;
                }
            }
        }

        // Process Polyline3D entities
        foreach (var poly3D in doc.Entities.Polylines3D)
        {
            if (poly3D.Layer != null && poly3D.Layer.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase))
            {
                if (!onlyClosed || poly3D.IsClosed)
                {
                    if (OrthogonalizePolyline3D(poly3D, minStep, rdpEps, minEdgeLen))
                        processedCount++;
                }
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Orthogonalizes all closed polylines in the document (any layer).
    /// </summary>
    public static int OrthogonalizeAllClosedPolylines(
        DxfDocument doc,
        double minStep = 1.0,
        double rdpEps = 3.0,
        double minEdgeLen = 2.0)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        int processedCount = 0;

        // Process Polyline2D entities (LWPOLYLINE)
        foreach (var poly2D in doc.Entities.Polylines2D)
        {
            if (poly2D.IsClosed)
            {
                if (OrthogonalizePolyline2D(poly2D, minStep, rdpEps, minEdgeLen))
                    processedCount++;
            }
        }

        // Process Polyline3D entities
        foreach (var poly3D in doc.Entities.Polylines3D)
        {
            if (poly3D.IsClosed)
            {
                if (OrthogonalizePolyline3D(poly3D, minStep, rdpEps, minEdgeLen))
                    processedCount++;
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Orthogonalizes a single Polyline2D entity (LWPOLYLINE).
    /// </summary>
    private static bool OrthogonalizePolyline2D(Polyline2D poly, double minStep, double rdpEps, double minEdgeLen)
    {
        if (poly.Vertexes == null || poly.Vertexes.Count < 2) return false;

        // Convert netDxf vertices to PolylineOrthogonalizer.Pt
        var pts = poly.Vertexes
            .Select(v => new PolylineOrthogonalizer.Pt(v.Position.X, v.Position.Y))
            .ToList();

        bool closed = poly.IsClosed;

        // Run orthogonalization
        var cleaned = PolylineOrthogonalizer.StraightenToRightAngles(
            pts,
            closed: closed,
            minStep: minStep,
            rdpEps: rdpEps,
            minEdgeLen: minEdgeLen
        );

        if (cleaned.Count < 2) return false;

        // Write back to polyline
        poly.Vertexes.Clear();
        foreach (var p in cleaned)
        {
            poly.Vertexes.Add(new Polyline2DVertex(p.X, p.Y, 0.0)); // bulge = 0.0 for straight segments
        }
        poly.IsClosed = closed;

        return true;
    }

    /// <summary>
    /// Orthogonalizes a single Polyline3D entity (using 2D coordinates).
    /// </summary>
    private static bool OrthogonalizePolyline3D(Polyline3D poly, double minStep, double rdpEps, double minEdgeLen)
    {
        if (poly.Vertexes == null || poly.Vertexes.Count < 2) return false;

        // Convert netDxf vertices to PolylineOrthogonalizer.Pt (using 2D projection)
        // Polyline3D vertices are Vector3 directly
        var pts = poly.Vertexes
            .Select(v => new PolylineOrthogonalizer.Pt(v.X, v.Y))
            .ToList();

        bool closed = poly.IsClosed;

        // Run orthogonalization
        var cleaned = PolylineOrthogonalizer.StraightenToRightAngles(
            pts,
            closed: closed,
            minStep: minStep,
            rdpEps: rdpEps,
            minEdgeLen: minEdgeLen
        );

        if (cleaned.Count < 2) return false;

        // Write back to polyline
        poly.Vertexes.Clear();
        foreach (var p in cleaned)
        {
            var vertex = new netDxf.Vector3(p.X, p.Y, 0.0); // Z = 0.0, keep original Z if needed
            poly.Vertexes.Add(vertex);
        }
        poly.IsClosed = closed;

        return true;
    }
}

