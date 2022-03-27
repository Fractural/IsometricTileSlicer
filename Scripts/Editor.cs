using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Color = Godot.Color;

public partial class Editor : Control
{
    public enum Columns
    {
        LayerName = 0,
        VisibleToggle = 1,
    }

    public enum ButtonID
    {
        VisibleToggle = 0
    }

    public List<Layer> Layers { get; set; } = new List<Layer>();
    public Layer CurrentLayer { get; set; }

    private string loadedTilesetPath;

    [Export]
    private PackedScene layerPrefab;
    [OnReadyGet]
    private Node2D layersHolder;

    [OnReadyGet]
    private InfiniteTriangleGrid infiniteGrid;
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
    private Button deleteCurrentLayerButton;
    [OnReadyGet]
    private LineEdit layerNameLineEdit;
    [OnReadyGet]
    private ColorPickerButton layerColorPickerButton;
    [OnReadyGet]
    private Tree layersTree;

    [OnReadyGet]
    private CheckBox tileBorderEnabledToggle;

    [OnReadyGet]
    private Control layerOptions;

    private bool erasing;
    private bool painting;

    private TreeItem layersTreeRoot;

    private RandomNumberGenerator rng = new RandomNumberGenerator();

    [OnReady]
    public void RealReady()
    {
        tileBorderSpinBox.Value = 16;
        gridEdgeLengthSpinBox.Value = 74;
        loadedTilesetPath = ProjectSettings.GlobalizePath(loadedTextureRect.Texture.ResourcePath);
        tileBorderEnabledToggle.Pressed = false;

        gridOffsetXSpinBox.Value = 0;
        gridOffsetYSpinBox.Value = 32;

        addLayerButton.Connect("pressed", this, nameof(AddNewLayer));
        deleteCurrentLayerButton.Connect("pressed", this, nameof(DeleteCurrentLayer));
        layerNameLineEdit.Connect("text_changed", this, nameof(OnLayerNameChanged));
        layerColorPickerButton.Connect("color_changed", this, nameof(OnLayerColorChanged));

        loadTilesheetButton.Connect("pressed", this, nameof(OnLoadTilesheetButtonPressed));
        saveTilesheetButton.Connect("pressed", this, nameof(OnSaveTilesheetButtonPressed));

        tileBorderEnabledToggle.Connect("toggled", this, nameof(OnTileBorderEnabledChanged));

        saveTilesheetDialog.Connect("file_selected", this, nameof(OnSaveTilesheetDialogFileSelected));
        loadTilesheetDialog.Connect("file_selected", this, nameof(OnLoadTilesheetDialogFileSelected));

        gridEdgeLengthSpinBox.Connect("value_changed", this, nameof(OnGridEdgeLengthChanged));
        gridOffsetYSpinBox.Connect("value_changed", this, nameof(OnGridOffsetChanged));
        gridOffsetXSpinBox.Connect("value_changed", this, nameof(OnGridOffsetChanged));

        tileBorderSpinBox.Connect("value_changed", this, nameof(OnTileBorderChanged));

        addLayerButton.Text = "";
        addLayerButton.Icon = GetIcon("Add", "EditorIcons");

        this.FocusMode = FocusModeEnum.Click;
        
        rng.Randomize();
        SetupLayersTree();
    }

    private void SetupLayersTree()
    {
        layersTree.HideRoot = true;
        layersTreeRoot = layersTree.CreateItem();
        layersTree.SelectMode = Tree.SelectModeEnum.Row;
        layersTree.Connect("button_pressed", this, nameof(OnLayerTreeButtonPressed));
        layersTree.Connect("item_selected", this, nameof(OnLayerTreeItemSelected));
        layersTree.Columns = 2;
        layersTree.SetColumnExpand((int) Columns.LayerName, true);

        UpdateTilemap(infiniteGrid.TargetTileMap);

        layerOptions.Visible = false;
    }

    private void UpdateTilemap(TriangleTileMap tileMap)
    {
        tileMap.TileBorderEnabled = tileBorderEnabledToggle.Pressed;
        tileMap.TileBorder = (float)tileBorderSpinBox.Value;
        tileMap.EdgeLength = (int)gridEdgeLengthSpinBox.Value;
        tileMap.GridOffset = new Vector2((float)gridOffsetXSpinBox.Value, (float)gridOffsetYSpinBox.Value);
    }

    private void OnLayerTreeItemSelected()
    {
        layerOptions.Visible = true;
        var selectedLayer = GetCurrentLayer();
        layerNameLineEdit.Text = selectedLayer.LayerName;
        layerColorPickerButton.Color = selectedLayer.LayerColor;
    }

    public void AddNewLayer()
    {
        var newLayer = layerPrefab.Instance<Layer>();
        layersHolder.AddChild(newLayer);
        string nextFreeName = GetNextFreeLayerName();
        newLayer.Construct(nextFreeName, true, Color.Color8((byte) rng.RandiRange(0, 255), (byte)rng.RandiRange(0, 255), (byte)rng.RandiRange(0, 255)));
        Layers.Add(newLayer);
        UpdateTilemap(newLayer.TriTileMap);

        // Add tree item
        TreeItem layerItem = layersTree.CreateItem(layersTreeRoot);
        layerItem.SetText((int) Columns.LayerName, nextFreeName);
        layerItem.AddButton((int) Columns.VisibleToggle, GetIcon("GuiVisibilityVisible", "EditorIcons"), (int)ButtonID.VisibleToggle);
        layerItem.SetMetadata((int)Columns.LayerName, newLayer);
    }

    public void DeleteCurrentLayer()
    {
        TreeItem selectedLayerItem = layersTree.GetSelected();
        if (selectedLayerItem == null)
            return;

        layersTreeRoot.RemoveChild(selectedLayerItem);
        var selectedLayer = selectedLayerItem.GetMetadata((int) Columns.LayerName) as Layer;
        Layers.Remove(selectedLayer);
        layersHolder.RemoveChild(selectedLayer);
        selectedLayer.QueueFree();
        selectedLayerItem.Free();
    }

    public string GetNextFreeLayerName()
    {
        int currIdx = 0;
        string currNamePrefix = "New Layer ";
        while (Layers.Any(x => x.LayerName == currNamePrefix + currIdx))
        {
            currIdx++;
        }
        return currNamePrefix + currIdx;
    }

    private void OnLayerTreeButtonPressed(TreeItem item, int column, int id)
    {
        Layer targetLayer = (Layer) item.GetMetadata((int) Columns.LayerName);
        if (targetLayer == null)
            throw new Exception("Expected target layer to not be null!");

        if (column == (int) Columns.VisibleToggle)
        {
            targetLayer.Visible = !targetLayer.Visible;
            // Update visibility button
            item.SetButton((int) Columns.VisibleToggle, (int)ButtonID.VisibleToggle, targetLayer.Visible ? GetIcon("GuiVisibilityVisible", "EditorIcons") : GetIcon("GuiVisibilityHidden", "EditorIcons"));
        }
    }

    private void OnLayerNameChanged(string newName)
    {
        var currentLayer = GetCurrentLayer();
        if (currentLayer == null)
            return;
        currentLayer.LayerName = newName;
        var treeItem = GetCurrentLayerItem();
        treeItem.SetText((int) Columns.LayerName, newName);
    }

    private void OnLayerColorChanged(Color newColor)
    {
        var currentLayer = GetCurrentLayer();
        if (currentLayer == null)
            return;
        currentLayer.LayerColor = newColor;
    }

    public override void _Process(float delta)
    {
        if (GetCurrentLayer() == null)
            return;

        if (painting)
        {
            var currTileMap = GetCurrentLayer().TriTileMap;
            var mouseTriPos = currTileMap.PickTri(currTileMap.GetGlobalMousePosition());
            currTileMap.AddTile(mouseTriPos);
        } else if (erasing)
        {
            var currTileMap = GetCurrentLayer().TriTileMap;
            var mouseTriPos = currTileMap.PickTri(currTileMap.GetGlobalMousePosition());
            currTileMap.RemoveTile(mouseTriPos);
        }
    }

    private TreeItem GetCurrentLayerItem()
    {
        var selectedItem = layersTree.GetSelected();
        if (selectedItem == null)
            return null;
        return selectedItem;
    }

    private Layer GetCurrentLayer()
    {
        var selectedItem = layersTree.GetSelected();
        if (selectedItem == null)
            return null;
        return selectedItem.GetMetadata((int) Columns.LayerName) as Layer;
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
        // Not necessary for inifinite grid
        foreach (var layer in Layers)
        {
            layer.TriTileMap.TileBorderEnabled = newValue;
            layer.TriTileMap.Update();
        }
    }

    private void OnTileBorderChanged(double newValue)
    {
        // Not necessary for inifinite grid
        foreach (var layer in Layers)
        {
            layer.TriTileMap.TileBorder = (float)tileBorderSpinBox.Value;
            layer.TriTileMap.Update();
        }
    }

    private void OnSaveTilesheetDialogFileSelected(string path)
    {
        // Save every layer that is visible
        foreach (var layer in Layers)
        {
            if (layer.Visible)
            {
                CSharpSaveImage(path.BaseName() + "_" + layer.LayerName + "." + path.Extension(), layer);
            }
        }
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
        infiniteGrid.TargetTileMap.EdgeLength = (int)gridEdgeLengthSpinBox.Value;
        foreach (var layer in Layers)
        {
            layer.TriTileMap.EdgeLength = (int)gridEdgeLengthSpinBox.Value;
            layer.TriTileMap.Update();
        }
        infiniteGrid.Update();
    }

    private void OnGridOffsetChanged(double newValue)
    {
        infiniteGrid.TargetTileMap.GridOffset = new Vector2((float)gridOffsetXSpinBox.Value, (float)gridOffsetYSpinBox.Value);
        foreach (var layer in Layers)
        {
            layer.TriTileMap.GridOffset = new Vector2((float)gridOffsetXSpinBox.Value, (float)gridOffsetYSpinBox.Value);
            layer.TriTileMap.Update();
        }
        infiniteGrid.Update();
    }

    public void CSharpSaveImage(string path, Layer layer)
    {
        var triTileMap = layer.TriTileMap;
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
}