using System.Text.Json;

namespace SmartExchanger.Models
{
    public sealed class GraphDocument
    {
        public const int CurrentFormatVersion = 1;

        public int FormatVersion { get; init; } = CurrentFormatVersion;

        public List<NodeDocument> Nodes { get; init; } = new();

        public List<ConnectionDocument> Connections { get; init; } = new();
    }

    public sealed class NodeDocument
    {
        public Guid Id { get; init; }

        public required string TypeId { get; init; }

        public double X { get; init; }

        public double Y { get; init; }

        public JsonElement State { get; init; }
    }

    public sealed class ConnectionDocument
    {
        public Guid SourceNodeId { get; init; }

        public required string SourcePortId { get; init; }

        public Guid TargetNodeId { get; init; }

        public required string TargetPortId { get; init; }
    }

    public sealed class GraphLoadResult
    {
        public IReadOnlyList<BaseNodeViewModel> Nodes { get; }

        public IReadOnlyList<ConnectionViewModel> Connections { get; }

        public IReadOnlyList<string> Warnings { get; }

        public GraphLoadResult(IReadOnlyList<BaseNodeViewModel> nodes, IReadOnlyList<ConnectionViewModel> connections,
            IReadOnlyList<string> warnings)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));

            Connections = connections ?? throw new ArgumentNullException(nameof(connections));

            Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        }
    }
}
