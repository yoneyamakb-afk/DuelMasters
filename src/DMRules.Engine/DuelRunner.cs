using DMRules.Engine;

namespace DMRules.Engine
{
    public static class DuelRunner
    {
        public static GameState RunOneGame(GameState s)
        {
            s = s.AddTrace("Battle.Start", "Duel started");
            s = s.AddTrace("Turn.1", "Player0 plays Dream Bolmeteus White Dragon");
            s = s.AddTrace("Attack", "Bolmeteus destroys opponent shield");
            s = s.AddTrace("Battle.End", "Player0 wins");
            return s;
        }
    }
}
