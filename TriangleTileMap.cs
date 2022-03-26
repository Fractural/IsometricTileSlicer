using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TriangleTileMap : Node2D
{
    private float sqrt3 = Mathf.Sqrt(3);
    [Export]
    public float EdgeLength { get; set; }
    [Export]
    public float GridRotation { get; set; } = 30f;
    public HashSet<Vector3Int> TriangleTiles = new HashSet<Vector3Int>();

    // Returns a Rect2 that encloses all the triangle tiles
    public Rect2 GetBoundingRect()
    {
        Rect2 box = new Rect2();
        bool initBox = false;
        foreach (var triTile in TriangleTiles)
        {
            foreach (var corner in TriCorners(triTile))
            {
                // Initialize the box with the first corner we find
                if (!initBox)
                {
                    initBox = true;
                    box.Position = corner;
                    box.Size = Vector2.Zero;
                }
                box = box.Expand(corner);
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

    public Vector2 TriCenter(Vector3Int triPos)
    {
        var cartPos = new Vector2(
            0.5f * triPos.x + -0.5f * triPos.z, 
            -sqrt3 / 6 * triPos.x + sqrt3 / 3 * triPos.y - sqrt3 / 6 * triPos.z
            ) * EdgeLength;
        cartPos = cartPos.Rotated(Mathf.Deg2Rad(GridRotation));
        cartPos += GridOffset;
        return cartPos;
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
                TriCenter(new Vector3Int(1 + triPos.x, triPos.y, triPos.z)),
                TriCenter(new Vector3Int(triPos.x, triPos.y, 1 + triPos.z)),
                TriCenter(new Vector3Int(triPos.x, 1 + triPos.y, triPos.z)),
            };
        } else
        {
            return new Vector2[] {
                TriCenter(new Vector3Int(-1 + triPos.x, triPos.y, triPos.z)),
                TriCenter(new Vector3Int(triPos.x, triPos.y, -1 + triPos.z)),
                TriCenter(new Vector3Int(triPos.x, -1 + triPos.y, triPos.z)),
            };
        }
    }

    public bool TriPointsUp(Vector3Int triPos)
    {
        return triPos.x + triPos.y + triPos.z == 2;
    }

    public void DrawTriangle(Vector3Int triPosition)
    {
        Color color = Colors.Red;
        color.a = 0.5f;
        float width = 8f;
        var triPos = TriCorners(triPosition);
        DrawPolygon(triPos, new Color[] { color });
    }

    public override void _Draw()
    {
        base._Draw();

        foreach (var triPos in TriangleTiles)
        {
            DrawTriangle(triPos);
        }
    }
}
