using Godot;
using System;

public partial class colt : Node3D, IRangedWeapon, IRangedAttack
{
	private unit? _unit;
    private Node3D? _model;
	private Control? _icon;
	private ranged_attack_selection? _attack_selection;
	private battle_ui? _battle_ui;

	public bool CanBeCanceled => true;
    public Control Icon => _icon ?? throw new ObjectNotInitializedException();

    public float Range => 10f;
	public float Damage => 20f;

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
	{
		_model = GetNode<Node3D>("Model");
		RemoveChild(_model);

		_icon = GetNode<Control>("Icon");
		RemoveChild(_icon);

		GetTree().CurrentScene.Ready += () => {
            _attack_selection = ((level)GetTree().CurrentScene).RangedAttackSelection;
			_attack_selection.TargetSelected += _TargetSelected;
			_attack_selection.TargetHovered += _TargetHovered;

			_battle_ui = GetTree().CurrentScene.GetNode<battle_ui>("BattleUI");
        };
	}

	private async void _TargetSelected(unit target)
	{
		_battle_ui!.Suspend();
		_unit!.ActionPoints--;
		await _unit!.PlayAnimation(unit.Animation.Shooting);
		_unit!.ChangeAnimationState(unit.Animation.Idle);
		target.ReceiveDamage(new Damage(Damage, _unit!, this));
		_unit!.FinishAction();
	}

	private void _TargetHovered(unit target)
	{
		_unit!.LookAt(target);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void Equipped(unit unit)
    {
		_unit = unit;

		_model!.Visible = true;
		_unit = unit;
		unit.Attach("RightHand", _model!);
		unit.Actions.Add(this);
		unit.ActionSelected += _ActionSelected;
		unit.ActionFinished += _ActionFinished;
    }

	private void _ActionSelected(IActionable action)
	{
		_attack_selection!.HideAttackBoxes();
	}

	private void _ActionFinished(IActionable action)
	{
		_attack_selection!.HideAttackBoxes();
		_unit!.ChangeAnimationState(unit.Animation.Idle);
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
			_unit!.SelectAction(this);
			_unit.ChangeAnimationState(unit.Animation.Aiming);
			_attack_selection!.ShowAttackBoxes(_unit!, global::Owner.AI, Range);
        }
    }
}
