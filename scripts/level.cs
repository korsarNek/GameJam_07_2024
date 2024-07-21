using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class level : Node3D
{
	private int _activeUnitIndex = 0;
	private List<unit> _units = new();
	private movement_grid? _movement_grid;

	public unit ActiveUnit => _units[_activeUnitIndex];

	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (Node n in GetTree().GetNodesInGroup("units"))
		{
			if (n is unit)
				_units.Add(GetNode<unit>(n.GetPath())); 
		}

		_movement_grid = GetNode<movement_grid>("MovementGrid");
		_movement_grid.ReachedTarget += () =>
		{
			_movement_grid.ShowMovementGrid(ActiveUnit);
		};

		_movement_grid.ShowMovementGrid(ActiveUnit);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
