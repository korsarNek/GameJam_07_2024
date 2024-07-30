using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class mount_ui : Control
{
	[Export]
	public SizeMode SizeMode = SizeMode.ScaleDown;

	/// <summary>
	/// The minimum size of the ui element. Only really relevant if <see cref="SizeMode.ScaleDown"/> is used.
	/// </summary>
	[Export]
	public Vector2 MinimumSize;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var camera = GetViewport().GetCamera3D();
		var parent = GetParent<CollisionObject3D>();
		var position = camera.UnprojectPosition(parent.GlobalPosition);
		if (camera.IsPositionBehind(parent.GlobalPosition))
		{
			Visible = false;
			return;
		}

		var screenRect = new Rect2(position, Vector2.Zero);
		foreach (var shape in GetShapes(parent))
		{
			var aabb = shape.GetDebugMesh().GetAabb();
			for (int i = 0; i < 8; i++)
				screenRect = screenRect.Expand(camera.UnprojectPosition(parent.ToGlobal(aabb.GetEndpoint(i))));
		}

		//When minsize gets used, we have to adjust the position to keep it centered.
		var newSize = new Vector2(Mathf.Max(MinimumSize.X, screenRect.Size.X), Mathf.Max(MinimumSize.Y, screenRect.Size.Y));
		screenRect.Position = new Vector2(screenRect.Position.X - (newSize.X - screenRect.Size.X) / 2, screenRect.Position.Y - (newSize.Y - screenRect.Size.Y) / 2);
		screenRect.Size = newSize;

		if (screenRect.Area > 0)
		{
			Visible = true;
			Position = screenRect.Position;
			if (SizeMode == SizeMode.AdjustSize)
				Size = screenRect.Size;
			else if (SizeMode == SizeMode.ScaleDown)
			{
				Size = screenRect.Size;
				var scale = Scale;
				if (Size.X > screenRect.Size.X)
					scale.X = screenRect.Size.X / Size.X;
				if (Size.Y > screenRect.Size.Y)
					scale.Y = screenRect.Size.Y / Size.Y;

				Scale = scale;
			}
		}
		else
			Visible = false;
	}

	private IEnumerable<Shape3D> GetShapes(CollisionObject3D collisionObject)
	{
		foreach (var owner in collisionObject.GetShapeOwners())
		{
			if (!collisionObject.IsShapeOwnerDisabled((uint)owner))
			{
				var obj = collisionObject.ShapeOwnerGetOwner((uint)owner) as CollisionShape3D;
				yield return obj!.Shape;
			}
		}
	}
}

public enum SizeMode
{
	None,
	AdjustSize,
	ScaleDown,
	//TODO: Keep aspect ratio
}
