using Godot;
using System;
using System.Collections;
using System.Linq;

public partial class battle_ui : MarginContainer
{
	private Container? _abilityContainer;
	private unit? _unit;

	public unit? Unit
	{
		get => _unit;
		set
		{
			if (_unit != value)
			{
				_unit = value;
				_UnitChanged();
			}
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_abilityContainer = GetNode<Container>("AbilityPanel/AbilityContainer");
	}

	private void _UnitChanged()
	{
		foreach (var child in _abilityContainer!.GetChildren().ToArray())
			_abilityContainer.RemoveChild(child);

		if (_unit is null)
			return;

		foreach (var action in _unit.Actions)
			_abilityContainer.AddChild(action.Icon);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
