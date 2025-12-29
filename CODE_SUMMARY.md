# Complete Code Summary - DXF Polyline Orthogonalizer

## Overview
This codebase processes DXF files to clean up noisy polylines into crisp, right-angled (orthogonal) shapes with horizontal and vertical edges only.

---

## File 1: DxfTool.cs (CLI Entry Point)

### Purpose
Command-line interface that orchestrates the entire process.

### Execution Flow

#### 1. **Argument Parsing** (lines 11-64)
- **Iteration**: `for (int i = 2; i < args.Length; i++)` - parses command-line arguments
- **API Calls**: None
- **Logic**: Switch statement processes flags: `--layer`, `--eps`, `--minStep`, `--minEdgeLen`, `--all`, `--help`

#### 2. **File Validation** (lines 66-81)
- **API Calls**: 
  - `File.Exists(inputPath)` - checks if file exists
  - `new FileInfo(inputPath)` - gets file metadata
  - `fileInfo.Length` - gets file size in bytes
- **Iterations**: None
- **Output**: Prints file size in MB

#### 3. **DXF Loading** (lines 83-118)
- **API Calls**:
  - `DxfDocument.Load(inputPath)` - **MAIN LOAD OPERATION** (netDxf library)
    - Parses entire DXF file from disk
    - Builds in-memory document structure
    - This is where it hangs on large/complex files
  - `doc.Entities.Count` - total entity count
  - `doc.Entities.Polylines2D.Count` - lightweight polyline count
  - `doc.Entities.Polylines3D.Count` - 3D polyline count
- **Time Measurement**: `DateTime.Now` captures load time

#### 4. **Polyline Processing** (lines 120-140)
- **API Calls**: 
  - `DxfOrthogonalizer.OrthogonalizeAllClosedPolylines()` OR
  - `DxfOrthogonalizer.OrthogonalizeAllClosedPolylinesOnLayer()`
- **Conditional Logic**: Branches based on `--all` flag or `layerName` parameter

#### 5. **DXF Saving** (lines 144-145)
- **API Calls**: 
  - `doc.Save(outputPath)` - writes modified DXF to disk (netDxf library)
  - Serializes entire document structure to DXF format

---

## File 2: DxfOrthogonalizer.cs (DXF Integration Layer)

### Purpose
Bridges netDxf library with PolylineOrthogonalizer algorithm.

### Methods

#### Method 1: `OrthogonalizeAllClosedPolylinesOnLayer()` (lines 17-64)

**API Calls:**
- `doc.Layers.FirstOrDefault()` - LINQ query to find layer by name (case-insensitive)
- `layer.Name.Equals()` - string comparison

**Iterations:**
1. **`foreach (var poly2D in doc.Entities.Polylines2D)`** (line 38)
   - Iterates ALL Polyline2D entities in document
   - Checks: `poly2D.Layer.Name.Equals(layerName)` - layer matching
   - Checks: `poly2D.IsClosed` - closed polyline check
   - **Nested Call**: `OrthogonalizePolyline2D()` for each matching polyline

2. **`foreach (var poly3D in doc.Entities.Polylines3D)`** (line 51)
   - Iterates ALL Polyline3D entities in document
   - Same filtering logic as above
   - **Nested Call**: `OrthogonalizePolyline3D()` for each matching polyline

**Returns**: Count of successfully processed polylines

#### Method 2: `OrthogonalizeAllClosedPolylines()` (lines 69-100)

**Iterations:**
- Same two `foreach` loops as Method 1, but without layer filtering
- Only checks `IsClosed` property

#### Method 3: `OrthogonalizePolyline2D()` (lines 105-136)

**API Calls:**
- `poly.Vertexes` - accesses vertex collection
- `poly.Vertexes.Count` - vertex count check
- `poly.Vertexes.Select()` - LINQ projection to convert vertices
  - `v.Position.X`, `v.Position.Y` - extracts 2D coordinates from each vertex
- `poly.Vertexes.Clear()` - removes all existing vertices
- `poly.Vertexes.Add()` - adds new vertices back
- `new Polyline2DVertex()` - creates new vertex objects
- `poly.IsClosed` - reads/writes closed state

**Iterations:**
1. **`poly.Vertexes.Select(v => ...).ToList()`** (lines 110-112)
   - Iterates ALL vertices in polyline
   - Converts each `Polyline2DVertex` → `PolylineOrthogonalizer.Pt`
   - **Result**: `List<Pt>` with same count as original vertices

2. **`PolylineOrthogonalizer.StraightenToRightAngles()`** (line 117)
   - **MAIN ALGORITHM CALL** - processes the point list (see File 3)

3. **`foreach (var p in cleaned)`** (line 129)
   - Iterates cleaned/orthogonalized points
   - Creates new `Polyline2DVertex` for each point
   - Adds to polyline (replacing original vertices)

**Data Transformation:**
```
DXF Polyline2D → List<Pt> → [Orthogonalization Algorithm] → List<Pt> → DXF Polyline2D
```

#### Method 4: `OrthogonalizePolyline3D()` (lines 141-174)

**Similar to Method 3, but:**
- Uses `v.X, v.Y` directly (Vector3, not Position property)
- Creates `new Vector3()` instead of `Polyline2DVertex`
- Sets Z=0.0 for all vertices (2D projection)

---

## File 3: PolylineOrthogonalizer.cs (Core Algorithm)

### Purpose
Takes a noisy point list and converts it to clean right-angled polyline.

### Main Method: `StraightenToRightAngles()` (lines 38-68)

**Algorithm Pipeline (5 stages):**

#### Stage 1: FilterMinStep (line 49)
**Purpose**: Remove duplicate/near-duplicate points

**Iterations:**
- `for (int i = 1; i < input.Count; i++)` (line 80)
  - Iterates through all input points
  - **Calculation per iteration**: `(p - prev).Len2()` - squared distance check
  - Only keeps points further than `minStep` apart

**Complexity**: O(n) where n = input.Count

#### Stage 2: RDP (Ramer-Douglas-Peucker) (line 52)
**Purpose**: Simplify polyline by removing points that don't significantly affect shape

**Iterations (Recursive):**
- **Closed polylines** (lines 105-127):
  1. `for (int i = 1; i < pts.Count; i++)` (line 108)
     - Finds farthest point from first point (splitting point)
     - Calculates distance: `(pts[i] - pts[0]).Len2()`
  2. Two loops to "open" the closed polyline (lines 114-116)
     - Rotates points so split point becomes start/end
  3. Calls `RdpOpen()` recursively

- **RdpOpen()** (lines 130-155):
  - **Recursive function** that divides and conquers
  1. `for (int i = 1; i < pts.Count - 1; i++)` (line 139)
     - Finds point farthest from line segment (first to last point)
     - **Per iteration**: `DistPointToSegment()` calculates distance
  2. If max distance > epsilon:
     - Recursively processes left half: `RdpOpen(left)`
     - Recursively processes right half: `RdpOpen(right)`
     - Merges results

**Complexity**: O(n log n) typical, O(n²) worst case

**API Calls:**
- `DistPointToSegment()` - geometric calculation (lines 157-167)

#### Stage 3: BuildRuns (line 57)
**Purpose**: Group consecutive segments into horizontal/vertical "runs"

**Iterations:**
- `for (int i = 1; i < segCount; i++)` (line 189)
  - Iterates through all segments
  - **Per iteration**: `SegOri()` determines if segment is H or V
  - Groups consecutive segments with same orientation

**Helper Functions:**
- `SegOri()` (lines 178-183):
  - Calculates: `Math.Abs(p1.X - p0.X)` vs `Math.Abs(p1.Y - p0.Y)`
  - Returns H (horizontal) if dx >= dy, else V (vertical)

- `FitRun()` (lines 219-251):
  - **Iterations**: `for (int k = 0; k <= segCount; k++)` (line 227)
    - Collects all points in a run
  - Calculates median coordinate (X for vertical, Y for horizontal)
  - **API Call**: `Median()` function (uses `Array.Sort()`)

**Complexity**: O(n)

#### Stage 4: IntersectionsToPolyline (line 61)
**Purpose**: Rebuild polyline by finding intersection points between consecutive runs

**Iterations:**
- `for (int i = 0; i < count; i++)` (line 260)
  - Iterates through all runs
  - For each pair (current, next):
    - If orientations differ (H+V or V+H):
      - Creates intersection point: `new Pt(r0.Const, r1.Const)`
    - If same orientation: skips (should be merged earlier)

**Complexity**: O(m) where m = number of runs (typically < n)

#### Stage 5: RemoveTinyEdges (line 64)
**Purpose**: Remove edges shorter than threshold

**Iterations:**
- `for (int i = 1; i < pts.Count; i++)` (line 295)
  - Checks distance: `(p - outPts[^1]).Len2()`
  - Only keeps points that create edges ≥ `minEdgeLen`

**Complexity**: O(k) where k = output size

#### Stage 6: MergeCollinear (line 65)
**Purpose**: Remove middle points in collinear sequences

**Iterations:**
- `for (int i = 0; i < n; i++)` (line 322)
  - For each point, checks if it's collinear with prev/next
  - **Helper**: `IsCollinear()` checks if three points have same X or Y
  - Only keeps points that aren't collinear

**Complexity**: O(k)

---

## Complete Data Flow

```
1. User Command Line
   ↓
2. DxfTool.Main()
   ├─ Parse arguments
   ├─ Validate file
   ├─ DxfDocument.Load() ← [HANG POINT - netDxf parsing]
   │   └─ Parses entire DXF from disk into memory
   │   └─ Creates Entity collections
   │
3. DxfOrthogonalizer.OrthogonalizeAllClosedPolylinesOnLayer()
   ├─ doc.Layers.FirstOrDefault() ← Find layer
   │
   ├─ foreach (poly2D in doc.Entities.Polylines2D) ← Iterate ALL polylines
   │   ├─ Check layer match
   │   ├─ Check IsClosed
   │   └─ OrthogonalizePolyline2D()
   │       ├─ poly.Vertexes.Select() ← Convert vertices to Pt[]
   │       │   └─ for each vertex: v.Position.X, v.Position.Y
   │       │
   │       ├─ PolylineOrthogonalizer.StraightenToRightAngles() ← ALGORITHM
   │       │   ├─ FilterMinStep() ← O(n) iteration
   │       │   ├─ Rdp() ← O(n log n) recursive
   │       │   │   └─ RdpOpen() recursively calls itself
   │       │   │   └─ DistPointToSegment() per point
   │       │   ├─ BuildRuns() ← O(n) iteration
   │       │   │   ├─ SegOri() per segment
   │       │   │   └─ FitRun() → Median() → Array.Sort()
   │       │   ├─ IntersectionsToPolyline() ← O(m) iteration
   │       │   ├─ RemoveTinyEdges() ← O(k) iteration
   │       │   └─ MergeCollinear() ← O(k) iteration
   │       │
   │       ├─ poly.Vertexes.Clear() ← Remove all
   │       └─ foreach (p in cleaned) ← Rebuild vertices
   │           └─ poly.Vertexes.Add(new Polyline2DVertex())
   │
   └─ foreach (poly3D in doc.Entities.Polylines3D) ← Same for 3D
       └─ [Similar flow with Vector3]
   ↓
4. DxfTool.Main()
   └─ doc.Save() ← Write DXF to disk
```

---

## Performance Characteristics

### Time Complexity (per polyline):
- **FilterMinStep**: O(n)
- **RDP**: O(n log n) typical, O(n²) worst case ⚠️ **BOTTLENECK**
- **BuildRuns**: O(n)
- **IntersectionsToPolyline**: O(m) where m << n typically
- **RemoveTinyEdges**: O(k) where k < n
- **MergeCollinear**: O(k)

**Overall**: Dominated by RDP algorithm

### Space Complexity:
- Multiple temporary lists created
- RDP recursion uses call stack
- Original + cleaned vertices in memory simultaneously

---

## Key API Dependencies

### netDxf Library:
- `DxfDocument.Load()` - file parsing ⚠️ **Can hang on large files**
- `DxfDocument.Save()` - file writing
- `doc.Entities.Polylines2D` - collection access
- `doc.Entities.Polylines3D` - collection access
- `doc.Layers` - layer lookup
- `Polyline2D.Vertexes` - vertex collection
- `Polyline2D.IsClosed` - property access
- `Polyline2DVertex` - vertex creation

### .NET Framework:
- `File.Exists()`, `FileInfo`
- `DateTime.Now` - timing
- `List<T>`, `IEnumerable<T>`
- `Math.Abs()`, `Math.Max()`, `Math.Min()`, `Math.Sqrt()`
- `Array.Sort()` - used in Median()
- LINQ: `.Select()`, `.ToList()`, `.FirstOrDefault()`, `.Where()`

---

## Potential Hanging Points

1. **`DxfDocument.Load()`** - Line 92 in DxfTool.cs
   - Parses entire DXF file
   - Can take minutes for very large files
   - No progress indication
   - Single-threaded operation

2. **RDP Recursion** - PolylineOrthogonalizer.cs
   - Deep recursion on complex polylines
   - O(n²) worst case can be slow
   - No cancellation support

3. **Large Collections** - DxfOrthogonalizer.cs
   - Iterating through thousands of polylines
   - Each polyline processed sequentially

