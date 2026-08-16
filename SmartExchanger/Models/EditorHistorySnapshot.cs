namespace SmartExchanger.Models
{
    public sealed class EditorHistorySnapshot
    {
        public required string StateDirectory { get; init; }
        public required GraphDocument Graph { get; init; }
        public string? CurrentProjectPath { get; init; }
        public List<Guid> SelectedNodesIds { get; init; } = new();
        public List<ConnectionDocument> SelectedConnections { get; init; } = new(); 
    }
}
