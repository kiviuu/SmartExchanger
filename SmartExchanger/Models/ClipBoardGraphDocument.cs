namespace SmartExchanger.Models
{
    /// <summary>
    /// Serializable representation of a copied fragment
    /// of a SmartExchanger graph.
    ///
    /// The document contains only nodes selected for copying
    /// and connections whose both endpoints belong to that selection.
    /// </summary>
    public sealed class ClipboardGraphDocument
    {
        public const int CurrentFormatVersion = 1;

        public int FormatVersion { get; init; } = CurrentFormatVersion;
        public required string SourceDirectory { get; init; }
        public double AnchorX { get; init; }
        public double AnchorY { get; init; }

        public List<NodeDocument> Nodes { get; init; } = new();
        public List<ConnectionDocument> Connections { get; init; } = new();
    }
}