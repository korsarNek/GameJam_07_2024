using Godot;
using System;

public interface IAction
{
    Control Icon { get; }
}

public class RangedAttack : IAction
{
    public int ActionPointCost => 1;

    public Control Icon => throw new NotImplementedException();

    public void Execute()
    {
        //TODO: Open ranged attack UI
    }
}

public interface IMeleeAttack : IAction
{
    
}

public class ChargeAttack : IAction
{
    public int ActionPointCost => throw new NotImplementedException();

    public Control Icon => throw new NotImplementedException();

    public void Execute()
    {
        throw new NotImplementedException();
    }
}

public class Reload : IAction
{
    public int ActionPointCost => throw new NotImplementedException();

    public Control Icon => throw new NotImplementedException();

    public void Execute()
    {
        throw new NotImplementedException();
    }
}
