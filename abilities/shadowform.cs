using System;
using System.Linq;
using Godot;

public partial class shadowform : Node3D, IAbility, ISelectable
{
    [Export]
    public int ActionPointCost;

    public bool CanBeCanceled => false;

    private Control? _icon;
    private unit? _unit;
    private GpuParticles3D? _effect;
    private movement_grid? _movement_grid;

    public Control Icon => _icon ?? throw new ObjectNotInitializedException();

    public override void _Ready()
    {
        base._Ready();

        _effect = GetNode<GpuParticles3D>("Effect");

        _icon = GetNode<Control>("Icon");
        RemoveChild(_icon);

        GetTree().CurrentScene.Ready += () => {
            _movement_grid = ((level)GetTree().CurrentScene).MovementGrid;
            _movement_grid.ReachedTarget += _ReachedTarget;
        };
    }

    private void _ReachedTarget(unit unit)
	{
        if (unit != _unit || unit.SelectedAction != this)
            return;

        _unit!.ActionPoints--;
        _effect!.Emitting = false;
        _unit.FinishAction();
	}

    public void Learned(unit unit)
    {
        if (_unit is not null)
            throw new SkillAlreadyAssignedException();
        
        unit.Actions.Add(this);
        _unit = unit;
    }

    public void Unlearned()
    {
        if (_unit is not null)
        {
            _unit.Actions.Remove(this);
        }
    }

    public void OnIconPressed(InputEvent input)
    {
        if (input.IsActionReleased("ui_use_ability"))
        {
            _effect!.Emitting = true;
            _unit!.SelectAction(this);

            var move = (move)_unit.Abilities.First(a => a is move);

            _movement_grid!.ShowMovementGridPhaseThrough(_unit, move!.Range);
        }
    }
}