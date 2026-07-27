using SmartExchanger.Models;
using SmartExchanger.Options;
using System.IO;
using System.Text.Json;

namespace SmartExchanger.Services
{
    public sealed class GraphPersistenceService: IGraphPersistenceService
    {
        private readonly INodeFactory _nodeFactory;
        private readonly INodeStateSerializer _nodeStateSerializer;

        public GraphPersistenceService(INodeFactory nodeFactory, INodeStateSerializer nodeStateSerializer)
        {
            _nodeFactory = nodeFactory ?? throw new ArgumentNullException(nameof(nodeFactory));
            _nodeStateSerializer = nodeStateSerializer ?? throw new ArgumentNullException(nameof(nodeStateSerializer));
        }

        public async Task SaveAsync(string filePath, IReadOnlyCollection<BaseNodeViewModel> nodes,
            IReadOnlyCollection<ConnectionViewModel> connections, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(nodes);
            ArgumentNullException.ThrowIfNull(connections);

            string fullFilePath = Path.GetFullPath(filePath);
            string projectDirectory = Path.GetDirectoryName(fullFilePath) ?? throw new InvalidOperationException("The project path does not contain a directory.");

            Directory.CreateDirectory(projectDirectory);
            GraphDocument document = CreateDocument(nodes, connections, projectDirectory);

            string temporaryFilePath = fullFilePath + ".tmp";

            try
            {
                await using (
                    var stream =
                        new FileStream(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None,
                            bufferSize: 64 * 1024, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(stream, document, GraphJsonOptions.Instance, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                File.Move(temporaryFilePath, fullFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryFilePath))
                {
                    File.Delete(temporaryFilePath);
                }
            }
        }

        public async Task<GraphLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            string fullFilePath = Path.GetFullPath(filePath);
            string projectDirectory = Path.GetDirectoryName(fullFilePath) ?? throw new InvalidOperationException("The project path does not contain a directory.");
            GraphDocument document;

            try
            {
                await using var stream =
                    new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                        bufferSize: 64 * 1024, useAsync: true);

                document = await JsonSerializer
                        .DeserializeAsync<GraphDocument>(stream, GraphJsonOptions.Instance, cancellationToken)
                        ?? throw new InvalidDataException("The selected project file is empty.");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("The selected file does not contain a valid SmartExchanger project.", ex);
            }

            ValidateDocument(document);

            var createdNodes = new List<BaseNodeViewModel>(document.Nodes.Count);
            var createdConnections = new List<ConnectionViewModel>(document.Connections.Count);
            var warnings = new List<string>();

            try
            {
                var nodesById = new Dictionary<Guid, BaseNodeViewModel>();

                foreach (NodeDocument nodeDocument in document.Nodes)
                {
                    BaseNodeViewModel node = _nodeFactory.Create(nodeDocument.TypeId);
                    node.Id = nodeDocument.Id;
                    node.Location = new Point(nodeDocument.X, nodeDocument.Y);
                    _nodeStateSerializer.RestoreState(node, nodeDocument.State, projectDirectory, warnings);
                    createdNodes.Add(node);
                    nodesById.Add(node.Id, node);
                }

                ValidateSingletonNodes(createdNodes);

                var connectedTargets = new HashSet<ConnectorViewModel>();

                var connectionKeys = new HashSet<string>(StringComparer.Ordinal);

                foreach (ConnectionDocument connectionDocument in document.Connections)
                {
                    BaseNodeViewModel sourceNode = GetNode(nodesById, connectionDocument.SourceNodeId, "source");

                    BaseNodeViewModel targetNode = GetNode(nodesById, connectionDocument.TargetNodeId, "target");

                    ConnectorViewModel sourceConnector = GetConnector(sourceNode.Outputs, connectionDocument.SourcePortId, sourceNode,  "output");

                    ConnectorViewModel targetConnector = GetConnector(targetNode.Inputs, connectionDocument.TargetPortId, targetNode, "input");

                    if (sourceNode == targetNode)
                    {
                        throw new InvalidDataException($"Node '{sourceNode.Id}' is connected to itself.");
                    }

                    if (!connectedTargets.Add(targetConnector))
                    {
                        throw new InvalidDataException($"Input port '{targetConnector.Id}' of node '{targetNode.Id}' contains more than one connection.");
                    }

                    string connectionKey =
                        $"{sourceNode.Id:N}:" +
                        $"{sourceConnector.Id}>" +
                        $"{targetNode.Id:N}:" +
                        $"{targetConnector.Id}";

                    if (!connectionKeys.Add(connectionKey))
                    {
                        throw new InvalidDataException("The project contains a duplicated connection.");
                    }

                    createdConnections.Add(new ConnectionViewModel(sourceConnector, targetConnector));
                }

                EnsureGraphIsAcyclic(createdNodes, createdConnections);

                return new GraphLoadResult(createdNodes, createdConnections, warnings);
            }
            catch
            {
                foreach (BaseNodeViewModel node in createdNodes)
                {
                    if (node is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                throw;
            }
        }

        private GraphDocument CreateDocument(IReadOnlyCollection<BaseNodeViewModel> nodes, IReadOnlyCollection<ConnectionViewModel> connections,
            string projectDirectory)
        {
            var nodeDocuments = nodes.Select(
                        node =>
                            new NodeDocument
                            {
                                Id = node.Id,
                                TypeId = _nodeFactory.GetTypeId(node),
                                X = node.Location.X,
                                Y = node.Location.Y,
                                State = _nodeStateSerializer.CaptureState(node, projectDirectory)
                            }).ToList();

            var connectionDocuments = connections.Select(
                        connection =>
                            new ConnectionDocument
                            {
                                SourceNodeId = connection.Source.Node.Id,
                                SourcePortId = connection.Source.Id,
                                TargetNodeId = connection.Target.Node.Id,
                                TargetPortId = connection.Target.Id
                            }).ToList();

            return new GraphDocument
            {
                FormatVersion = GraphDocument.CurrentFormatVersion,
                Nodes = nodeDocuments,
                Connections = connectionDocuments
            };
        }

        private static void ValidateDocument(GraphDocument document)
        {
            if (document.FormatVersion != GraphDocument.CurrentFormatVersion)
            {
                throw new NotSupportedException(
                    $"Project format version '{document.FormatVersion}' " +
                    "is not supported. " +
                    $"Supported version: {GraphDocument.CurrentFormatVersion}.");
            }
            if (document.Nodes is null)
            {
                throw new InvalidDataException("The project does not contain a node collection.");
            }
            if (document.Connections is null)
            {
                throw new InvalidDataException("The project does not contain a connection collection.");
            }

            var nodeIds = new HashSet<Guid>();

            foreach (NodeDocument node in document.Nodes)
            {
                if (node.Id == Guid.Empty)
                {
                    throw new InvalidDataException("The project contains a node with an empty identifier.");
                }
                if (!nodeIds.Add(node.Id))
                {
                    throw new InvalidDataException($"The project contains duplicated node identifier '{node.Id}'.");
                }

                if (string.IsNullOrWhiteSpace(node.TypeId))
                {
                    throw new InvalidDataException($"Node '{node.Id}' does not contain a type identifier.");
                }

                if (!double.IsFinite(node.X) || !double.IsFinite(node.Y))
                {
                    throw new InvalidDataException($"Node '{node.Id}' contains an invalid location.");
                }
                if (node.State.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                {
                    throw new InvalidDataException($"Node '{node.Id}' does not contain a state object.");
                }
            }

            foreach (ConnectionDocument connection in document.Connections)
            {
                if (connection.SourceNodeId == Guid.Empty || connection.TargetNodeId == Guid.Empty)
                {
                    throw new InvalidDataException("The project contains a connection with an empty node identifier.");
                }
                if (string.IsNullOrWhiteSpace(connection.SourcePortId) || string.IsNullOrWhiteSpace(connection.TargetPortId))
                {
                    throw new InvalidDataException("The project contains a connection with an empty port identifier.");
                }
            }
        }

        private static void ValidateSingletonNodes(IReadOnlyCollection<BaseNodeViewModel> nodes)
        {
            int textureSizeCount = nodes.OfType<TextureSizeNodeViewModel>().Count();

            if (textureSizeCount != 1)
            {
                throw new InvalidDataException("A project must contain exactly one Texture Size node.");
            }

            if (nodes.OfType<MaterialOutputNodeViewModel>().Count() > 1)
            {
                throw new InvalidDataException("A project cannot contain more than one Material Output node.");
            }

            if (nodes.OfType<TexturePreviewNodeViewModel>().Count() > 1)
            {
                throw new InvalidDataException("A project cannot contain more than one Texture Preview node.");
            }
        }

        private static BaseNodeViewModel GetNode(IReadOnlyDictionary<Guid, BaseNodeViewModel> nodesById, Guid nodeId,
            string endpointName)
        {
            if (!nodesById.TryGetValue(nodeId, out BaseNodeViewModel? node))
            {
                throw new InvalidDataException($"Connection {endpointName} node '{nodeId}' does not exist.");
            }
            return node;
        }

        private static ConnectorViewModel GetConnector(IEnumerable<ConnectorViewModel> connectors, string connectorId,
            BaseNodeViewModel node, string connectorKind)
        {
            ConnectorViewModel[] matches = connectors.Where(
                        connector =>
                            string.Equals(connector.Id, connectorId, StringComparison.Ordinal)).ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidDataException($"The {connectorKind} port '{connectorId}' of node '{node.Id}' does not exist.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Node class '{node.GetType().FullName}' " +
                    $"contains duplicated {connectorKind} port identifier '{connectorId}'.");
            }
            return matches[0];
        }

        private static void EnsureGraphIsAcyclic(IReadOnlyCollection<BaseNodeViewModel> nodes, IReadOnlyCollection<ConnectionViewModel> connections)
        {
            var adjacency = nodes.ToDictionary(node => node.Id, _ => new List<Guid>());

            foreach (ConnectionViewModel connection in connections)
            {
                adjacency[connection.Source.Node.Id].Add(connection.Target.Node.Id);
            }

            var states = new Dictionary<Guid, VisitState>();

            foreach (BaseNodeViewModel node in nodes)
            {
                Visit(node.Id, adjacency, states);
            }
        }

        private static void Visit(Guid nodeId, IReadOnlyDictionary<Guid, List<Guid>> adjacency, IDictionary<Guid, VisitState> states)
        {
            if (states.TryGetValue(nodeId, out VisitState state))
            {
                if (state == VisitState.Visiting)
                {
                    throw new InvalidDataException("The project contains a cycle in the node graph.");
                }

                if (state == VisitState.Visited)
                {
                    return;
                }
            }

            states[nodeId] = VisitState.Visiting;

            foreach (Guid childId in adjacency[nodeId])
            {
                Visit(childId, adjacency, states);
            }

            states[nodeId] = VisitState.Visited;
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }
    }
}
