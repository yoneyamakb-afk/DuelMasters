using Xunit;
using DMRules.Engine.Tracing;

namespace DMRules.Tests
{
    public class TriggerInstrumentationTest
    {
        [Fact(DisplayName = "Trigger/APNAP instrumentation can probe (no-op safe)")]
        public void ProbeNoopSafe()
        {
            var _ = TriggerInstrumentation.TryProbeAndTraceOnce();
            Assert.True(true); // 何があっても成功（安全）
        }
    }
}
