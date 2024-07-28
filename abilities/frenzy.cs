using System;
using Godot;

public partial class frenzy : Node3D, IAbility
{
    private unit? _unit;
    private GpuParticles3D? _effect;

    public override void _Ready()
    {
        base._Ready();

        _effect = GetNode<GpuParticles3D>("Effect");
    }

    //passive ability, triggered on kill
    public void Learned(unit unit)
    {
        if (_unit is not null)
            throw new SkillAlreadyAssignedException();

        unit.Kills += Killed;
        _unit = unit;
    }

    public void Unlearned()
    {
        if (_unit is not null)
            _unit.Kills -= Killed;
        
        _unit = null;
    }

    private void Killed(unit target, Damage damage)
    {
        if (damage.Action is IMeleeAttack && _unit!.ActionPoints < _unit.MaximumActionPoints)
        {
            _unit!.ActionPoints++;
            _effect!.Restart();
            _effect!.Emitting = true;
            //TODO: make a text popup
        }
    }
}