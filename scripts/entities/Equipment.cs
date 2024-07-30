using Godot;
using System;

public interface IEquipment
{
    void Equipped(unit unit);
    void Unequipped(unit unit);
}

public interface IRangedWeapon : IEquipment
{
    float Range { get; }
}

public interface IMeleeWeapon : IEquipment
{
    int Range { get; }
}

public class Armor : IEquipment
{
    [Export]
    public float DamageReductionPercentage = 0.1f;

    public void Equipped(unit unit)
    {
        unit.ReceivedDamage += CalculateDamage;
    }

    public void Unequipped(unit unit)
    {
        unit.ReceivedDamage -= CalculateDamage;
    }

    private Damage CalculateDamage(Damage damage)
    {
        return damage with { Amount = damage.Amount - (damage.Amount * DamageReductionPercentage) };
    }
}
