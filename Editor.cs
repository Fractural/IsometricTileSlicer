using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

public partial class Editor : Control
{
    private string loadedTilesetPath;

    [OnReadyGet]
    private InfiniteTriangleGrid infiniteGrid;
    [OnReadyGet]
    private TriangleTileMap triTileMap;
    [OnReadyGet]
    private TextureRect savedTextureRect;
    [OnReadyGet]
    private TextureRect loadedTextureRect;

    [OnReadyGet]
    private Button loadTilesheetButton;
    [OnReadyGet]
    private FileDialog loadTilesheetDialog;

    [OnReadyGet]
    private Button saveTilesheetButton;
    [OnReadyGet]
    private FileDialog saveTilesheetDialog;

    [OnReadyGet]
    private SpinBox gridEdgeLengthSpinBox;

    [OnReadyGet]
    private SpinBox gridOffsetXSpinBox;
    [OnReadyGet]
    private SpinBox gridOffsetYSpinBox;

    [OnReadyGet]
    private SpinBox tileBorderSpinBox;

    [OnReadyGet]
    private Button addLayerButton;
    [OnReadyGet]
    private PopupDialog addLayerPopup;
    [OnReadyGet]
    private LineEdit newLayerLineEdit;
    [OnReadyGet]
    private PopupDialog renameLayerPopup;
    [OnReadyGet]
    private LineEdit renameLayerLineEdit;
    [OnReadyGet]
    private Tree layersTree;

    // TODO: Add tree items

    [OnReadyGet]
    private CheckBox tileBorderEnabledToggle;

    private bool erasing;
    private bool painting;

    [OnReady]
    public void RealReady()
    {
        tileBorderSpinBox.Value = triTileMap.TileBorder;
        gridEdgeLengthSpinBox.Value = triTileMap.EdgeLength;
        loadedTilesetPath = ProjectSettings.GlobalizePath(loadedTextureRect.Texture.ResourcePath);
        tileBorderEnabledToggle.Pressed = triTileMap.TileBorderEnabled;

        gridOffsetXSpinBox.Value = triTileMap.GridOffset.x;
        gridOffsetYSpinBox.Value = triTileMap.GridOffset.y;

        loadTilesheetButton.Connect("pressed", this, nameof(OnLoadTilesheetButtonPressed));
        saveTilesheetButton.Connect("pressed", this, nameof(OnSaveTilesheetButtonPressed));

        tileBorderEnabledToggle.Connect("toggled", this, nameof(OnTileBorderEnabledChanged));

        saveTilesheetDialog.Connect("file_selected", this, nameof(OnSaveTilesheetDialogFileSelected));
        loadTilesheetDialog.Connect("file_selected", this, nameof(OnLoadTilesheetDialogFileSelected));

        gridEdgeLengthSpinBox.Connect("value_changed", this, nameof(OnGridEdgeLengthChanged));
        gridOffsetYSpinBox.Connect("value_changed", this, nameof(OnGridOffsetChanged));
        gridOffsetXSpinBox.Connect("value_changed", this, nameof(OnGridOffsetChanged));

        tileBorderSpinBox.Connect("value_changed", this, nameof(OnTileBorderChanged));

        this.FocusMode = FocusModeEnum.Click;
    }

    public override void _Process(float delta)
    {
        if (painting)
        {
            var mouseTriPos = triTileMap.PickTri(triTileMap.GetGlobalMousePosition());
            triTileMap.AddTile(mouseTriPos);
        } else if (erasing)
        {
            var mouseTriPos = triTileMap.PickTri(triTileMap.GetGlobalMousePosition());
            triTileMap.RemoveTile(mouseTriPos);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mousebuttonEvent)
        {
            if (mousebuttonEvent.Pressed && mousebuttonEvent.ButtonIndex == (int) ButtonList.Left)
            {
                // Grab focus whenever we have an unhandled mouse event
                this.GrabFocus();
            }
        } else if (@event is InputEventKey keyEvent)
        {
            switch ((KeyList)keyEvent.Scancode)
            {
                case KeyList.W:
                    painting = keyEvent.Pressed;
                    GetTree().SetInputAsHandled();
                    break;
                case KeyList.E:
                    erasing = keyEvent.Pressed;
                    GetTree().SetInputAsHandled();
                    break;
            }
        }
    }

    private void OnTileBorderEnabledChanged(bool newValue)
    {
        triTileMap.TileBorderEnabled = newValue;
        triTileMap.Update();
    }

    private void OnTileBorderChanged(double newValue)
    {
        triTileMap.TileBorder = (float)tileBorderSpinBox.Value;
        triTileMap.Update();
    }

    private void OnSaveTilesheetDialogFileSelected(string path)
    {
        CSharpSaveImage(path);
    }

    private void OnLoadTilesheetDialogFileSelected(string path)
    {
        var image = new Godot.Image();
        var err = image.Load(path);
        if (err == Error.Ok)
        {
            loadedTilesetPath = path;

            var texture = new ImageTexture();
            texture.CreateFromImage(image);
            loadedTextureRect.Texture = texture;
        } else
        {
            GD.PrintErr($"Could not load image at \"{path}\"!");
        }
    }

    private void OnSaveTilesheetButtonPressed()
    {
        saveTilesheetDialog.PopupCentered();
    }

    private void OnLoadTilesheetButtonPressed()
    {
        loadTilesheetDialog.PopupCentered();
    }

    private void OnGridEdgeLengthChanged(double newValue)
    {
        triTileMap.EdgeLength = (int) gridEdgeLengthSpinBox.Value;
        triTileMap.Update();
        infiniteGrid.Update();
    }

    private void OnGridOffsetChanged(double newValue)
    {
        // Update grid offset
        triTileMap.GridOffset = new Vector2((float) gridOffsetXSpinBox.Value, (float) gridOffsetYSpinBox.Value);
        triTileMap.Update();
        infiniteGrid.Update();
    }

    public void CSharpSaveImage(string path)
    {
        var boundingRect = triTileMap.GetBoundingRect();

        GraphicsPath graphicsPath = new GraphicsPath();   // a Graphicspath
        foreach (var polygon in triTileMap.GetAllTilePolygons())
            graphicsPath.AddPolygon(polygon.Select(x => new PointF(x.x - boundingRect.Position.x, x.y - boundingRect.Position.y)).ToArray());        // with one Polygon

        Bitmap bitmap = (Bitmap)Bitmap.FromFile(loadedTilesetPath);
        Bitmap bitmap1 = new Bitmap((int) boundingRect.Size.x, (int)boundingRect.Size.y);
        bitmap1.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

        Graphics graphics = Graphics.FromImage(bitmap1);

        graphics.Clip = new Region(graphicsPath);   // restrict drawing region
            
        graphics.DrawImage(bitmap, -boundingRect.Position.x, -boundingRect.Position.y);   // draw clipped
        bitmap1.Save(path);

        var savedImageData = new Godot.Image();
        savedImageData.Load(path);
        var savedTexture = new ImageTexture();
        savedTexture.CreateFromImage(savedImageData);
        savedTextureRect.Texture = savedTexture;

        graphicsPath.Dispose();
    }

    // Too slow because this is single threaded
    public void GodotSaveImage()
    {
        var loadedTextureData = loadedTextureRect.Texture.GetData();
        var boundingRect = triTileMap.GetBoundingRect();

        var savedImageData = new Godot.Image();
        savedImageData.Create((int)boundingRect.Size.x, (int)boundingRect.Size.y, false, Godot.Image.Format.Rgba8);
        savedImageData.Lock();
        // Only scan in the bounding box of the tilemap
        for (int x = 0; x < (int)boundingRect.Size.x; x++)
        {
            for (int y = 0; y < (int)boundingRect.Size.y; y++)
            {
                var pixelPos = new Vector2((int)boundingRect.Position.x + x, (int)boundingRect.Position.y + y);
                if (triTileMap.HasTileAtCartPos(pixelPos))
                {
                    var pixel = loadedTextureData.GetPixelv(pixelPos);
                    savedImageData.SetPixel(x, y, pixel);
                }
            }
        }
        savedImageData.Unlock();

        var savedTexture = new ImageTexture();
        savedTexture.CreateFromImage(savedImageData);
        savedTextureRect.Texture = savedTexture;
    }
}