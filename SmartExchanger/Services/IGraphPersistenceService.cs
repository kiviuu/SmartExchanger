using SmartExchanger.Models;

namespace SmartExchanger.Services
{
    public interface IGraphPersistenceService
    {
        Task SaveAsync(string filePath, IReadOnlyCollection<BaseNodeViewModel> nodes,
            IReadOnlyCollection<ConnectionViewModel> connections, CancellationToken cancellationToken = default);

        Task<GraphLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
