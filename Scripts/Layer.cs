using Godot;
using GodotOnReady.Attributes;
using Color = Godot.Color;

public partial class Layer : Node
{
    public void Construct(string layerName, bool visible, Color layerColor)
    {
        LayerName = layerName;
        Visible = visible;
        LayerColor = layerColor;
    }

    public string LayerName { get; set; }
    public bool Visible
    {
        get
        {
            return visible;
        }
        set
        {
            visible = value;
            TriTileMap.Visible = visible;
        }
    }
    private bool visible;
    public Color LayerColor
    {
        get
        {
            return layerColor;
        }
        set
        {
            layerColor = value;
            TriTileMap.TileColor = layerColor;
        }
    }
    private Color layerColor;

    [OnReadyGet]
    public TriangleTileMap TriTileMap { get; set; }
    
    public void Deserialize(string obj)
    {
        // TODO
    }

    public string Serialize()
    {
        // TODO
        return null;
    }
}
