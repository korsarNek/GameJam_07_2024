using Godot;
using System;
using System.Collections;
using System.Linq;

public partial class battle_ui : MarginContainer
{
	private Container? _abilityContainer;
	private Label? _actionPoints;
	private unit? _unit;
	private BaseButton? _cancel;

	public unit? Unit
	{
		get => _unit;
		set
		{
			if (_unit != value)
			{
				if (_unit is not null)
				{
					_unit.ActionSelected -= _ActionSelected;
					foreach (var child in _abilityContainer!.GetChildren().ToArray())
						_abilityContainer.RemoveChild(child);
				}

				_unit = value;

				if (_unit is not null)
				{
					foreach (var action in _unit!.Actions)
						_abilityContainer!.AddChild(action.Icon);

					UpdateActionPoints();

					_unit.ActionSelected += _ActionSelected;
					_unit.ActionAvailable += _ActionAvailable;
				}
			}
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_abilityContainer = GetNode<Container>("ActionContainer/AbilityPanel/AbilityContainer");
		_actionPoints = GetNode<Label>("ActionContainer/ActionPoints");
		_cancel = GetNode<BaseButton>("AbortActionContainer/AbortAction");

		_cancel.Visible = false;
	}

	public void Suspend()
	{
		Visible = false;
	}

	private void _ActionSelected(IActionable action)
	{
		if (action.CanBeCanceled)
			_cancel!.Visible = true;
	}

	private void _ActionAvailable()
	{
		_cancel!.Visible = false;
		Visible = true;
		UpdateActionPoints();
	}

	public void CancelPressed()
	{
		_unit!.FinishAction();
	}

	private void UpdateActionPoints()
	{
		_actionPoints!.Text = $"Action points: {_unit!.ActionPoints}/{_unit.MaximumActionPoints}";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
