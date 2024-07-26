using Godot;
using System;
using System.Linq;

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
	private Timer? _timer;
	private bool _timerStarted = false;

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
		_navigationAgent.NavigationFinished += _TargetReached;
	}

	private void _TargetReached()
	{
		_animationPlayer!.Stop();
		EmitSignal(SignalName.ReachedTarget);
		if (_timerStarted)
		{
			_timer!.Stop();
			_timerStarted = false;
		}
	}

	private void _TimeoutReached()
	{
		//Teleport to target location if we are stuck.
		Position = _navigationAgent!.TargetPosition;
		_TargetReached();
	}

    public override void _PhysicsProcess(double delta)
    {
		if (_navigationAgent!.IsNavigationFinished())
			return;
		else if (!_timerStarted)
		{
			if (_timer is null)
			{
                _timer = new Timer
                {
                    OneShot = true,
					Autostart = true,
                };
				_timer.Timeout += _TimeoutReached;
                AddChild(_timer);
			}
			var length = _navigationAgent.GetCurrentNavigationResult().Path.Select(v => v.Length()).Sum();
			_timer.Start(length / MovementSpeed * 1.25); //25% buffer 
			_timerStarted = true;
		}

		var current = GlobalPosition;
		var next = _navigationAgent.GetNextPathPosition();
		var velocity = current.DirectionTo(next) * MovementSpeed;

		Velocity = velocity;
		LookAt(next);
		// Kick objects around if we are colliding.
		var collision = MoveAndCollide(velocity * (float)delta, testOnly: true);
		if (collision?.GetCollisionCount() > 0 && collision.GetCollider() is RigidBody3D body)
		{
			var direction = collision.GetNormal();
			var position = collision.GetPosition();
			body.ApplyForce(direction.Rotated(UpDirection, MathF.PI) * MovementSpeed, body.ToLocal(position));
		}
		MoveAndSlide();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		
	}

    protected override void Dispose(bool disposing)
    {
		if (disposing)
		{
			if (_timer is not null)
				_timer.Timeout -= _TimeoutReached;
			if (_navigationAgent is not null)
				_navigationAgent.TargetReached -= _TargetReached;
		}
        base.Dispose(disposing);
    }
}
