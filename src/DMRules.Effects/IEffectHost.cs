
namespace DMRules.Effects;

/// <summary>
/// Host-facing abstraction for executing EffectIR actions against the real engine.
/// You will provide an implementation on the engine side (M12.2+).
/// </summary>
public interface IEffectHost
{
    void DrawCards(int playerId, int count);
    void AddMana(int playerId, int count);
}
