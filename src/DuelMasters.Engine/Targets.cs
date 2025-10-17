namespace DuelMasters.Engine;

public readonly record struct TargetSpec(int Index)
{
    public static readonly TargetSpec None = new TargetSpec(-1);
}
