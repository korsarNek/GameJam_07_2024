using System;
using Godot;

public partial class move : Node3D, IAbility
{
    [Export]
    public int ActionPointCost { get; set; } = 1;

    [Export]
    public int Range { get; set; } = 5;

    private movement_grid? _movement_grid;
    private unit? _unit;

    public override void _Ready()
    {
        base._Ready();

        _movement_grid = GetTree().CurrentScene.GetNode<movement_grid>("MovementGrid");
        _movement_grid.ReachedTarget += _ReachedTarget;
    }

    private void _ReachedTarget(unit unit)
	{
        if (unit != _unit || unit.SelectedAction != null)
            return;

        _unit!.ActionPoints--;
        _unit.FinishAction();
	}

    public void Learned(unit unit)
    {
        if (_unit is not null)
            throw new SkillAlreadyAssignedException();

        _unit = unit;
        _unit.ActionAvailable += _ActionAvailable;
        _unit.ActionSelected += _ActionSelected;
    }

    private void _ActionAvailable()
    {
        _movement_grid!.ShowMovementGrid(_unit!, Range);
    }

    private void _ActionSelected(IAction action)
    {
        //If an explicit action has been selected, hide the default movement grid.
        _movement_grid!.RemoveGrid();
    }

    public void Unlearned()
    {
        if (_unit is not null)
        {
            _unit.ActionAvailable -= _ActionAvailable;
            _unit.ActionSelected -= _ActionSelected;
        }
        _unit = null;
    }
}