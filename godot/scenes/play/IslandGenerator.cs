using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Editor tool that procedurally generates an island from a Path3D outline.
///
/// Workflow:
///   1. Draw the island shape using the child Path3D node ("IslandPath") in the 3D editor.
///   2. Adjust the exported properties in the Inspector to tune the look.
///   3. Tick the "Generate" checkbox — the island mesh, collision, and trees are baked
///      into the scene as child nodes with zero runtime cost.
///   4. Save the scene and instance it in play.tscn under the Terrain node.
///
/// To regenerate: tick Generate again (existing generated nodes are cleared first).
/// To wipe without regenerating: tick Clear.
/// </summary>
[Tool]
public partial class IslandGenerator : Node3D
{
  // ──────────────────────────────── Exported Properties ────────────────────────────────

  [ExportGroup("Terrain")]
  /// <summary>Maximum height (meters) at the island's peak.</summary>
  [Export] public float MaxHeight = 4.0f;
  /// <summary>Height (meters) the shoreline rapidly rises to just inside the polygon edge,
  /// before flattening into a gradual slope toward MaxHeight.</summary>
  [Export] public float WaterHeight = 1.0f;
  /// <summary>
  /// Height ratio (0–1) below which the terrain is fully sandy.
  /// Above this value it transitions to grass.
  /// </summary>
  [Export] public float BeachFraction = 0.30f;
  /// <summary>FastNoiseLite frequency. Lower = larger noise features.</summary>
  [Export] public float NoiseScale = 0.08f;
  /// <summary>
  /// Number of subdivision passes. Each pass multiplies triangle count by 4.
  /// Depth 3 → 64× triangles per original. Keep ≤ 4 for reasonable mesh sizes.
  /// </summary>
  [Export(PropertyHint.Range, "1,5")] public int SubdivisionDepth = 3;

  [ExportGroup("Trees")]
  /// <summary>Target number of palm trees to scatter across the island.</summary>
  [Export] public int TreeCount = 12;
  /// <summary>Minimum height ratio (0–1) required before a tree is placed. Keeps trees off the beach.</summary>
  [Export] public float TreeHeightThreshold = 0.35f;
  /// <summary>Uniform scale applied to every palm tree.</summary>
  [Export] public float TreeScale = 1.0f;

  [ExportGroup("Generation")]
  /// <summary>Seed for both the noise and the tree-placement RNG.</summary>
  [Export] public int NoiseSeed = 42;

  /// <summary>
  /// Tick this in the Inspector to generate the island.
  /// Existing generated nodes are removed first, so it is safe to regenerate.
  /// </summary>
  [Export]
  public bool Generate
  {
    get => false;
    set { if (value) GenerateIsland(); }
  }

  /// <summary>Tick this in the Inspector to remove all generated child nodes.</summary>
  [Export]
  public bool Clear
  {
    get => false;
    set { if (value) ClearGenerated(); }
  }

  // ──────────────────────────────── Constants ────────────────────────────────

  // Colors are defined in sRGB (perceptual) space then converted to linear,
  // because Godot 4's rendering pipeline expects linear vertex colors.
  // Without this, values like (0.85, 0.75, 0.55) would appear washed out.
  private static readonly Color SandColor = new Color(0.91f, 0.70f, 0.56f).SrgbToLinear();
  private static readonly Color GrassColor = new Color(0.32f, 0.71f, 0.52f).SrgbToLinear();

  private const string PathNodeName = "IslandPath";
  private const string TerrainNodeName = "Terrain";
  private const string CollisionNodeName = "TerrainCollision";
  private const string TreesNodeName = "Trees";

  private const string PalmStraightPath = "res://art/kenny_pirate/palm-detailed-straight.glb";
  private const string PalmBendPath = "res://art/kenny_pirate/palm-detailed-bend.glb";

  // ──────────────────────────────── Main Entry Point ────────────────────────────────

  private void GenerateIsland()
  {
    if (!Engine.IsEditorHint()) return;

    ClearGenerated();

    var path = GetNodeOrNull<Path3D>(PathNodeName);
    if (path?.Curve == null || path.Curve.PointCount < 3)
    {
      GD.PrintErr($"IslandGenerator: Need a child Path3D named '{PathNodeName}' with at least 3 control points.");
      return;
    }

    // ── 1. Build polygon from path ──────────────────────────────────────────
    var polygon = BuildPolygonFromPath(path);
    if (polygon.Count < 3)
    {
      GD.PrintErr("IslandGenerator: Polygon has fewer than 3 vertices after deduplication.");
      return;
    }
    var poly = polygon.ToArray();

    // ── 2. Triangulate ──────────────────────────────────────────────────────
    var indices = Geometry2D.TriangulatePolygon(poly);
    if (indices.Length == 0)
    {
      GD.PrintErr("IslandGenerator: Triangulation failed. Ensure the path outline is non-self-intersecting and wound counter-clockwise.");
      return;
    }

    // ── 3. Build initial triangle list ──────────────────────────────────────
    var triangles = new List<(Vector2 a, Vector2 b, Vector2 c)>(indices.Length / 3);
    for (int i = 0; i < indices.Length; i += 3)
      triangles.Add((poly[indices[i]], poly[indices[i + 1]], poly[indices[i + 2]]));

    // ── 4. Subdivide ────────────────────────────────────────────────────────
    for (int d = 0; d < SubdivisionDepth; d++)
    {
      var next = new List<(Vector2, Vector2, Vector2)>(triangles.Count * 4);
      foreach (var (a, b, c) in triangles)
      {
        var ab = (a + b) * 0.5f;
        var bc = (b + c) * 0.5f;
        var ca = (c + a) * 0.5f;
        next.Add((a, ab, ca));
        next.Add((ab, b, bc));
        next.Add((ca, bc, c));
        next.Add((ab, bc, ca));
      }
      triangles = next;
    }

    // ── 5. Height function setup ────────────────────────────────────────────
    var noise = new FastNoiseLite
    {
      Seed = NoiseSeed,
      Frequency = NoiseScale,
      NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex
    };

    // ── 6. Build mesh ───────────────────────────────────────────────────────
    // Compute minimum distance to polygon boundary for every unique mesh vertex.
    // This approach works correctly for concave polygons — no centroid required.
    var distCache = new Dictionary<Vector2, float>();
    foreach (var (a, b, c) in triangles)
      foreach (var pt in new[] { a, b, c })
        if (!distCache.ContainsKey(pt))
          distCache[pt] = MinDistToBoundary(pt, poly);

    // refDist = the largest boundary-distance found across all vertices.
    // Serves as the "fully interior" reference: distToEdge/refDist → [0,1].
    float refDist = 0f;
    foreach (var d in distCache.Values) refDist = Mathf.Max(refDist, d);
    if (refDist < 1e-6f) { GD.PrintErr("IslandGenerator: degenerate polygon — no interior."); return; }

    var heightCache = new Dictionary<Vector2, float>();
    foreach (var kvp in distCache)
      heightCache[kvp.Key] = ComputeHeight(kvp.Key, kvp.Value, refDist, noise);

    var st = new SurfaceTool();
    st.Begin(Mesh.PrimitiveType.Triangles);

    foreach (var (a, b, c) in triangles)
    {
      AddVertex(st, a, heightCache[a]);
      AddVertex(st, b, heightCache[b]);
      AddVertex(st, c, heightCache[c]);
    }

    st.GenerateNormals(); // flat normals — low-poly look
    var mesh = st.Commit();

    // ── 7. MeshInstance3D ───────────────────────────────────────────────────
    var mat = new StandardMaterial3D { VertexColorUseAsAlbedo = true };

    var mi = new MeshInstance3D { Name = TerrainNodeName, Mesh = mesh };
    mi.MaterialOverride = mat;
    AddChild(mi);
    mi.Owner = GetTree().EditedSceneRoot;

    // ── 8. Collision ────────────────────────────────────────────────────────
    var collShape = new CollisionShape3D { Name = "TerrainShape", Shape = mesh.CreateTrimeshShape() };

    var body = new StaticBody3D
    {
      Name = CollisionNodeName,
      CollisionLayer = 4, // Layer 3 = Terrain (bit 3 → value 4)
      CollisionMask = 7   // Layers 1-3
    };
    body.AddChild(collShape);
    collShape.Owner = GetTree().EditedSceneRoot;

    AddChild(body);
    body.Owner = GetTree().EditedSceneRoot;

    // ── 9. Trees ────────────────────────────────────────────────────────────
    PlaceTrees(poly, refDist, noise);

    GD.Print($"IslandGenerator: built {triangles.Count} triangles.");
  }

  // ──────────────────────────────── Mesh Helpers ────────────────────────────────

  private void AddVertex(SurfaceTool st, Vector2 xz, float h)
  {
    float ratio = MaxHeight > 0f ? h / MaxHeight : 0f;

    // Hard threshold: below BeachFraction → sand, at or above → grass.
    st.SetColor(ratio < BeachFraction ? SandColor : GrassColor);
    st.AddVertex(new Vector3(xz.X, h, xz.Y));
  }

  /// <summary>
  /// Height at a given XZ position.
  /// <paramref name="distToEdge"/> is the pre-computed minimum distance to any polygon edge.
  /// <paramref name="refDist"/> is the maximum such distance across all mesh vertices (the "most interior" point).
  /// Works correctly for concave polygons — no centroid casting required.
  /// </summary>
  private float ComputeHeight(Vector2 xz, float distToEdge, float refDist, FastNoiseLite noise)
  {
    // radial: 0 at polygon boundary, 1 at the most interior mesh point.
    float radial = Mathf.Clamp(distToEdge / refDist, 0f, 1f);

    // Two-phase height profile:
    //   Shore zone  (outer ~15% of radius): rapid rise from 0 → WaterHeight (cliff/bank)
    //   Hill zone   (remaining interior):   gradual rise from WaterHeight → MaxHeight
    const float shoreZone = 0.15f;
    float cliffT = Mathf.SmoothStep(0f, shoreZone, radial);
    float hillT  = Mathf.SmoothStep(shoreZone, 1.0f, radial);

    // Noise applied only to the hill so the cliff stays consistently steep.
    float noiseVal = (noise.GetNoise2D(xz.X, xz.Y) + 1f) * 0.5f;

    return cliffT * WaterHeight + hillT * (MaxHeight - WaterHeight) * (0.4f + 0.6f * noiseVal);
  }

  /// <summary>Minimum perpendicular distance from <paramref name="point"/> to any polygon edge.</summary>
  private static float MinDistToBoundary(Vector2 point, Vector2[] poly)
  {
    float min = float.MaxValue;
    for (int i = 0; i < poly.Length; i++)
    {
      Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
      Vector2 ab = b - a, ap = point - a;
      float t = Mathf.Clamp(ap.Dot(ab) / ab.LengthSquared(), 0f, 1f);
      float d = (ap - ab * t).Length();
      if (d < min) min = d;
    }
    return min;
  }

  // ──────────────────────────────── Tree Placement ────────────────────────────────

  private void PlaceTrees(Vector2[] poly, float refDist, FastNoiseLite noise)
  {
    var treesNode = new Node3D { Name = TreesNodeName };
    AddChild(treesNode);
    treesNode.Owner = GetTree().EditedSceneRoot;

    var palmStraight = GD.Load<PackedScene>(PalmStraightPath);
    var palmBend = GD.Load<PackedScene>(PalmBendPath);

    if (palmStraight == null || palmBend == null)
    {
      GD.PrintErr("IslandGenerator: Could not load palm tree scenes. Check art/kenny_pirate paths.");
      return;
    }

    var bounds = GetBounds(poly);
    var rng = new Random(NoiseSeed + 999);

    int placed = 0;
    int maxAttempts = TreeCount * 100;

    for (int attempt = 0; attempt < maxAttempts && placed < TreeCount; attempt++)
    {
      float x = (float)(bounds.Position.X + rng.NextDouble() * bounds.Size.X);
      float z = (float)(bounds.Position.Y + rng.NextDouble() * bounds.Size.Y);
      var pt = new Vector2(x, z);

      if (!Geometry2D.IsPointInPolygon(pt, poly)) continue;

      float h = ComputeHeight(pt, MinDistToBoundary(pt, poly), refDist, noise);
      if (MaxHeight > 0f && h / MaxHeight < Mathf.Max(BeachFraction, TreeHeightThreshold)) continue;

      var scene = rng.NextDouble() < 0.5 ? palmStraight : palmBend;
      var tree = scene.Instantiate<Node3D>();
      tree.Name = $"palm_{placed}";
      tree.Position = new Vector3(x, h, z);
      tree.RotationDegrees = new Vector3(0f, (float)(rng.NextDouble() * 360.0), 0f);
      tree.Scale = Vector3.One * TreeScale;

      treesNode.AddChild(tree);
      tree.Owner = GetTree().EditedSceneRoot;
      SetOwnerRecursive(tree, GetTree().EditedSceneRoot);

      placed++;
    }

    GD.Print($"IslandGenerator: placed {placed}/{TreeCount} trees.");
  }

  private void SetOwnerRecursive(Node node, Node owner)
  {
    foreach (var child in node.GetChildren())
    {
      child.Owner = owner;
      SetOwnerRecursive(child, owner);
    }
  }

  // ──────────────────────────────── Clear ────────────────────────────────

  private void ClearGenerated()
  {
    if (!Engine.IsEditorHint()) return;

    foreach (var name in new[] { TerrainNodeName, CollisionNodeName, TreesNodeName })
    {
      var node = GetNodeOrNull(name);
      if (node == null) continue;
      RemoveChild(node);
      node.Free();
    }
  }

  // ──────────────────────────────── Geometry Utilities ────────────────────────────────

  private static List<Vector2> BuildPolygonFromPath(Path3D path)
  {
    var pts = path.Curve.GetBakedPoints(); // evenly-sampled 3D points
    var result = new List<Vector2>(pts.Length);

    foreach (var p in pts)
      result.Add(new Vector2(p.X, p.Z));

    // Drop duplicate last point if the curve is closed
    if (result.Count > 1 && result[0].DistanceTo(result[^1]) < 0.05f)
      result.RemoveAt(result.Count - 1);

    return result;
  }

  private static Rect2 GetBounds(Vector2[] poly)
  {
    float minX = float.MaxValue, minZ = float.MaxValue;
    float maxX = float.MinValue, maxZ = float.MinValue;
    foreach (var p in poly)
    {
      minX = Mathf.Min(minX, p.X);
      minZ = Mathf.Min(minZ, p.Y);
      maxX = Mathf.Max(maxX, p.X);
      maxZ = Mathf.Max(maxZ, p.Y);
    }
    return new Rect2(minX, minZ, maxX - minX, maxZ - minZ);
  }
}
