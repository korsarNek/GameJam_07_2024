using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class melee_attack_selection : Node3D
{
	private PackedScene? _attack_box;
	private level? _level;
	private List<attack_box> _attackBoxes = new();

	public event Action<unit>? TargetSelected;
	public event Action<unit>? TargetHovered;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_attack_box = GD.Load<PackedScene>("res://components/attack_box.tscn");
		_level = (level)GetTree().CurrentScene;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ShowAttackBoxes(unit attacker, Owner owner, float range)
	{
		var targets =_level!.Units.Where(u => !u.IsDead && u.Owner == owner && u.GlobalPosition.DistanceTo(attacker.GlobalPosition) <= range);
		foreach (var target in targets)
		{
			var box = (attack_box)_attack_box!.Instantiate();
			box.Clicked += _AttackBoxClicked;
			box.Hovered += _AttackBoxHovered;
			box.SetMeta("targetUnit", target);
			_attackBoxes.Add(box);
			target.AddChild(box);
		}
	}

	private void _AttackBoxClicked(attack_box target)
	{
		var unit = (unit)target.GetMeta("targetUnit");
		TargetSelected?.Invoke(unit);
		HideAttackBoxes();
	}

	private void _AttackBoxHovered(attack_box target)
	{
		var unit = (unit)target.GetMeta("targetUnit");
		TargetHovered?.Invoke(unit);
	}

	public void HideAttackBoxes()
	{
		foreach (var box in _attackBoxes)
		{
			box.Clicked -= _AttackBoxClicked;
			box.Hovered -= _AttackBoxHovered;
			box.GetParent().RemoveChild(box);
		}
		_attackBoxes.Clear();
	}
}
