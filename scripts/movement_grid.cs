using Godot;
using System;
using System.Collections.Generic;

public partial class movement_grid : Node3D
{
	private Vector3 GridSize3 => new Vector3(GridSize, GridSize, GridSize);
	private GridCellState[,] _grid = new GridCellState[0, 0];
	private PackedScene? _movableTileScene;
	private PackedScene? _moveTargetScene;
	private Node3D? _moveTarget;
	private unit? _focusedUnit;
	private List<Node3D> _tiles = new();

	public Vector3 Reference { get; set; }

	[Export]
	public float GridSize = 1;

	[Signal]
	public delegate void ReachedTargetEventHandler();

	public Vector3 Origin {
		get
		{
			return new Vector3(Reference.X - (float)_grid.GetLength(0) / 2 * GridSize, Reference.Y, Reference.Z - (float)_grid.GetLength(1) / 2 * GridSize);
		}
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_movableTileScene = GD.Load<PackedScene>("res://components/movable_tile.tscn");
		_moveTargetScene = GD.Load<PackedScene>("res://components/move_target.tscn");
	}

	public void ShowMovementGrid(unit unit)
	{
		_focusedUnit = unit;
		Reference = ToRealCoordinates(ToGridCoordinates(unit.Position));

		InitializeGrid(unit.MovementRange);
		CheckCollisions();
		FloodFill(ToGridCoordinates(unit.Position), unit.MovementRange);
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

	public void RemoveGrid()
	{
		foreach (var tile in _tiles)
		{
			RemoveChild(tile);
			tile.Dispose();
		}
		_tiles.Clear();
	}

	public void VisualizeGrid()
	{
		for (int x = 0; x < _grid.GetLength(0); x++)
			for (int y = 0; y < _grid.GetLength(1); y++)
			{
				if (_grid[x, y] == GridCellState.Moveable)
				{
					var tile = _movableTileScene!.Instantiate<Node3D>();
					tile.Position = ToRealCoordinates(new Vector2I(x, y));
					_tiles.Add(tile);
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
					CollisionMask = 0b10,
					Shape = box,
					Transform = Transform3D.Identity.Translated(ToRealCoordinates(new Vector2I(x, y))),
				}, maxResults: 1);

				if (intersection.Count != 0)
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

	private void UnitReachedTarget()
	{
		_focusedUnit!.ReachedTarget -= UnitReachedTarget;
		_focusedUnit = null;
		EmitSignal(SignalName.ReachedTarget);
	}

	public override void _Input(InputEvent @event)
	{
		// Mouse in viewport coordinates.
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.IsActionReleased("move_to"))
		{
			var coordinate = MouseHoverCell();
			if (coordinate is Vector2I target)
			{
				_focusedUnit!.NavigateTo(ToRealCoordinates(target));
				//TODO: add a timeout so that the unit just gets teleported to the target if it gets stuck.
				RemoveGrid();
				_focusedUnit!.ReachedTarget += UnitReachedTarget;
			}
		}
		else if (@event is InputEventMouseMotion)
		{
			var coordinate = MouseHoverCell();
			if (coordinate is Vector2I target)
			{
				if (_moveTarget is null)
				{
					_moveTarget = _moveTargetScene!.Instantiate<Node3D>();
					AddChild(_moveTarget);
				}

				_moveTarget.Visible = true;
				_moveTarget.Position = ToRealCoordinates(target);
			}
			else if (_moveTarget is not null)
				_moveTarget.Visible = false;
		}
	}

	private Vector2I? MouseHoverCell()
	{
		var (from, to) = GetMouseScreenRay();
		var result = GetWorld3D().DirectSpaceState.IntersectRay(new PhysicsRayQueryParameters3D()
		{
			CollideWithBodies = true,
			CollisionMask = 0b100,
			From = from,
			To = to,
		});
		if (result.Count != 0)
			return ToGridCoordinates((Vector3)result["position"]);
		
		return null;
	}

	private (Vector3 from, Vector3 to) GetMouseScreenRay()
	{
		var viewport = GetViewport();
		var mousePosition = viewport.GetMousePosition();
		var camera = viewport.GetCamera3D();

		var position = camera.ProjectRayOrigin(mousePosition);
		var direction = camera.ProjectRayNormal(mousePosition);

		var end = position + direction * camera.Far;

		return (position, end);
	}
}

public enum GridCellState
{
	NotInitialized = 0,
	Moveable,
	NotReachable,
}
