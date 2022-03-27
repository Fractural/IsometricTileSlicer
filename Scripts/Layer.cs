using Godot;
using GodotOnReady.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = Godot.Color;

public partial class Layer : Node
{
    [Serializable]
    public class Data
    {
        public Color LayerColor { get; set; }
        public string LayerName { get; set; }
        public bool Visible { get; set; }
        public List<Vector3Int> TriangleTiles { get; set; } = new List<Vector3Int>();
    }

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
    
    public void Deserialize(Data data)
    {
        LayerName = data.LayerName;
        Visible = data.Visible;
        LayerColor = data.LayerColor;
        TriTileMap.AddTiles(data.TriangleTiles);
    }

    public Data Serialize()
    {
        var data = new Data();
        data.LayerName = LayerName;
        data.Visible = Visible;
        data.LayerColor = LayerColor;
        data.TriangleTiles = TriTileMap.TriangleTiles.ToList();
        return data;
    }
}
