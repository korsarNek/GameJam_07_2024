using Godot;
using System;
using System.Linq;

public partial class sword : Node3D, IMeleeWeapon, IMeleeAttack
{
	[Export]
	public int Range { get; set; } = 1;

	[Export]
	public float Damage { get; set; } = 20f;

    public Control Icon => _icon ?? throw new ObjectNotInitializedException();

	private unit? _unit;
    private Node3D? _model;
	private Control? _icon;
	private movement_grid? _movement_grid;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_model = GetNode<Node3D>("Model");
		RemoveChild(_model);

		_icon = GetNode<Control>("Icon");
		RemoveChild(_icon);

		_movement_grid = GetTree().CurrentScene.GetNode<movement_grid>("MovementGrid");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void Equipped(unit unit)
    {
		if (_unit is not null)
			throw new SkillAlreadyAssignedException();
		
		_model!.Visible = true;
		_unit = unit;
		unit.Attach("RightHand", _model!);
		unit.Actions.Add(this);
    }

    public void Unequipped(unit unit)
    {
		_model!.Visible = false;
		unit.Unattach("RightHand", _model!);
		unit.Actions.Remove(this);
		_unit = null;
    }

	public void OnIconPressed(InputEvent input)
    {
        if (input.IsActionReleased("ui_use_ability"))
        {
			var move = (move)_unit!.Abilities.First(a => a is move);
			_movement_grid!.ShowMovementGrid(_unit!, move.Range);
			// Show movement grid, but enemy units are the only valid target.
			GD.Print("Use weapon");
        }
    }
}
