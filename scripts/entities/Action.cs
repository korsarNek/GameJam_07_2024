using Godot;
using System;

public interface IActionable
{
    bool CanBeCanceled { get; }
}

public interface ISelectable : IActionable
{
    Control Icon { get; }
}

public interface IRangedAttack : ISelectable
{
    
}

public interface IMeleeAttack : ISelectable
{
    
}
