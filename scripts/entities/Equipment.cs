using Godot;
using System;

public interface IEquipment
{
    void Equipped(unit unit);
    void Unequipped(unit unit);
}

public class RangedWeapon : IEquipment
{
    public int MaximumAmmunition => 12;
    public int Ammunition { get; set; }

    public float Range => 8;
    public float Damage => 10;

    public RangedWeapon()
    {
        Ammunition = MaximumAmmunition;
    }

    public void Equipped(unit unit)
    {
        throw new NotImplementedException();
    }

    public void Unequipped(unit unit)
    {
        throw new NotImplementedException();
    }
}

public interface IMeleeWeapon : IEquipment
{
    int Range { get; }
    float Damage { get; }
}

public class Armor : IEquipment
{
    [Export]
    public float DamageReductionPercentage = 0.1f;

    public void Equipped(unit unit)
    {
        unit.ReceiveDamage += CalculateDamage;
    }

    public void Unequipped(unit unit)
    {
        unit.ReceiveDamage -= CalculateDamage;
    }

    private Damage CalculateDamage(Damage damage)
    {
        return damage with { Amount = damage.Amount - (damage.Amount * DamageReductionPercentage) };
    }
}
