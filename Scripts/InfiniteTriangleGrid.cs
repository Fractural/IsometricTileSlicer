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
    public Camera2D TargetCamera { get; set; }
    [OnReadyGet]
    public TriangleTileMap TargetTileMap { get; set; }

    private Vector3Int prevCameraTriPos;
    private Vector2 prevZoom;

    [OnReady]
    public void RealReady()
    {
        prevCameraTriPos = TargetTileMap.PickTri(TargetCamera.GlobalPosition);
        prevZoom = TargetCamera.Zoom;
        Update();

        GetTree().Root.Connect("size_changed", this, nameof(OnSizeChanged));
    }

    private void OnSizeChanged()
    {
        Update();
    }

    public override void _Process(float delta)
    {
        var currCameraTriPos = TargetTileMap.PickTri(TargetCamera.GlobalPosition);
        if (currCameraTriPos != prevCameraTriPos || TargetCamera.Zoom != prevZoom)
        {
            // Draw only when there is a change.
            prevCameraTriPos = currCameraTriPos;
            prevZoom = TargetCamera.Zoom;
            Update();
        }
    }

    public void HardcodedDraw()
    {
        var viewportTransform = TargetCamera.GetViewportTransform();
        Rect2 visibleRectGlobal = viewportTransform.AffineInverse() * TargetCamera.GetViewportRect();

        // Add padding to visibleRectGlobal
        var paddedEnd = visibleRectGlobal.End + Vector2.One * GridLinePadding;
        var paddedStart = visibleRectGlobal.Position - Vector2.One * GridLinePadding;
        visibleRectGlobal.Position = paddedStart;
        visibleRectGlobal.End = paddedEnd;

        //   Diagram
        //
        //                ...''|''...                 +
        //          ...'''     |     '''...           |
        //    ...'''           |           '''...     |--- vertExtent
        //   +-----------------+-----------------+    +
        //    '''...           |           ...'''     
        //          '''...     |     ...'''           
        //                '''..|..'''                 
        //
        //   +--------+--------+
        //            |
        //      horzExtend
        
        float vertExtent = TargetTileMap.EdgeLength * TargetTileMap.GridScale.y / 2f;
        float horzExtent = Mathf.Sqrt(TargetTileMap.EdgeLength * TargetTileMap.EdgeLength - TargetTileMap.EdgeLength / 2f * TargetTileMap.EdgeLength / 2f) * TargetTileMap.GridScale.x;

        // Half tri
        //
        // |''...               +
        // |     '''...         |
        // |        A .'''...   |--- vertExtent
        // +----------"------+  +
        //
        // +-------+---------+
        //         |
        //     horzExtent
        //
        // Let A = angle
        
        float angle = Mathf.Atan(vertExtent / horzExtent);

        float diagonalLineLength = visibleRectGlobal.Size.y / Mathf.Sin(angle);
        float verticalLineLength = visibleRectGlobal.Size.y;

        Vector3Int topLeftTriPos = TargetTileMap.PickTri(visibleRectGlobal.Position);
        Vector2 topLeftCartPos = TargetTileMap.TriCorners(topLeftTriPos)[0];
        // Make sure totalHorzLengths is a multiple of two
        int totalHorzExtents = (int) (visibleRectGlobal.Size.x / horzExtent) * 2;
        int totalDoubleVertExtents = (int) (visibleRectGlobal.Size.y / vertExtent * 2f);
        Vector2 topRightCartPos = new Vector2(topLeftCartPos.x + totalHorzExtents * horzExtent, topLeftCartPos.y);
        
        int counter = 0;
        
        var verticalLine = Vector2.Down * verticalLineLength;
        var diagonalLineOne = Vector2.Right.Rotated(angle) * diagonalLineLength;
        var diagonalLineTwo = Vector2.Right.Rotated(Mathf.Deg2Rad(180) - angle) * diagonalLineLength;
        for (int i = 0; i <= totalHorzExtents; i ++)
        {
            var currTopPos = new Vector2(topLeftCartPos.x + i * horzExtent, topLeftCartPos.y);
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
        for (int i = 1; i < totalDoubleVertExtents; i++)
        {
            var currTopLeftPos = new Vector2(topLeftCartPos.x, topLeftCartPos.y + i * vertExtent * 2f);
            DrawLine(currTopLeftPos, currTopLeftPos + diagonalLineOne, GridLineColor, GridLineThickness);
            
            var currTopRightPos = new Vector2(topRightCartPos.x, topRightCartPos.y + i * vertExtent * 2f);
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
        var viewportTransform = TargetCamera.GetViewportTransform();
        Rect2 visibleRectGlobal = viewportTransform.AffineInverse() * TargetCamera.GetViewportRect();

        float diagonalLineLength = visibleRectGlobal.Size.y / Mathf.Sin(Mathf.Deg2Rad(30f));
        float verticalLineLength = visibleRectGlobal.Size.y;

        float horzEdgeLength = TargetTileMap.EdgeLength * Mathf.Cos(Mathf.Deg2Rad(30f));

        // Get all triangles along the 
        Vector3Int topLeftTriPos = TargetTileMap.PickTri(visibleRectGlobal.Position);
        Vector2 topLeftCartPos = TargetTileMap.TriCorners(topLeftTriPos)[0];
        Vector2 topRightCartPos;
    }

    public Vector2 GetLeftMostTriCorner()
    {
        var viewportTransform = TargetCamera.GetViewportTransform();
        Rect2 visibleRectGlobal = viewportTransform.AffineInverse() * TargetCamera.GetViewportRect();

        Vector3Int topLeftTriPos = TargetTileMap.PickTri(visibleRectGlobal.Position);
        var triCorners = TargetTileMap.TriCorners(topLeftTriPos);
        
        return triCorners[0];
    }
}