using Godot;
using System;

public class CameraController : Camera2D
{
    [Serializable]
    public class Data
    {
        public Vector2 Position { get; set; }
        public Vector2 Zoom { get; set; }
    }

    private bool moveCamera;

    private Vector2 prevPos;

    public override void _Ready()
    {
        prevPos = Position;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton)
        {
            var mouseButtonEvent = @event as InputEventMouseButton;
            if (mouseButtonEvent.ButtonIndex == (int)ButtonList.Left)
            {
                if (mouseButtonEvent.Pressed)
                {
                    prevPos = mouseButtonEvent.Position;
                    moveCamera = true;
                }
                else
                {
                    moveCamera = false;
                }
            }
            else if (mouseButtonEvent.ButtonIndex == (int)ButtonList.WheelUp)
            {
                Zoom /= 1.1f;
            }
            else if (mouseButtonEvent.ButtonIndex == (int)ButtonList.WheelDown)
            {
                Zoom *= 1.1f;
            }
        }
        else if (@event is InputEventMouseMotion && moveCamera)
        {
            var mouseMotionEvent = @event as InputEventMouseMotion;
            GetTree().SetInputAsHandled();
            Position += (prevPos - mouseMotionEvent.Position) * Zoom.x;
            prevPos = mouseMotionEvent.Position;
        }
    }

    public Data Serialize()
    {
        var data = new Data();
        data.Position = GlobalPosition;
        data.Zoom = Zoom;
        return data;
    }

    public void Deserialize(Data data)
    {
        GlobalPosition = data.Position;
        Zoom = data.Zoom;
    }
}
