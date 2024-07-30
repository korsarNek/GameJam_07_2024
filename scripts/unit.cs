using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

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

	[Export]
	public new Owner Owner = Owner.AI;

	public int ActionPoints { get; set; }
	public float Health;

	private IActionable? _selectedAction;
	private NavigationAgent3D? _navigationAgent;
	private AnimationTree? _animationTree;
	private Timer? _timer;
	private bool _timerStarted = false;
	private NavigationMode _navigationMode = NavigationMode.Idle;
	private List<DamageCalculationHandler> _damageReceivedHandlers = new();
	private PackedScene? _damageNumbers;
	private Dictionary<string, List<TaskCompletionSource>> _animationRequests = new();

	public List<ISelectable> Actions = new();
	public List<IEquipment> Equipped = new();
	public List<IAbility> Abilities = new();
	public bool IsDead => Health <= 0;

	[Signal]
	public delegate void ReachedTargetEventHandler();

	[Signal]
	public delegate void TurnEndedEventHandler();

	[Signal]
	public delegate void ActionAvailableEventHandler();

	public event Action<IActionable>? ActionSelected;
	public event Action<IActionable>? ActionFinished;

	public event Action<unit>? Died;

	public event Action<unit, Damage>? Kills;

	public delegate Damage DamageCalculationHandler(Damage damage);

	public IActionable? SelectedAction => _selectedAction;

	public event DamageCalculationHandler ReceivedDamage
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

	public void SelectAction(IActionable action)
	{
		_selectedAction = action;
		ActionSelected?.Invoke(action);
	}

	public Task PlayAnimation(string animationName)
	{
		_animationTree!.Set($"parameters/{animationName}/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);
		if (!_animationRequests.TryGetValue(animationName, out var list))
		{
			list = new List<TaskCompletionSource>();
			_animationRequests[animationName] = list;
		}

		var task = new TaskCompletionSource();
		list.Add(task);

		return task.Task;
	}

	public void ChangeAnimationState(string animationName)
	{
		_animationTree!.Set("parameters/Transition/transition_request", animationName);
	}

	public void FinishAction()
	{
		if (_selectedAction is not null)
			ActionFinished?.Invoke(_selectedAction);
		
		_selectedAction = null;
		if (ActionPoints <= 0)
			EmitSignal(SignalName.TurnEnded);
		else
			EmitSignal(SignalName.ActionAvailable);
	}

	public void ReceiveDamage(Damage damage)
	{
		foreach (var handler in _damageReceivedHandlers)
			damage = handler(damage);

		var display = (damage_number)_damageNumbers!.Instantiate();
		display.setText($"-{damage.Amount}");
		display.AnimationFinished += _DamageAnimationFinished;
		AddChild(display);

		Health -= damage.Amount;
		if (Health <= 0)
		{
			Health = 0;
			Died?.Invoke(this);
			// Remove collision, so units can stand on top.
			SetCollisionLayerValue(Constants.ObstaclesLayer, false);
			damage.Source.Kills?.Invoke(this, damage);
			ChangeAnimationState(Animation.Dying);
		}
		else if (damage.Amount > 20)
			PlayAnimation(Animation.BigHit);
		else
			PlayAnimation(Animation.Hit);
	}

	private void _DamageAnimationFinished(damage_number damage)
	{
		damage.AnimationFinished -= _DamageAnimationFinished;
		RemoveChild(damage);
	}

	public void NavigateTo(Vector3 position)
	{
		_navigationAgent!.TargetPosition = position;
		ChangeAnimationState(Animation.Running);
		_navigationMode = NavigationMode.Agent;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_damageNumbers = GD.Load<PackedScene>("res://components/damage_number.tscn");

		_navigationAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		_navigationAgent.NavigationFinished += _TargetReached;

		_animationTree = (AnimationTree)FindChild("AnimationTree");
		_animationTree.AnimationFinished += _AnimationFinished;

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

	private void _AnimationFinished(StringName name)
	{
		if (!_animationRequests.TryGetValue(name.ToString(), out var list))
			return;

		foreach (var task in list)
			task.SetResult();

		list.Clear();
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
		ChangeAnimationState(Animation.Idle);
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

	public void LookAt(unit unit)
	{
		LookAt(unit.GlobalPosition, UpDirection, true);
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

	public static class Animation
	{
		public static string Idle => "idle";
		public static string Running => "running";
		public static string Hit => "hit";
		public static string BigHit => "big hit";
		public static string Dying => "dying";
		public static string Shooting => "shooting";
		public static string Aiming => "aiming";
	}
}

public enum NavigationMode
{
	Idle,
	Agent,
	Manual,
}

public enum Owner
{
	Player = 0b1,
	AI = 0b10,
}
