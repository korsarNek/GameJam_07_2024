using Godot;
using System;

public interface IAbility
{
    void Learned(unit unit);

    void Unlearned();
}