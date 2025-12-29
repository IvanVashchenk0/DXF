using System;
using System.IO;
using netDxf;

/// <summary>
/// Command-line tool for orthogonalizing polylines in DXF files.
/// Usage: tool.exe input.dxf output.dxf [--layer LAYERNAME] [--eps EPSILON] [--minStep MINSTEP] [--minEdgeLen MINEDGELEN] [--all]
/// </summary>
public class DxfTool
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            Environment.Exit(1);
        }

        string inputPath = args[0];
        string outputPath = args[1];
        string? layerName = null;
        double eps = 3.0;
        double minStep = 1.0;
        double minEdgeLen = 2.0;
        bool allLayers = false;

        // Parse arguments
        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--layer":
                case "-l":
                    if (i + 1 < args.Length)
                    {
                        layerName = args[++i];
                    }
                    break;
                case "--eps":
                case "-e":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double epsVal))
                        eps = epsVal;
                    break;
                case "--minstep":
                case "-s":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double minStepVal))
                        minStep = minStepVal;
                    break;
                case "--minedgelen":
                case "-m":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double minEdgeLenVal))
                        minEdgeLen = minEdgeLenVal;
                    break;
                case "--all":
                case "-a":
                    allLayers = true;
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
            }
        }

        // Validate input file
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file '{inputPath}' not found.");
            Environment.Exit(1);
        }

        try
        {
            Console.WriteLine($"Loading DXF file: {inputPath}");
            var doc = DxfDocument.Load(inputPath);
            Console.WriteLine($"Loaded successfully. Processing polylines...");

            int processedCount = 0;

            if (allLayers)
            {
                Console.WriteLine($"Processing all closed polylines (all layers)...");
                processedCount = DxfOrthogonalizer.OrthogonalizeAllClosedPolylines(
                    doc, minStep, eps, minEdgeLen);
            }
            else if (!string.IsNullOrEmpty(layerName))
            {
                Console.WriteLine($"Processing closed polylines on layer: {layerName}");
                processedCount = DxfOrthogonalizer.OrthogonalizeAllClosedPolylinesOnLayer(
                    doc, layerName, minStep, eps, minEdgeLen);
            }
            else
            {
                Console.WriteLine("Warning: No layer specified and --all not used. Use --layer LAYERNAME or --all");
                Console.WriteLine("Processing all closed polylines anyway...");
                processedCount = DxfOrthogonalizer.OrthogonalizeAllClosedPolylines(
                    doc, minStep, eps, minEdgeLen);
            }

            Console.WriteLine($"Processed {processedCount} polylines.");

            Console.WriteLine($"Saving to: {outputPath}");
            doc.Save(outputPath);
            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("DXF Polyline Orthogonalizer Tool");
        Console.WriteLine("=================================");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  tool.exe <input.dxf> <output.dxf> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --layer, -l NAME     Process polylines on specific layer (default: all layers)");
        Console.WriteLine("  --eps, -e VALUE      RDP simplification tolerance (default: 3.0)");
        Console.WriteLine("  --minStep, -s VALUE  Minimum step distance (default: 1.0)");
        Console.WriteLine("  --minEdgeLen, -m VALUE  Minimum edge length after cleanup (default: 2.0)");
        Console.WriteLine("  --all, -a            Process all closed polylines on all layers");
        Console.WriteLine("  --help, -h           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  tool.exe in.dxf out.dxf --layer OUTLINE --eps 3 --minStep 1");
        Console.WriteLine("  tool.exe in.dxf out.dxf --all --eps 5");
        Console.WriteLine("  tool.exe in.dxf out.dxf --layer WALLS -e 2.5 -s 0.5");
    }
}

