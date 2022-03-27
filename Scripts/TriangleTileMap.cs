using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TriangleTileMap : Node2D
{
    private float sqrt3 = Mathf.Sqrt(3);
    [Export]
    public Color TileColor
    {
        get => tileColor;
        set
        {
            tileColor = value;
            Update();
        }
    }
    private Color tileColor = Colors.Red;
    [Export]
    public float EdgeLength { get; set; }
    [Export]
    public float TileBorder { get; set; } = 0;
    [Export]
    public bool TileBorderEnabled { get; set; } = false;
    [Export]
    public float GridRotation { get; set; } = 30f;
    public HashSet<Vector3Int> TriangleTiles = new HashSet<Vector3Int>();

    // Returns a Rect2 that encloses all the triangle tiles
    public Rect2 GetBoundingRect()
    {
        if (TileCount == 0)
            return new Rect2();

        Rect2 box = new Rect2();
        box.Position = TriCorners(TriangleTiles.First())[0];
        box.Size = Vector2.Zero;

        foreach (var polygon in GetAllTilePolygons())
        {
            foreach (var vertex in polygon)
            {
                box = box.Expand(vertex);
            }
        }
        return box;
    }

    public int TileCount => TriangleTiles.Count;

    public Vector2 GridOffset { get; set; }

    public bool HasTile(Vector3Int trianglePos)
    {
        return TriangleTiles.Contains(trianglePos);
    }

    public void AddTiles(IEnumerable<Vector3Int> addTiles)
    {
        foreach (var tile in addTiles)
            TriangleTiles.Add(tile);
    }

    public void RemoveTiles(IEnumerable<Vector3Int> removeTiles)
    {
        foreach (var tile in removeTiles)
            TriangleTiles.Remove(tile);
    }

    public bool HasTileAtCartPos(Vector2 cartPos)
    {
        return TriangleTiles.Contains(PickTri(cartPos));
    }

    public bool AddTile(Vector3Int trianglePos)
    {
        var result = TriangleTiles.Add(trianglePos);
        if (result)
            Update();
        return result;
    }

    public bool RemoveTile(Vector3Int trianglePos)
    {
        var result = TriangleTiles.Remove(trianglePos);
        if (result)
            Update();
        return result;
    }

    public void BuildTriMap(int size)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(new Vector3Int(0, 1, 0));
        while (queue.Count > 0)
        {
            var next = queue.Dequeue();
            if (!TriangleTiles.Contains(next) && Mathf.Abs(next.x) <= size && Mathf.Abs(next.y) <= size && Mathf.Abs(next.z) <= size)
            {
                TriangleTiles.Add(next);
                foreach (var pos in TriNeighbors(next))
                    queue.Enqueue(pos);
            }
        }
    }

    public Vector3Int[] TriNeighbors(Vector3Int triPos)
    {
        if (TriPointsUp(triPos))
        {
            return new Vector3Int[]
            {
                new Vector3Int(triPos.x - 1, triPos.y, triPos.z),
                new Vector3Int(triPos.x, triPos.y - 1, triPos.z),
                new Vector3Int(triPos.x, triPos.y, triPos.z - 1)
            };
        } else
        {
            return new Vector3Int[]
            {
                new Vector3Int(triPos.x + 1, triPos.y, triPos.z),
                new Vector3Int(triPos.x, triPos.y + 1, triPos.z),
                new Vector3Int(triPos.x, triPos.y, triPos.z + 1)
            };
        }
    }

    public Vector2 TriCenter(Vector3 triPos)
    {
        var cartPos = new Vector2(
            0.5f * triPos.x + -0.5f * triPos.z,
            -sqrt3 / 6 * triPos.x + sqrt3 / 3 * triPos.y - sqrt3 / 6 * triPos.z
            ) * EdgeLength;
        cartPos = cartPos.Rotated(Mathf.Deg2Rad(GridRotation));
        cartPos += GridOffset;
        return cartPos;
    }

    public Vector2 TriCenter(Vector3Int triPos)
    {
        return TriCenter((Vector3) triPos);
    }

    public Vector3Int PickTri(Vector2 cartPos)
    {
        cartPos -= GridOffset;
        cartPos = cartPos.Rotated(Mathf.Deg2Rad(-GridRotation));
        return new Vector3Int(
            (int) Mathf.Ceil((  1   * cartPos.x -   sqrt3       / 3 * cartPos.y) / EdgeLength),
            (int) Mathf.Floor((                     sqrt3 * 2   / 3 * cartPos.y) / EdgeLength) + 1,
            (int) Mathf.Ceil(( -1   * cartPos.x -   sqrt3       / 3 * cartPos.y) / EdgeLength));
    }

    public Vector2[] TriCorners(Vector3Int triPos)
    {
        if (TriPointsUp(triPos))
        {
            return new Vector2[] {
                TriCenter(new Vector3(1 + triPos.x, triPos.y, triPos.z)),
                TriCenter(new Vector3(triPos.x, triPos.y, 1 + triPos.z)),
                TriCenter(new Vector3(triPos.x, 1 + triPos.y, triPos.z)),
            };
        } else
        {
            return new Vector2[] {
                TriCenter(new Vector3(-1 + triPos.x, triPos.y, triPos.z)),
                TriCenter(new Vector3(triPos.x, triPos.y, -1 + triPos.z)),
                TriCenter(new Vector3(triPos.x, -1 + triPos.y, triPos.z)),
            };
        }
    }

    public bool TriPointsUp(Vector3Int triPos)
    {
        return triPos.x + triPos.y + triPos.z == 2;
    }

    public void DrawTriangle(Vector3Int triPosition)
    {
        Color color = TileColor;
        color.a = 0.5f;
        var triPos = TriCorners(triPosition);
        DrawPolygon(triPos, new Color[] { color });
    }

    public Vector2[] GetTilePolygon(Vector3Int triPosition)
    {
        var trianglePolygon = TriCorners(triPosition);
        if (!TileBorderEnabled || TileBorder == 0)
            return trianglePolygon;

        var godotPolygon = Geometry.OffsetPolyline2d(trianglePolygon.Append(trianglePolygon.First()).ToArray(), TileBorder, Geometry.PolyJoinType.Round, Geometry.PolyEndType.Round);
        var outlinePolygon = godotPolygon[0] as Vector2[];
        var finalPolygon = Geometry.MergePolygons2d(outlinePolygon, trianglePolygon)[0] as Vector2[];
        return finalPolygon;
    }

    public List<Vector2[]> GetAllTilePolygons()
    {
        List<Vector2[]> allTilePolygons = new List<Vector2[]>();
        HashSet<Vector3Int> checkedTriangleTiles = new HashSet<Vector3Int>();
        foreach (var triPos in TriangleTiles)
        {
            if (checkedTriangleTiles.Contains(triPos))
                continue;
            checkedTriangleTiles.Add(triPos);

            Vector2[] tilePolygon = GetTilePolygon(triPos);

            if (TileBorderEnabled && TileBorder > 0)
            {
                // Try merge this tile with neighbors
                Queue<Vector3Int> neighbors = new Queue<Vector3Int>();
                foreach (var neighborPos in TriNeighbors(triPos))
                    if (TriangleTiles.Contains(neighborPos) && !checkedTriangleTiles.Contains(neighborPos))
                        neighbors.Enqueue(neighborPos);
                while (neighbors.Count > 0)
                {
                    var currNeighbor = neighbors.Dequeue();
                    var godotPolygon = Geometry.MergePolygons2d(tilePolygon, GetTilePolygon(currNeighbor));
                    tilePolygon = godotPolygon[0] as Vector2[];
                    checkedTriangleTiles.Add(currNeighbor);
                    foreach (var neighborPos in TriNeighbors(currNeighbor))
                        if (TriangleTiles.Contains(neighborPos) && !checkedTriangleTiles.Contains(neighborPos))
                            neighbors.Enqueue(neighborPos);
                }
            }

            // We have merged all possible neighbors
            allTilePolygons.Add(tilePolygon);
        }

        // Try merge all polygons with each other

        if (TileBorderEnabled && TileBorder > 0)
            allTilePolygons = GeometryUtils.MergePolygons(allTilePolygons.ToArray()).ToList();

        return allTilePolygons;
    }

    public override void _Draw()
    {
        base._Draw();

        var allPolygons = GetAllTilePolygons();
        foreach (var polygon in allPolygons)
        {
            Color color = TileColor;
            color.a = 0.5f;
            DrawPolygon(polygon, new Color[] { color });
        }
    }
}
