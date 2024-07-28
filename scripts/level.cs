using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class level : Node3D
{
	private int _activeUnitIndex = 0;
	private List<unit> _units = new();
	private battle_ui? _battleUi;
	private Control? _mainMenu;

	public unit ActiveUnit => _units[_activeUnitIndex];

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_battleUi = GetNode<battle_ui>("BattleUI");
		_mainMenu = GetNode<Control>("main_menu");

		foreach (Node n in GetTree().GetNodesInGroup("units"))
		{
			if (n is unit)
				_units.Add(GetNode<unit>(n.GetPath())); 
		}
		_units.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
		
		foreach (var unit in _units)
		{
			unit.TurnEnded += _TurnEnded;
		}

		_StartTurn();
	}

	private void _StartTurn()
	{
		ActiveUnit.StartTurn();
		_battleUi!.Unit = ActiveUnit;
	}

	private void _TurnEnded()
	{
		_activeUnitIndex = (_activeUnitIndex + 1) % _units.Count;
		_StartTurn();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("Escape"))
		{
			_mainMenu!.Visible = true;
		}
    }
}
