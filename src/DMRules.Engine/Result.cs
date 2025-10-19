namespace DMRules.Engine;

public readonly record struct ExecResult(bool ChangedFlag)
{
    public static readonly ExecResult NoChange = new(false);
    public static readonly ExecResult DidChange = new(true);
}
