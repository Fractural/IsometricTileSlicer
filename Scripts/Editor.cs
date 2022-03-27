using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.Json;
using Color = Godot.Color;

public partial class Editor : Control
{
    public enum Columns
    {
        LayerName = 0,
        VisibleToggle = 0,
    }

    public enum ButtonID
    {
        VisibleToggle = 0
    }

    public List<Layer> Layers { get; set; } = new List<Layer>();
    public Layer CurrentLayer { get; set; }

    private string loadedProjectPath;
    private string loadedTilesheetPath;
    private TreeItem prevSelectedItem;

    [Export]
    private PackedScene layerPrefab;
    [OnReadyGet]
    private Node2D layersHolder;

    [OnReadyGet]
    private CameraController cameraController;
    [OnReadyGet]
    private InfiniteTriangleGrid infiniteGrid;
    [OnReadyGet]
    private TextureRect savedTextureRect;
    [OnReadyGet]
    private TextureRect loadedTextureRect;

    [OnReadyGet]
    private Label loadedProjectLabel;
    [OnReadyGet]
    private Label loadedTilesheetLabel;

    [OnReadyGet]
    private Button saveProjectButton;
    [OnReadyGet]
    private FileDialog saveProjectDialog;

    [OnReadyGet]
    private Button loadProjectButton;
    [OnReadyGet]
    private FileDialog loadProjectDialog;

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
    private SpinBox gridScaleXSpinBox;
    [OnReadyGet]
    private SpinBox gridScaleYSpinBox;

    [OnReadyGet]
    private SpinBox tileBorderSpinBox;

    [OnReadyGet]
    private Button addLayerButton;
    [OnReadyGet]
    private Button deleteCurrentLayerButton;
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
    private JsonSerializerOptions jsonOptions = new JsonSerializerOptions();

    [OnReady]
    public void RealReady()
    {
        Reset();

        saveProjectButton.Connect("pressed", this, nameof(OnSaveProjectButtonPressed));
        loadProjectButton.Connect("pressed", this, nameof(OnLoadProjectButtonPressed));

        saveProjectDialog.Connect("file_selected", this, nameof(SaveProject));
        loadProjectDialog.Connect("file_selected", this, nameof(LoadProject));

        addLayerButton.Connect("pressed", this, nameof(AddNewLayer));
        deleteCurrentLayerButton.Connect("pressed", this, nameof(DeleteCurrentLayer));
        layerColorPickerButton.Connect("color_changed", this, nameof(OnLayerColorChanged));

        loadTilesheetButton.Connect("pressed", this, nameof(OnLoadTilesheetButtonPressed));
        saveTilesheetButton.Connect("pressed", this, nameof(OnSaveTilesheetButtonPressed));

        tileBorderEnabledToggle.Connect("toggled", this, nameof(OnTileBorderEnabledChanged));

        saveTilesheetDialog.Connect("file_selected", this, nameof(SaveSlicedTilesheet));
        loadTilesheetDialog.Connect("file_selected", this, nameof(LoadTilesheet));

        gridEdgeLengthSpinBox.Connect("value_changed", this, nameof(OnGridEdgeLengthChanged));
        gridOffsetYSpinBox.Connect("value_changed", this, nameof(OnGridOffsetChanged));
        gridOffsetXSpinBox.Connect("value_changed", this, nameof(OnGridOffsetChanged));

        gridScaleXSpinBox.Connect("value_changed", this, nameof(OnGridScaleChanged));
        gridScaleYSpinBox.Connect("value_changed", this, nameof(OnGridScaleChanged));

        tileBorderSpinBox.Connect("value_changed", this, nameof(OnTileBorderChanged));

        addLayerButton.Text = "";
        addLayerButton.Icon = GetIcon("Add", "EditorIcons");

        this.FocusMode = FocusModeEnum.Click;
        
        rng.Randomize();
        jsonOptions.IncludeFields = true;

        layersTree.HideRoot = true;
        layersTree.SelectMode = Tree.SelectModeEnum.Single;
        layersTree.Connect("button_pressed", this, nameof(OnLayerTreeButtonPressed));
        layersTree.Connect("item_selected", this, nameof(OnLayerTreeItemSelected));
        layersTree.Connect("item_edited", this, nameof(OnLayerTreeItemEdited));
        layersTree.Columns = 1;
        layersTree.SetColumnExpand((int)Columns.LayerName, true);

        UpdateTilemap(infiniteGrid.TargetTileMap);

        layerOptions.Visible = false;
    }

    public void Reset(bool resetLoadedProject = true)
    {
        loadedTextureRect.Texture = null;
        savedTextureRect.Texture = null;
        tileBorderSpinBox.Value = 16;
        gridEdgeLengthSpinBox.Value = 74;
        loadedTilesheetPath = "";
        if (resetLoadedProject)
        {
            loadedProjectPath = "";
            loadedProjectLabel.Text = "";
        }
        loadedTilesheetLabel.Text = "";
        tileBorderEnabledToggle.Pressed = false;

        gridScaleXSpinBox.Value = 1;
        gridScaleYSpinBox.Value = 1;

        gridOffsetXSpinBox.Value = 0;
        gridOffsetYSpinBox.Value = 32;

        foreach (var layer in Layers)
            layer.QueueFree();

        layersTree.Clear();
        layersTreeRoot = layersTree.CreateItem();

        Layers.Clear();
    }

    public void LoadProject(string path)
    {
        var file = new File();
        if (file.Open(path, File.ModeFlags.Read) == Error.Ok)
        {
            var deserializedData = JsonSerializer.Deserialize<Data>(file.GetAsText(), jsonOptions);
            loadedProjectPath = path;
            loadedProjectLabel.Text = loadedProjectPath;
            Deserialize(deserializedData);
        }
        else
        {
            GD.PrintErr("Could not open file for loading!");
        }
        file.Close();
    }

    public void SaveProject(string path)
    {
        var data = Serialize(path);
        var jsonData = JsonSerializer.Serialize(data, jsonOptions);
        var file = new File();
        if (file.Open(path, File.ModeFlags.Write) == Error.Ok)
        {
            file.StoreString(jsonData);
            loadedProjectPath = path;
            loadedProjectLabel.Text = path;
            loadedTilesheetLabel.Text = loadedProjectPath.TryGetSiblingOrChildRelativeFilePath(loadedTilesheetPath);
        } else
        {
            GD.PrintErr("Could not open file for saving!");
        }
        file.Close();
    }

    private void OnSaveProjectButtonPressed()
    {
        saveProjectDialog.PopupCentered();
    }

    private void OnLoadProjectButtonPressed()
    {
        loadProjectDialog.PopupCentered();
    }
    
    private void UpdateTilemap(TriangleTileMap tileMap)
    {
        tileMap.TileBorderEnabled = tileBorderEnabledToggle.Pressed;
        tileMap.TileBorder = (float)tileBorderSpinBox.Value;
        tileMap.EdgeLength = (float)gridEdgeLengthSpinBox.Value;
        tileMap.GridOffset = new Vector2((float)gridOffsetXSpinBox.Value, (float)gridOffsetYSpinBox.Value);
        tileMap.GridScale = new Vector2((float)gridScaleXSpinBox.Value, (float)gridScaleYSpinBox.Value);
    }

    private void OnLayerTreeItemEdited()
    {
        TreeItem editedItem = layersTree.GetEdited();
        int editedColumn = layersTree.GetEditedColumn();
        if (editedColumn == (int) Columns.LayerName)
        {
            var layer = editedItem.GetMetadata((int) Columns.LayerName) as Layer;
            layer.LayerName = editedItem.GetText(editedColumn);
        }
    }

    private void OnLayerTreeItemSelected()
    {
        var selectedItem = GetCurrentLayerItem();

        if (prevSelectedItem != null)
            prevSelectedItem.SetEditable((int)Columns.LayerName, false);
        prevSelectedItem = selectedItem;

        selectedItem.CallDeferred("set_editable", (int)Columns.LayerName, true);
        layerOptions.Visible = true;
        var selectedLayer = GetCurrentLayer();
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
        layerItem.SetEditable((int) Columns.LayerName, false);
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

    public void SaveSlicedTilesheet(string path)
    {
        // Exit if we don't have a texture loaded
        if (loadedTilesheetPath == "")
            return;

        // Save every layer that is visible
        foreach (var layer in Layers)
        {
            if (layer.Visible)
            {
                CSharpSaveImage(path.BaseName() + "_" + layer.LayerName + "." + path.Extension(), layer);
            }
        }
    }

    public void LoadTilesheet(string path)
    {
        var image = new Godot.Image();
        var absolutePath = loadedProjectPath.TryGetAbsolutePath(path);
        var relativePath = loadedProjectPath.TryGetSiblingOrChildRelativeFilePath(path);
        var err = image.Load(absolutePath);
        if (err == Error.Ok)
        {
            loadedTilesheetPath = absolutePath;
            loadedTilesheetLabel.Text = relativePath;

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
        infiniteGrid.TargetTileMap.EdgeLength = (float)gridEdgeLengthSpinBox.Value;
        foreach (var layer in Layers)
        {
            layer.TriTileMap.EdgeLength = (float)gridEdgeLengthSpinBox.Value;
            layer.TriTileMap.Update();
        }
        infiniteGrid.Update();
    }

    private void OnGridOffsetChanged(double newValue)
    {
        Vector2Int offset = new Vector2Int((int)gridOffsetXSpinBox.Value, (int)gridOffsetYSpinBox.Value);
        infiniteGrid.TargetTileMap.GridOffset = offset;
        foreach (var layer in Layers)
        {
            layer.TriTileMap.GridOffset = offset;
            layer.TriTileMap.Update();
        }
        infiniteGrid.Update();
    }


    private void OnGridScaleChanged(float newValue)
    {
        Vector2 scale = new Vector2((float) gridScaleXSpinBox.Value, (float) gridScaleYSpinBox.Value);
        infiniteGrid.TargetTileMap.GridScale = scale;
        foreach (var layer in Layers)
        {
            layer.TriTileMap.GridScale = scale;
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

        Bitmap bitmap = (Bitmap)Bitmap.FromFile(loadedTilesheetPath);
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

    private void AddLayerFromData(Layer.Data layerData)
    {
        var newLayer = layerPrefab.Instance<Layer>();
        layersHolder.AddChild(newLayer);
        newLayer.Deserialize(layerData);
        Layers.Add(newLayer);
        UpdateTilemap(newLayer.TriTileMap);

        // Add tree item
        TreeItem layerItem = layersTree.CreateItem(layersTreeRoot);
        layerItem.SetText((int)Columns.LayerName, layerData.LayerName);
        layerItem.SetEditable((int)Columns.LayerName, false);
        layerItem.AddButton((int)Columns.VisibleToggle, layerData.Visible ? GetIcon("GuiVisibilityVisible", "EditorIcons") : GetIcon("GuiVisibilityHidden", "EditorIcons"), (int)ButtonID.VisibleToggle);
        layerItem.SetMetadata((int)Columns.LayerName, newLayer);
    }

    public class Data
    {
        public List<Layer.Data> LayersData { get; set; } = new List<Layer.Data>();
        public CameraController.Data CameraData { get; set; }
        public int TileBorder { get; set; }
        public bool TileBorderEnabled { get; set; }
        public int GridEdgeLength { get; set; }
        public Vector2Int GridOffset { get; set; }
        public Vector2 GridScale { get; set; }
        public string LoadedTilesetPath { get; set; }
    }

    public Data Serialize(string savePath = "")
    {
        var data = new Data();
        
        data.GridScale = new Vector2((float) gridScaleXSpinBox.Value, (float) gridScaleYSpinBox.Value);
        data.GridOffset = new Vector2Int((int)gridOffsetXSpinBox.Value, (int)gridOffsetYSpinBox.Value);
        data.GridEdgeLength = (int)gridEdgeLengthSpinBox.Value;
        data.TileBorder = (int)tileBorderSpinBox.Value;
        data.TileBorderEnabled = tileBorderEnabledToggle.Pressed;

        data.LoadedTilesetPath = loadedTilesheetPath;

        if (savePath != "")
        {
            var baseDirPath = savePath.GetBaseDir();
            data.LoadedTilesetPath = loadedProjectPath.TryGetSiblingOrChildRelativeFilePath(loadedTilesheetPath);
        }

        data.CameraData = cameraController.Serialize();
        
        foreach (var layer in Layers)
            data.LayersData.Add(layer.Serialize());

        return data;
    }

    public void Deserialize(Data data)
    {
        Reset(false);

        // TODO: Make a dedicated node for Vector2
        gridScaleXSpinBox.Value = data.GridScale.x;
        gridScaleYSpinBox.Value = data.GridScale.y;
        gridOffsetXSpinBox.Value = data.GridOffset.x;
        gridOffsetYSpinBox.Value = data.GridOffset.y;
        gridEdgeLengthSpinBox.Value = data.GridEdgeLength;
        tileBorderSpinBox.Value = data.TileBorder;
        tileBorderEnabledToggle.Pressed = data.TileBorderEnabled;

        UpdateTilemap(infiniteGrid.TargetTileMap);
        LoadTilesheet(data.LoadedTilesetPath);

        cameraController.Deserialize(data.CameraData);

        // Add layers after the tilemap data has been loaded
        foreach (var layerData in data.LayersData)
            AddLayerFromData(layerData);
    }
}