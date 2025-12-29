using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Testing PolylineOrthogonalizer");
        Console.WriteLine("=============================\n");

        // Test case 1: Simple noisy rectangle
        var noisyRect = new List<PolylineOrthogonalizer.Pt>
        {
            new PolylineOrthogonalizer.Pt(0.0, 0.1),
            new PolylineOrthogonalizer.Pt(5.2, 0.3),
            new PolylineOrthogonalizer.Pt(10.1, 0.2),
            new PolylineOrthogonalizer.Pt(10.3, 5.1),
            new PolylineOrthogonalizer.Pt(10.2, 10.0),
            new PolylineOrthogonalizer.Pt(5.0, 10.2),
            new PolylineOrthogonalizer.Pt(0.1, 10.1),
            new PolylineOrthogonalizer.Pt(0.2, 5.0),
        };

        Console.WriteLine("Input (noisy rectangle):");
        foreach (var pt in noisyRect)
        {
            Console.WriteLine($"  ({pt.X:F2}, {pt.Y:F2})");
        }

        var result = PolylineOrthogonalizer.StraightenToRightAngles(noisyRect, closed: true);
        
        Console.WriteLine("\nOutput (orthogonalized):");
        foreach (var pt in result)
        {
            Console.WriteLine($"  ({pt.X:F2}, {pt.Y:F2})");
        }

        Console.WriteLine($"\nPoints reduced from {noisyRect.Count} to {result.Count}");

        // Test case 2: Open polyline
        Console.WriteLine("\n\nTest case 2: Open polyline");
        Console.WriteLine("===========================");
        
        var openPolyline = new List<PolylineOrthogonalizer.Pt>
        {
            new PolylineOrthogonalizer.Pt(0.0, 0.0),
            new PolylineOrthogonalizer.Pt(1.1, 0.2),
            new PolylineOrthogonalizer.Pt(5.0, 0.1),
            new PolylineOrthogonalizer.Pt(5.2, 3.1),
            new PolylineOrthogonalizer.Pt(5.1, 7.0),
        };

        Console.WriteLine("Input (open noisy polyline):");
        foreach (var pt in openPolyline)
        {
            Console.WriteLine($"  ({pt.X:F2}, {pt.Y:F2})");
        }

        var result2 = PolylineOrthogonalizer.StraightenToRightAngles(openPolyline, closed: false);
        
        Console.WriteLine("\nOutput (orthogonalized):");
        foreach (var pt in result2)
        {
            Console.WriteLine($"  ({pt.X:F2}, {pt.Y:F2})");
        }

        Console.WriteLine($"\nPoints reduced from {openPolyline.Count} to {result2.Count}");
        Console.WriteLine("\nTest completed successfully!");
    }
}

