using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

public partial class unit : CharacterBody3D
{
	[Export]
	public int MaximumActionPoints = 2;

	[Export]
	public float MaximumHealth = 100;

	[Export]
	public int Initiative = 5;

	[Export]
	public int MovementSpeed = 5;

	[Export]
	public float Weight = 80;

	public int ActionPoints { get; set; }
	public float Health;

	private IAction? _selectedAction;
	private NavigationAgent3D? _navigationAgent;
	private AnimationPlayer? _animationPlayer;
	private Timer? _timer;
	private bool _timerStarted = false;
	private NavigationMode _navigationMode = NavigationMode.Idle;
	private List<DamageCalculationHandler> _damageReceivedHandlers = new();

	public List<IAction> Actions = new();
	public List<IEquipment> Equipped = new();
	public List<IAbility> Abilities = new();

	[Signal]
	public delegate void ReachedTargetEventHandler();

	[Signal]
	public delegate void TurnEndedEventHandler();

	[Signal]
	public delegate void ActionAvailableEventHandler();

	public event Action<IAction>? ActionSelected;

	public event Action? Died;

	public event Action<unit, Damage>? Kills;

	public delegate Damage DamageCalculationHandler(Damage damage);

	public IAction? SelectedAction => _selectedAction;

	public event DamageCalculationHandler ReceiveDamage
	{
		add => _damageReceivedHandlers.Add(value);
		remove => _damageReceivedHandlers.Remove(value);
	}

	public unit()
	{
		Health = MaximumHealth;
	}

	public void StartTurn()
	{
		ActionPoints = MaximumActionPoints;
		EmitSignal(SignalName.ActionAvailable);
	}

	public void SelectAction(IAction action)
	{
		_selectedAction = action;
		ActionSelected?.Invoke(action);
	}

	public void FinishAction()
	{
		_selectedAction = null;
		if (ActionPoints <= 0)
			EmitSignal(SignalName.TurnEnded);
		else
			EmitSignal(SignalName.ActionAvailable);
	}

	public void ProcessDamage(Damage damage)
	{
		foreach (var handler in _damageReceivedHandlers)
			damage = handler(damage);

		Health -= damage.Amount;
		if (Health <= 0)
		{
			Health = 0;
			Died?.Invoke();
			damage.Source.Kills?.Invoke(this, damage);
		}
	}

	public void NavigateTo(Vector3 position)
	{
		_navigationAgent!.TargetPosition = position;
		_animationPlayer!.Play("running");
		_navigationMode = NavigationMode.Agent;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		ActionPoints = MaximumActionPoints;
		_animationPlayer = (AnimationPlayer)FindChild("AnimationPlayer");
		var animation = _animationPlayer.GetAnimation("running");
		animation.LoopMode = Animation.LoopModeEnum.Linear;
		_navigationAgent.NavigationFinished += _TargetReached;

		foreach (var node in GetChildren())
		{
			if (node is IAbility ability)
			{
				ability.Learned(this);
				Abilities.Add(ability);
			}
			else if (node is IEquipment equipment)
			{
				equipment.Equipped(this);
				Equipped.Add(equipment);
			}
		}
	}

	public void Attach(string boneName, Node3D node)
	{
		var attachment = GetNode<BoneAttachment3D>(boneName);
		attachment.AddChild(node);
	}

	public void Unattach(string boneName, Node3D node)
	{
		var attachment = GetNode<BoneAttachment3D>(boneName);
		attachment.RemoveChild(node);
	}

	private void _TargetReached()
	{
		_animationPlayer!.Stop();
		// When it reached the target according to the navigationAgent, we might still be a bit off. So we switch mode to manually move there.
		_navigationMode = NavigationMode.Manual;
	}

	private void _TimeoutReached()
	{
		//Teleport to target location if we are stuck.
		Position = _navigationAgent!.TargetPosition;
		_TargetReached();
	}

    public override void _PhysicsProcess(double delta)
    {
		//TODO: I'll have to build my own navigation system, the default one is shit.
		if (_navigationMode == NavigationMode.Agent)
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

			_Move(_navigationAgent.GetNextPathPosition(), MovementSpeed, (float)delta);
		}
		else if (_navigationMode == NavigationMode.Manual)
		{
			var target = _navigationAgent!.GetFinalPosition();
			var distance = GlobalPosition.DistanceTo(target);
			if (distance < MovementSpeed * (float)delta)
			{
				_navigationMode = NavigationMode.Idle;
				EmitSignal(SignalName.ReachedTarget);
				if (_timerStarted)
				{
					_timer!.Stop();
					_timerStarted = false;
				}
			}
			else
			{
				_Move(target, distance * MovementSpeed * 3, (float)delta);
			}
		}
    }

	private void _Move(Vector3 next, float speed, float delta)
	{
		var direction = GlobalPosition.DirectionTo(next);
		if (direction.Dot(UpDirection) < 0.9)
			LookAt(next, UpDirection, true);
		
		// Kick objects around if we are colliding.
		var collision = MoveAndCollide(direction * speed * delta);
		if (collision is not null && collision.GetCollider() is RigidBody3D body)
		{
			body.ApplyForce(collision.GetNormal().Rotated(UpDirection, MathF.PI) * speed * speed * Weight * delta, body.ToLocal(collision.GetPosition()));
		}
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		
	}
}

public enum NavigationMode
{
	Idle,
	Agent,
	Manual,
}
