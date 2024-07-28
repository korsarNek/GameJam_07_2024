using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public partial class movement_grid : Node3D
{
	private Vector3 GridSize3 => new Vector3(GridSize, GridSize, GridSize);
	private GridCellState[,] _grid = new GridCellState[0, 0];
	private PackedScene? _movableTileScene;
	private PackedScene? _moveTargetScene;
	private Node3D? _moveTarget;
	private unit? _focusedUnit;
	private List<Node3D> _tiles = new();
	private NavigationMesh? _navigationMesh;
	private MeshInstance3D? _debugNode;
	private StandardMaterial3D? _debugMaterial;

	public Vector3 Reference { get; set; }

	[Export]
	public float GridSize = 1;

	[Signal]
	public delegate void ReachedTargetEventHandler(unit unit);

	[Signal]
	public delegate void MoveInitiatedEventHandler();

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

	public void ShowMovementGrid(unit unit, int range)
	{
		ShowMovementGrid(unit, range, phaseThroughObstacles: false);
	}

	public void ShowMovementGridPhaseThrough(unit unit, int range)
	{
		ShowMovementGrid(unit, range, phaseThroughObstacles: true);
	}

	private void ShowMovementGrid(unit unit, int range, bool phaseThroughObstacles)
	{
		_focusedUnit = unit;
		Reference = ToRealCoordinates(ToGridCoordinates(unit.Position));

		InitializeGrid(range);
		CheckCollisions();

		if (phaseThroughObstacles)
			RangeFill(ToGridCoordinates(unit.Position), range);
		else
			FloodFill(ToGridCoordinates(unit.Position), range);

		//UpdateNavigationMesh(phaseThroughObstacles);
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

	private void VisualizeGrid()
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

	private void InitializeGrid(int movementRange)
	{
		_grid = new GridCellState[movementRange * 2 + 1, movementRange * 2 + 1];
		for (int x = 0; x < _grid.GetLength(0); x++)
			for (int z = 0; z < _grid.GetLength(1); z++)
			{
				_grid[x, z] = GridCellState.NotInitialized;
			}
	}

	private void CheckCollisions()
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
					CollisionMask = Constants.AsMask(Constants.ObstaclesLayer),
					Shape = box,
					Transform = Transform3D.Identity.Translated(ToRealCoordinates(new Vector2I(x, y))),
				}, maxResults: 1);

				if (intersection.Count != 0)
					_grid[x, y] = GridCellState.NotReachable;

				box.Dispose();
			}
	}

	private bool IsInGrid(Vector2I coordinate)
	{
		return coordinate.X >= 0 && coordinate.Y >= 0 && coordinate.X < _grid.GetLength(0) && coordinate.Y < _grid.GetLength(1);
	}

	private (Vector2I up, Vector2I right, Vector2I down, Vector2I left) SurroundingCoordinates(Vector2I coordinate)
	{
		return (
			coordinate with { Y = coordinate.Y - 1 },
			coordinate with { X = coordinate.X + 1 },
			coordinate with { Y = coordinate.Y + 1 },
			coordinate with { X = coordinate.X - 1 }
		);
	}

	/// <summary>
	/// Marks reachable cells, as if the unit is unaffected by obstacles, but can't stop inside an obstacle.
	/// </summary>
	/// <param name="coordinate"></param>
	/// <param name="range"></param>
	private void RangeFill(Vector2I coordinate, int range)
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
			if (_grid[coordinate.X, coordinate.Y] == GridCellState.NotInitialized)
				_grid[coordinate.X, coordinate.Y] = GridCellState.Moveable;

			range--;
			if (range <= 0)
				return;

			var (up, right, down, left) = SurroundingCoordinates(coordinate);
			if (IsInGrid(left))
				queue.Enqueue((left, range));
			if (IsInGrid(right))
				queue.Enqueue((right, range));
			if (IsInGrid(up))
				queue.Enqueue((up, range));
			if (IsInGrid(down))
				queue.Enqueue((down, range));
		}
	}

	/// <summary>
	/// Marks reachable cells by flood filling, causing units to walk around obstacles.
	/// </summary>
	/// <param name="coordinate"></param>
	/// <param name="range"></param>
	private void FloodFill(Vector2I coordinate, int range)
	{
		var queue = new Queue<(Vector2I, int)>();
		queue.Enqueue((coordinate, range));

		while (queue.Count != 0)
		{
			var (coord, remainingRange) = queue.Dequeue();
			Fill(coord, remainingRange);
		}

		// For now, only make the start grid unavailable, if we need something else, for example for larget units, we can try a solution again.
		_grid[coordinate.X, coordinate.Y] = GridCellState.Self;

		void Fill(Vector2I coordinate, int range)
		{
			_grid[coordinate.X, coordinate.Y] = GridCellState.Moveable;

			range--;
			if (range <= 0)
				return;

			var (up, right, down, left) = SurroundingCoordinates(coordinate);
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
		var unit = _focusedUnit;
		_focusedUnit = null;
		EmitSignal(SignalName.ReachedTarget, unit);
	}

	public override void _Input(InputEvent @event)
	{
		if (_focusedUnit is null)
			return;

		// Mouse in viewport coordinates.
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.IsActionReleased("move_to"))
		{
			var coordinate = MouseHoverCell();
			if (coordinate is Vector2I target)
			{
				var targetCoordinates = ToRealCoordinates(target);
				_focusedUnit!.NavigateTo(targetCoordinates);
				EmitSignal(SignalName.MoveInitiated);
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
			CollisionMask = Constants.AsMask(Constants.SelectableLayer),
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

	private NavigationMesh EnsureNavigationMesh()
	{
		if (_navigationMesh is not null)
			return _navigationMesh;

		_navigationMesh = new NavigationMesh();
		var region = NavigationServer3D.RegionCreate();
		
		NavigationServer3D.RegionSetEnabled(region, true);
		NavigationServer3D.RegionSetMap(region, GetWorld3D().NavigationMap);
		NavigationServer3D.RegionSetNavigationMesh(region, _navigationMesh);

		return _navigationMesh;
	}

	private void UpdateNavigationMesh(bool phaseThroughObstacles)
	{
		var mesh = EnsureNavigationMesh();
		mesh.Clear();

		float offset = 1;
		var origin = Origin;
		float height = origin.Y + offset;
		UniqueList<Vector3> vertices = new();
		for (int x = 0; x < _grid.GetLength(0); x++)
			for (int y = 0; y < _grid.GetLength(1); y++)
			{
				if (_grid[x, y] == GridCellState.Moveable || _grid[x, y] == GridCellState.Self || (phaseThroughObstacles && _grid[x, y] == GridCellState.NotReachable))
				{
					float xCorner = origin.X + x * GridSize;
					float yCorner = origin.Y + y * GridSize;
					var topLeft = new Vector3(xCorner, height, yCorner);
					var topRight = new Vector3(xCorner + GridSize, height, yCorner);
					var bottomRight = new Vector3(xCorner + GridSize, height, yCorner + GridSize);
					var bottomLeft = new Vector3(xCorner, height, yCorner + GridSize);

					mesh.AddPolygon(vertices.AddRange(topLeft, topRight, bottomRight));
					mesh.AddPolygon(vertices.AddRange(topLeft, bottomRight, bottomLeft));
				}
			}

		mesh.Vertices = vertices.ToArray();

		if (NavigationServer3D.GetDebugEnabled())
		{
			if (_debugMaterial is null)
			{
                _debugMaterial = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                    AlbedoColor = new Color(0, 1, 1, 0.5f)
                };
            }

			var debugMesh = new ImmediateMesh();
			foreach (var polygon in mesh.Polygons)
			{
				debugMesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip, _debugMaterial);
				foreach (var index in polygon.AsInt32Array())
					debugMesh.SurfaceAddVertex(vertices[index]);
				debugMesh.SurfaceEnd();
			}

			if (_debugNode is null)
			{
                _debugNode = new MeshInstance3D();
				GetTree().CurrentScene.AddChild(_debugNode);
			}

			_debugNode.Mesh = debugMesh;
		}
	}

	private (bool up, bool right, bool down, bool left) AreEdgesConnected(Vector2I coordinate, bool phaseThroughObstacles)
	{
		var (up, right, down, left) = SurroundingCoordinates(coordinate);
		return (
			IsInGrid(up) && IsMoveable(up.X, up.Y, phaseThroughObstacles),
			IsInGrid(right) && IsMoveable(right.X, right.Y, phaseThroughObstacles),
			IsInGrid(down) && IsMoveable(down.X, down.Y, phaseThroughObstacles),
			IsInGrid(left) && IsMoveable(left.X, left.Y, phaseThroughObstacles)
		);
	}

	private IEnumerable<Vector2I[]> CutIntoStripes(bool phaseThroughObstacles)
	{
		List<Vector2I> coordinates = new();
		bool moveable = false;
		for (int x = 0; x < _grid.GetLength(0); x++)
		{
			for (int y = 0; y < _grid.GetLength(1); y++)
			{
				if (IsMoveable(x, y, phaseThroughObstacles))
				{
					coordinates.Add(new Vector2I(x, y));
					moveable = true;
				}
				else if (moveable)
				{
					yield return coordinates.ToArray();
					coordinates.Clear();
					moveable = false;
				}
			}
			if (moveable)
			{
				yield return coordinates.ToArray();
				coordinates.Clear();
				moveable = false;
			}
		}

		if (moveable)
			yield return coordinates.ToArray();
	}

	private bool IsMoveable(int x, int y, bool phaseThroughObstacles) => _grid[x, y] == GridCellState.Moveable || (phaseThroughObstacles && _grid[x, y] == GridCellState.NotReachable);
}

public enum GridCellState
{
	NotInitialized = 0,
	Moveable,
	NotReachable,
	Self,
}

public class UniqueList<T> : IReadOnlyList<T>
{
	private List<T> _items = new();

    public T this[int index] => _items[index];

	public int Count => _items.Count;

	public int Add(T element)
	{
		int index = _items.IndexOf(element);
		if (index == -1)
		{
			_items.Add(element);
			return _items.Count - 1;
		}
		return index;
	}

	public int[] AddRange(params T[] elements)
	{
		var indices = new int[elements.Length];
		for (int i = 0; i < elements.Length; i++)
			indices[i] = Add(elements[i]);

		return indices;
	}

    public IEnumerator<T> GetEnumerator()
    {
		return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
		return GetEnumerator();
    }

	public T[] ToArray() => _items.ToArray();

}
