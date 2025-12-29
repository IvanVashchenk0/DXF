# How to Run the DXF Polyline Orthogonalizer Tool

## Prerequisites
- .NET 8.0 SDK (already installed in your system)
- A DXF file to process

## Step-by-Step Instructions

### Step 1: Navigate to the Project Directory
```bash
cd /Users/ivanvashchenko/Desktop/docs-pacs/dxf_test/TestApp
```

### Step 2: Build the Project (Optional - dotnet run will build automatically)
```bash
dotnet build
```

### Step 3: Run the Tool

#### Option A: Process Polylines on a Specific Layer
```bash
dotnet run -- input.dxf output.dxf --layer LAYERNAME
```

**Example:**
```bash
dotnet run -- myfile.dxf result.dxf --layer OUTLINE
```

#### Option B: Process All Closed Polylines (All Layers)
```bash
dotnet run -- input.dxf output.dxf --all
```

**Example:**
```bash
dotnet run -- myfile.dxf result.dxf --all
```

#### Option C: With Custom Parameters
```bash
dotnet run -- input.dxf output.dxf --layer LAYERNAME --eps 3.0 --minStep 1.0 --minEdgeLen 2.0
```

**Example with short flags:**
```bash
dotnet run -- input.dxf output.dxf -l OUTLINE -e 3 -s 1 -m 2
```

### Step 4: View the Results
Open the output DXF file in AutoCAD or your preferred DXF viewer to verify the orthogonalized polylines.

## Command-Line Arguments

| Argument | Short | Description | Default |
|----------|-------|-------------|---------|
| `--layer NAME` | `-l` | Process polylines on specific layer | (all layers) |
| `--eps VALUE` | `-e` | RDP simplification tolerance | 3.0 |
| `--minStep VALUE` | `-s` | Minimum step distance | 1.0 |
| `--minEdgeLen VALUE` | `-m` | Minimum edge length after cleanup | 2.0 |
| `--all` | `-a` | Process all closed polylines on all layers | (disabled) |
| `--help` | `-h` | Show help message | - |

## Examples

### Example 1: Simple Layer Processing
```bash
dotnet run -- drawing.dxf cleaned.dxf --layer WALLS
```

### Example 2: Higher Precision (Smaller Tolerance)
```bash
dotnet run -- drawing.dxf cleaned.dxf --layer OUTLINE --eps 1.0
```

### Example 3: Process Everything
```bash
dotnet run -- drawing.dxf cleaned.dxf --all --eps 5.0
```

### Example 4: Custom Settings for Noisy Drawings
```bash
dotnet run -- noisy.dxf clean.dxf --layer FLOOR --eps 2.5 --minStep 0.5 --minEdgeLen 1.0
```

## Troubleshooting

### If you get "File not found" error:
- Make sure the input DXF file path is correct
- Use absolute paths if needed: `/full/path/to/file.dxf`

### If no polylines are processed:
- Check that the layer name is correct (case-insensitive)
- Verify the DXF file contains closed polylines on that layer
- Try using `--all` to see all available polylines

### If the output looks wrong:
- Adjust `--eps` parameter (larger = more aggressive simplification)
- Adjust `--minStep` (removes points closer than this distance)
- Adjust `--minEdgeLen` (removes edges shorter than this)

## Quick Test

To test if everything is working, run the help command:
```bash
dotnet run -- --help
```

This will display the usage information without processing any files.

