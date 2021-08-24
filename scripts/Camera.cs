using Godot;
using System;

public class Camera : Camera2D
{
    // Declare member variables here. Examples:
    [Export] public float Min_zoom = 0.1f;
    [Export] public float Max_zoom = 10f;

    public float CurrentZoom = 1f;

    // Called when the node enters the scene tree for the first time.
    // public override void _Ready()
    // {
        
    // }

    // zoom and camera smoothing adjustment
    public override void _PhysicsProcess(float delta)
	{
        if (Input.IsActionJustReleased("mousewheel_up") && CurrentZoom >= Min_zoom) {
            CurrentZoom *= 0.85f;
            this.Zoom = new Vector2(CurrentZoom, CurrentZoom);
            this.SmoothingSpeed = 12f - CurrentZoom;
        }
        if (Input.IsActionJustReleased("mousewheel_down") && CurrentZoom <= Max_zoom) {
            CurrentZoom *= 1.15f;
            this.Zoom = new Vector2(CurrentZoom, CurrentZoom);
            this.SmoothingSpeed = 12f - CurrentZoom;
        }

        if (SmoothingSpeed < 2) {
            SmoothingSpeed = 2;
        }
    }
}
