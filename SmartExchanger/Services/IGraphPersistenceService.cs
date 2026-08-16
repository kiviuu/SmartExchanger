using SmartExchanger.Models;

namespace SmartExchanger.Services
{
    public interface IGraphPersistenceService
    {
        GraphDocument CaptureDocument(IReadOnlyCollection<BaseNodeViewModel> nodes, IReadOnlyCollection<ConnectionViewModel> connections, string projectDirectory);
        GraphLoadResult RestoreDocument(GraphDocument document, string projectDirectory);
        Task SaveAsync(string filePath, IReadOnlyCollection<BaseNodeViewModel> nodes,
            IReadOnlyCollection<ConnectionViewModel> connections, CancellationToken cancellationToken = default);

        Task<GraphLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
