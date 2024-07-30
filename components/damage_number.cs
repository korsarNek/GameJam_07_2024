using Godot;
using System;

public partial class damage_number : mount_ui
{
	private Label? _label;
	private AnimationPlayer? _animation;
	private string _text = "placeholder";

	public event Action<damage_number>? AnimationFinished;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

		_label = GetNode<Label>("Label");
		_animation = GetNode<AnimationPlayer>("AnimationPlayer");
		_animation.AnimationFinished += _AnimationFinished;

		_label.Text = _text;
		_animation.Play("fade up");
	}

	private void _AnimationFinished(StringName name)
	{
		AnimationFinished?.Invoke(this);
	}

	public void setText(string text)
	{
		_text = text;
		if (_label is not null)
		{
			_label.Text = text;
			_animation!.Play("fade up");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
	}
}
