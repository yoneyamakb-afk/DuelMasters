using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DMRules.Engine;

namespace DMRules.Storage
{
    public interface ITraceStore
    {
        Task SaveAsync(IEnumerable<TraceEntry> trace, CancellationToken ct = default);
    }
}
