using Xunit;
using DMRules.Tests.Snapshots;

namespace DMRules.Tests.M19
{
    public class M19_SnapshotHarness_DemoTests
    {
        [Fact]
        public void PlayTwin_Demo_Snapshot()
        {
            // Deterministic, engine-independent demo payload
            var obj = new {
                Case = "PlayTwinDemo",
                SourceFaceId = 10,
                Side = "B",
                ResolvedFaceId = 11
            };
            SnapshotAssert.MatchJson("m19_demo_playtwin", obj);
        }
    }
}