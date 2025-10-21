using Xunit;
using System.IO;

namespace DMRules.Tests
{
    public class ReplayTests
    {
        [Fact(Skip = "Enable after ReplayRunner is wired to engine.")]
        public void SampleRealLogExistsAndReplayToolOutputsTrace()
        {
            Assert.True(File.Exists("samples/real_logs/game_001.json"));
        }
    }
}
