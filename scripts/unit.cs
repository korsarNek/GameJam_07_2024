using Godot;
using System;

public partial class unit : CharacterBody3D
{
	[Export]
	public int MovementRange = 5;

	[Export]
	public float MovementSpeed = 5;

	[Export]
	public int MaximumActionPoints = 2;

	private int _remainingActionPoints = 0;
	private NavigationAgent3D? _navigationAgent;
	private AnimationPlayer? _animationPlayer;

	[Signal]
	public delegate void ReachedTargetEventHandler();

	public void NavigateTo(Vector3 position)
	{
		_navigationAgent!.TargetPosition = position;
		_animationPlayer!.Play("running");
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_remainingActionPoints = MaximumActionPoints;
		_animationPlayer = (AnimationPlayer)FindChild("AnimationPlayer");
		var animation = _animationPlayer.GetAnimation("running");
		animation.LoopMode = Animation.LoopModeEnum.Linear;
		_navigationAgent.NavigationFinished += () => {
			_animationPlayer!.Stop();
			EmitSignal(SignalName.ReachedTarget);
		};
	}

    public override void _PhysicsProcess(double delta)
    {
		if (_navigationAgent!.IsNavigationFinished())
			return;

		var current = GlobalPosition;
		var next = _navigationAgent.GetNextPathPosition();

		Velocity = current.DirectionTo(next) * MovementSpeed;
		LookAt(next);
		MoveAndSlide();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		
	}
}
