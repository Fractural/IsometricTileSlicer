using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class InfiniteTriangleGrid : Node2D
{
    [Export]
    public bool ShowGrid { get; set; }
    [Export]
    public float GridLineThickness { get; set; } = 16f;
    [Export]
    public float GridLinePadding { get; set; } = 16f;
    [Export]
    public Color GridLineColor { get; set; } = Colors.Red;

    [OnReadyGet]
    private Camera2D camera;
    [OnReadyGet]
    private TriangleTileMap triTileMap;

    private Vector3Int prevCameraTriPos;
    private Vector2 prevZoom;

    [OnReady]
    public void RealReady()
    {
        prevCameraTriPos = triTileMap.PickTri(camera.GlobalPosition);
        prevZoom = camera.Zoom;
        Update();

        GetTree().Root.Connect("size_changed", this, nameof(OnSizeChanged));
    }

    private void OnSizeChanged()
    {
        Update();
    }

    public override void _Process(float delta)
    {
        var currCameraTriPos = triTileMap.PickTri(camera.GlobalPosition);
        if (currCameraTriPos != prevCameraTriPos || camera.Zoom != prevZoom)
        {
            // Draw only when there is a change.
            prevCameraTriPos = currCameraTriPos;
            prevZoom = camera.Zoom;
            Update();
        }
    }

    public void HardcodedDraw()
    {
        var viewportTransform = camera.GetViewportTransform();
        Rect2 visibleRectGlobal = viewportTransform.AffineInverse() * camera.GetViewportRect();

        // Add padding to visibleRectGlobal
        var paddedEnd = visibleRectGlobal.End + Vector2.One * GridLinePadding;
        var paddedStart = visibleRectGlobal.Position - Vector2.One * GridLinePadding;
        visibleRectGlobal.Position = paddedStart;
        visibleRectGlobal.End = paddedEnd;

        float diagonalLineLength = visibleRectGlobal.Size.y / Mathf.Sin(Mathf.Deg2Rad(30f));
        float verticalLineLength = visibleRectGlobal.Size.y;

        float horzEdgeLength = triTileMap.EdgeLength * Mathf.Cos(Mathf.Deg2Rad(30f));
        float vertEdgeLength = triTileMap.EdgeLength;

        Vector3Int topLeftTriPos = triTileMap.PickTri(visibleRectGlobal.Position);
        Vector2 topLeftCartPos = triTileMap.TriCorners(topLeftTriPos)[0];
        // Make sure totalHorzLengths is a multiple of two
        int totalHorzLengths = (int) (visibleRectGlobal.Size.x / (horzEdgeLength * 2)) * 2;
        int totalVertLengths = (int) (visibleRectGlobal.Size.y / vertEdgeLength);
        Vector2 topRightCartPos = new Vector2(topLeftCartPos.x + totalHorzLengths * horzEdgeLength, topLeftCartPos.y);

        int counter = 0;
        
        var verticalLine = Vector2.Down * verticalLineLength;
        var diagonalLineOne = Vector2.Right.Rotated(Mathf.Deg2Rad(30)) * diagonalLineLength;
        var diagonalLineTwo = Vector2.Right.Rotated(Mathf.Deg2Rad(180 - 30)) * diagonalLineLength;
        for (int i = 0; i <= totalHorzLengths; i ++)
        {
            var currTopPos = new Vector2(topLeftCartPos.x + i * horzEdgeLength, topLeftCartPos.y);
            if (counter % 2 == 0)
            {
                // Draw diagonal lines every 2 horizontal distances
                DrawLine(currTopPos, currTopPos + diagonalLineOne, GridLineColor, GridLineThickness);
                DrawLine(currTopPos, currTopPos + diagonalLineTwo, GridLineColor, GridLineThickness);
            }
            // Draw vertical line
            DrawLine(currTopPos, currTopPos + verticalLine, GridLineColor, GridLineThickness);

            counter++;
        }

        // Draw first and last positions
        for (int i = 1; i < totalVertLengths; i++)
        {
            var currTopLeftPos = new Vector2(topLeftCartPos.x, topLeftCartPos.y + i * vertEdgeLength);
            DrawLine(currTopLeftPos, currTopLeftPos + diagonalLineOne, GridLineColor, GridLineThickness);
            
            var currTopRightPos = new Vector2(topRightCartPos.x, topRightCartPos.y + i * vertEdgeLength);
            DrawLine(currTopRightPos, currTopRightPos + diagonalLineTwo, GridLineColor, GridLineThickness);
        }
    }

    public override void _Draw()
    {
        base._Draw();

        if (ShowGrid)
        {
            HardcodedDraw();
        }
    }

    public void DynamicDraw()
    {
        var viewportTransform = camera.GetViewportTransform();
        Rect2 visibleRectGlobal = viewportTransform.AffineInverse() * camera.GetViewportRect();

        float diagonalLineLength = visibleRectGlobal.Size.y / Mathf.Sin(Mathf.Deg2Rad(30f));
        float verticalLineLength = visibleRectGlobal.Size.y;

        float horzEdgeLength = triTileMap.EdgeLength * Mathf.Cos(Mathf.Deg2Rad(30f));

        // Get all triangles along the 
        Vector3Int topLeftTriPos = triTileMap.PickTri(visibleRectGlobal.Position);
        Vector2 topLeftCartPos = triTileMap.TriCorners(topLeftTriPos)[0];
        Vector2 topRightCartPos;
    }

    public Vector2 GetLeftMostTriCorner()
    {
        var viewportTransform = camera.GetViewportTransform();
        Rect2 visibleRectGlobal = viewportTransform.AffineInverse() * camera.GetViewportRect();

        Vector3Int topLeftTriPos = triTileMap.PickTri(visibleRectGlobal.Position);
        var triCorners = triTileMap.TriCorners(topLeftTriPos);
        
        return triCorners[0];
    }
}