using Godot;
using System;

public partial class camera : CharacterBody3D
{
	[Export]
	public float MovementSpeed = 10;

	[Export]
	public float RotationSpeed = 2;

	[Export]
	public float ZoomSpeed = 3;

	[Export]
	public float ZoomMinimum = 1;

	[Export]
	public float ZoomMaximum = 5;

	private Camera3D? _camera;
	private float _currentZoom;
	private Vector3 _rotationalVelocity;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    public override void _PhysicsProcess(double delta)
    {
		float rotation = Input.GetAxis("camera_rotate_left", "camera_rotate_right");
		float zoom = Input.GetAxis("camera_zoom_in", "camera_zoom_out");
		Vector2 inputDirection = Input.GetVector("camera_left", "camera_right", "camera_forward", "camera_backward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();

		Velocity = direction * MovementSpeed;
		_rotationalVelocity = Vector3.Down * rotation * RotationSpeed;

		MoveAndSlide();
		Rotation += _rotationalVelocity * (float)delta;

		_camera!.LookAt(Position);
		_camera.Translate(new Vector3(0, 0, zoom * ZoomSpeed * (float)delta));

		base._PhysicsProcess(delta);
    }
}
