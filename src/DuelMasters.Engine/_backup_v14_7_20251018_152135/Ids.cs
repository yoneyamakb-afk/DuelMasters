namespace DuelMasters.Engine;

public readonly record struct CardId(int Value);
public readonly record struct PlayerId(int Value)
{
    public PlayerId Opponent() => new PlayerId(Value ^ 1);
}


