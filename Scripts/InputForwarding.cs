using Godot;
using GodotOnReady.Attributes;
using System;

public partial class InputForwarding : ViewportContainer
{
    [OnReadyGet]
    private Viewport viewport;

    public override void _Input(InputEvent @event)
    {
        viewport.UnhandledInput(@event);
    }
}
