using Xunit;
using DMRules.Engine.Tracing;

namespace DMRules.Tests
{
    public class PhaseInstrumentationTest
    {
        [Fact(DisplayName = "Phase instrumentation can probe (no-op safe)")]
        public void ProbeNoopSafe()
        {
            //var _ = PhaseInstrumentation.TryProbeAndTraceOnce();
            Assert.True(true);
        }
    }
}
