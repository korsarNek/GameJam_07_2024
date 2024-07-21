using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class level : Node3D
{
	private int _activeUnitIndex = 0;
	private List<unit> _units = new();
	private Vector3 GridSize3 => new Vector3(GridSize, GridSize, GridSize);
	private GridCellState[,] _grid = new GridCellState[0, 0];
	private PackedScene _movableTileScene;

	[Export]
	public float GridSize = 30;

	public unit ActiveUnit => _units[_activeUnitIndex];
	public Rid ActiveUnitRid => ((CollisionShape3D)ActiveUnit.FindChild("CollisionShape3D")).Shape.GetRid();
	public Vector3 Origin => new Vector3(ActiveUnit.Position.X - (float)_grid.GetLength(0) / 2 * GridSize, ActiveUnit.Position.Y, ActiveUnit.Position.Z - (float)_grid.GetLength(1) / 2 * GridSize);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//TODO: instantiate the scene for the selectable tiles.
		_movableTileScene = GD.Load<PackedScene>("res://components/movable_tile.tscn");

		foreach (Node n in GetTree().GetNodesInGroup("units"))
		{
			if (n is unit)
				_units.Add(GetNode<unit>(n.GetPath())); 
		}

		InitializeGrid(ActiveUnit.MovementRange);
		CheckCollisions();
		FloodFill(ToGridCoordinates(ActiveUnit.Position), ActiveUnit.MovementRange + 1);
		VisualizeGrid();
	}

	public Vector2I ToGridCoordinates(Vector3 vector)
	{
		var origin = Origin;
		return new Vector2I((int)((vector.X - origin.X) / GridSize), (int)((vector.Z - origin.Z) / GridSize));
	}

	public Vector3 ToRealCoordinates(Vector2I vector)
	{
		var origin = Origin;
		return new Vector3(origin.X + vector.X * GridSize + GridSize / 2, origin.Y, origin.Z + vector.Y * GridSize + GridSize / 2);
	}

	public void VisualizeGrid()
	{
		for (int x = 0; x < _grid.GetLength(0); x++)
			for (int y = 0; y < _grid.GetLength(1); y++)
			{
				if (_grid[x, y] == GridCellState.Moveable)
				{
					var tile = _movableTileScene.Instantiate<Node3D>();
					tile.Position = ToRealCoordinates(new Vector2I(x, y));
					AddChild(tile);
				}
			}
	}

	public void InitializeGrid(int movementRange)
	{
		_grid = new GridCellState[movementRange * 2 + 1, movementRange * 2 + 1];
		for (int x = 0; x < _grid.GetLength(0); x++)
			for (int z = 0; z < _grid.GetLength(1); z++)
			{
				_grid[x, z] = GridCellState.NotInitialized;
			}
	}

	public void CheckCollisions()
	{
		var space = GetWorld3D().DirectSpaceState;
		for (int x = 0; x < _grid.GetLength(0); x++)
			for (int y = 0; y < _grid.GetLength(1); y++)
			{
				var box = new BoxShape3D()
				{
					Size = GridSize3,
				};
				var intersection = space.IntersectShape(new PhysicsShapeQueryParameters3D()
				{
					CollideWithBodies = true,
					Exclude = new() { ActiveUnitRid }, //TODO: exluding doesn't seem to work
					Shape = box,
					Transform = Transform3D.Identity.Translated(ToRealCoordinates(new Vector2I(x, y))),
				}, maxResults: 1);

				if (intersection.Any())
					_grid[x, y] = GridCellState.NotReachable;

				box.Dispose();
			}
	}

	public bool IsInGrid(Vector2I coordinate)
	{
		return coordinate.X >= 0 && coordinate.Y >= 0 && coordinate.X < _grid.GetLength(0) && coordinate.Y < _grid.GetLength(1);
	}

	public void FloodFill(Vector2I coordinate, int range)
	{
		var queue = new Queue<(Vector2I, int)>();
		queue.Enqueue((coordinate, range));

		while (queue.Count != 0)
		{
			var (coord, remainingRange) = queue.Dequeue();
			Fill(coord, remainingRange);
		}

		void Fill(Vector2I coordinate, int range)
		{
			_grid[coordinate.X, coordinate.Y] = GridCellState.Moveable;

			if (range <= 0)
				return;

			range--;

			var left = new Vector2I(coordinate.X - 1, coordinate.Y);
			var right = new Vector2I(coordinate.X + 1, coordinate.Y);
			var up = new Vector2I(coordinate.X, coordinate.Y - 1);
			var down = new Vector2I(coordinate.X, coordinate.Y + 1);

			if (isValid(left))
				queue.Enqueue((left, range));
			if (isValid(right))
				queue.Enqueue((right, range));
			if (isValid(up))
				queue.Enqueue((up, range));
			if (isValid(down))
				queue.Enqueue((down, range));

		}

		bool isValid(Vector2I coordinate)
		{
			return IsInGrid(coordinate) && _grid[coordinate.X, coordinate.Y] == GridCellState.NotInitialized;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}

public enum GridCellState
{
	NotInitialized = 0,
	Moveable,
	NotReachable,
}
