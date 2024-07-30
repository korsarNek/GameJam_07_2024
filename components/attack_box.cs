using Godot;
using System;

public partial class attack_box : mount_ui
{
	public event Action<attack_box>? Clicked;
	public event Action<attack_box>? Hovered;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}

	public override void _GuiInput(InputEvent @event)
    {
		if (@event.IsActionPressed("ui_use_ability"))
			Clicked?.Invoke(this);
    }

	public void _MouseEntered()
	{
		Hovered?.Invoke(this);
	}
}
