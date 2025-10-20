using System.Data;

namespace DMRules.Storage
{
    /// <summary>
    /// Provider-agnostic connection factory. Implement this in your app using any ADO.NET provider.
    /// </summary>
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }

    /// <summary>
    /// Lightweight helper to build a factory from a delegate.
    /// </summary>
    public sealed class FuncConnectionFactory : IDbConnectionFactory
    {
        private readonly System.Func<IDbConnection> _factory;
        public FuncConnectionFactory(System.Func<IDbConnection> factory) => _factory = factory;
        public IDbConnection Create() => _factory();
    }
}
