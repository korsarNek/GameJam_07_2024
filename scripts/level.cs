using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class level : Node3D
{
	private int _activeUnitIndex = 0;
	private List<unit> _units = new();
	private battle_ui? _battleUi;
	private Node3D? _activeUnitDisplay;
	private movement_grid? _movementGrid;
	private melee_attack_selection? _meleeAttackSelection;
	private ranged_attack_selection? _rangedAttackSelection;

	public unit ActiveUnit => _units[_activeUnitIndex];
	public IReadOnlyList<unit> Units => _units;

	public movement_grid MovementGrid => _movementGrid ?? throw new ObjectNotInitializedException();
	public melee_attack_selection MeleeAttackSelection => _meleeAttackSelection ?? throw new ObjectNotInitializedException();
	public ranged_attack_selection RangedAttackSelection => _rangedAttackSelection ?? throw new ObjectNotInitializedException();
	public battle_ui BattleUI => _battleUi ?? throw new ObjectNotInitializedException();

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_movementGrid = GetNode<movement_grid>("MovementGrid");
		_meleeAttackSelection = GetNode<melee_attack_selection>("MeleeAttackSystem");
		_rangedAttackSelection = GetNode<ranged_attack_selection>("RangedAttackSystem");
		_battleUi = GetNode<battle_ui>("BattleUI");

		_activeUnitDisplay = GD.Load<PackedScene>("res://components/active_unit.tscn").Instantiate<Node3D>();
		

		foreach (Node n in GetTree().GetNodesInGroup("units"))
		{
			if (n is unit)
				_units.Add(GetNode<unit>(n.GetPath())); 
		}
		_units.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
		
		foreach (var unit in _units)
		{
			unit.Died += _Died;
			unit.TurnEnded += _TurnEnded;
		}

		this.Ready += _StartTurn;
	}

	private void _StartTurn()
	{
		ActiveUnit.StartTurn();
		_battleUi!.Unit = ActiveUnit;
		ActiveUnit.AddChild(_activeUnitDisplay);
	}

	private void _Died(unit unit)
	{
		int index = _units.IndexOf(unit);
		_units.RemoveAt(index);
		if (index < _activeUnitIndex)
			_activeUnitIndex--;
	}

	private void _TurnEnded()
	{
		ActiveUnit.RemoveChild(_activeUnitDisplay);
		_activeUnitIndex = (_activeUnitIndex + 1) % _units.Count;
		_StartTurn();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
