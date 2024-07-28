using System;

public class SkillAlreadyAssignedException : InvalidOperationException
{
    public SkillAlreadyAssignedException() : base("This skill has already leared by a unit, created a new skill instance for another unit to learn the skill.") {}
}

public class ObjectNotInitializedException : InvalidOperationException
{
    public ObjectNotInitializedException() : base("Object has not been initialized.") {}
}