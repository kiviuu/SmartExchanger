using CommunityToolkit.Mvvm.Input;
using SmartExchanger.Models;
using SmartExchanger.Options;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SmartExchanger.ViewModels
{
    public partial class EditorViewModel
    {
        private const string GraphClipboardFormat = "SmartExchanger.GraphSelection.v1";

        private Point _graphPointerLocation;


        /// <summary>
        /// Updates the last known mouse position in graph coordinates.
        /// This position is used for node creation and clipboard paste.
        /// </summary>
        public void UpdateGraphPointerLocation(Point graphLocation)
        {
            if (!double.IsFinite(graphLocation.X) ||
                !double.IsFinite(graphLocation.Y))
            {
                return;
            }

            _graphPointerLocation = graphLocation;
        }


        private void OnSelectedNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CopySelectionCommand.NotifyCanExecuteChanged();

            CutSelectionCommand.NotifyCanExecuteChanged();

            PasteSelectionCommand.NotifyCanExecuteChanged();
        }


        [RelayCommand(CanExecute = nameof(CanCopySelection))]
        private void CopySelection(BaseNodeViewModel? contextNode)
        {
            TryCopySelectionToClipboard(contextNode, out _);
        }


        private bool CanCopySelection(BaseNodeViewModel? contextNode)
        {
            if (_isDisposed ||IsTextInputFocused())
            {
                return false;
            }

            /*
             * If the context menu was opened on a node that is not
             * currently selected, the command will operate only on
             * that context node.
             */
            if (contextNode is not null && !SelectedNodes.Contains(contextNode))
            {
                return IsClipboardEligible(contextNode);
            }

            return SelectedNodes.Any(IsClipboardEligible);
        }


        [RelayCommand(CanExecute = nameof(CanCutSelection))]
        private void CutSelection(BaseNodeViewModel? contextNode)
        {
            if (!TryCopySelectionToClipboard(contextNode, out List<BaseNodeViewModel> copiedNodes))
            {
                return;
            }

            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();
            bool graphChanged = RemoveNodesInternal(copiedNodes);

            SelectedNodes.Clear();
            SelectedConnections.Clear();

            if (!graphChanged)
            {
                return;
            }

            InvalidateGraph(requestGpuPurge: true);

            CommitGraphMutation(copiedNodes.Count == 1 ? "Cut node" : $"Cut {copiedNodes.Count} nodes", historyBefore);
        }


        private bool CanCutSelection(BaseNodeViewModel? contextNode)
        {
            return CanCopySelection(contextNode);
        }


        [RelayCommand(CanExecute = nameof(CanPasteSelection))]
        private void PasteSelection()
        {
            if (_isDisposed)
            {
                return;
            }

            if (!TryReadClipboardDocument(out ClipboardGraphDocument? document, out string? clipboardError))
            {
                if (!string.IsNullOrWhiteSpace(clipboardError))
                {
                    MessageBox.Show(
                        clipboardError,
                        "Clipboard",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return;
            }

            ClipboardPasteResult pasteResult;

            try
            {
                pasteResult = CreatePastedGraph(document!, _graphPointerLocation);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Could not paste the copied nodes." +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "Paste failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (pasteResult.Nodes.Count == 0)
            {
                ShowPasteWarnings(pasteResult.Warnings);

                return;
            }

            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();

            /*
             * The graph is modified only after the entire copied
             * fragment has been reconstructed successfully.
             */
            foreach (BaseNodeViewModel node in pasteResult.Nodes)
            {
                AddNodeInternal(node);
            }

            foreach (ConnectionViewModel connection in pasteResult.Connections)
            {
                Connections.Add(connection);
            }

            /*
             * Clear the previous selection and select every pasted node.
             * Because SelectedNodes is bound to Nodify's SelectedItems,
             * the new containers become selected in the editor.
             */
            SelectedConnections.Clear();

            SelectedNodes.Clear();

            foreach (BaseNodeViewModel node in pasteResult.Nodes)
            {
                SelectedNodes.Add(node);
            }

            InvalidateGraph(requestGpuPurge: false);
            CommitGraphMutation(pasteResult.Nodes.Count == 1 ? "Paste node" : $"Paste {pasteResult.Nodes.Count} nodes", historyBefore);
            ShowPasteWarnings(pasteResult.Warnings);
        }


        private bool CanPasteSelection()
        {
            if (_isDisposed || IsTextInputFocused())
            {
                return false;
            }

            try
            {
                return Clipboard.ContainsData(GraphClipboardFormat);
            }
            catch (ExternalException)
            {
                return false;
            }
        }


        private bool TryCopySelectionToClipboard(BaseNodeViewModel? contextNode, out List<BaseNodeViewModel> copiedNodes)
        {
            copiedNodes = new List<BaseNodeViewModel>();

            EnsureContextNodeSelected(contextNode);

            copiedNodes = SelectedNodes
                    .Where(IsClipboardEligible)
                    .Distinct(ReferenceComparer<BaseNodeViewModel>.Instance)
                    .ToList();

            if (copiedNodes.Count == 0)
            {
                return false;
            }

            try
            {
                ClipboardGraphDocument document = CreateClipboardDocument(copiedNodes);

                string payload = JsonSerializer.Serialize(document, GraphJsonOptions.Instance);

                if (!TryWriteClipboardPayload(payload, out string? errorMessage))
                {
                    MessageBox.Show(
                        "Could not copy the selected nodes " +
                        "to the Windows clipboard." +
                        Environment.NewLine +
                        Environment.NewLine +
                        errorMessage,
                        "Clipboard failure",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    copiedNodes.Clear();
                    return false;
                }

                PasteSelectionCommand.NotifyCanExecuteChanged();

                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Could not serialize the selected nodes." +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    "Copy failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                copiedNodes.Clear();
                return false;
            }
        }


        private ClipboardGraphDocument CreateClipboardDocument(IReadOnlyCollection<BaseNodeViewModel> copiedNodes)
        {
            if (copiedNodes.Count == 0)
            {
                throw new InvalidOperationException("There are no nodes to copy.");
            }

            string sourceDirectory = GetClipboardSourceDirectory();

            double anchorX = copiedNodes.Min(node => node.Location.X);

            double anchorY = copiedNodes.Min(node => node.Location.Y);

            var copiedNodeSet = new HashSet<BaseNodeViewModel>(copiedNodes, ReferenceComparer<BaseNodeViewModel>.Instance);

            List<NodeDocument> nodeDocuments = copiedNodes
                    .Select(node =>
                            new NodeDocument
                            {
                                /*
                                 * These IDs identify nodes only inside
                                 * the clipboard document.
                                 *
                                 * New IDs are generated during paste.
                                 */
                                Id = node.Id,
                                TypeId = nodeFactory.GetTypeId(node),
                                X = node.Location.X,
                                Y = node.Location.Y,
                                State = nodeStateSerializer.CaptureState(node,sourceDirectory)
                            }).ToList();

            List<ConnectionDocument> connectionDocuments = Connections
                    .Where(connection =>
                            copiedNodeSet.Contains(connection.Source.Node) &&
                            copiedNodeSet.Contains(connection.Target.Node))
                    .Select(connection =>
                            new ConnectionDocument
                            {
                                SourceNodeId = connection.Source.Node.Id,
                                SourcePortId = connection.Source.Id,
                                TargetNodeId = connection.Target.Node.Id,
                                TargetPortId = connection.Target.Id
                            }).ToList();

            return new ClipboardGraphDocument
            {
                FormatVersion = ClipboardGraphDocument.CurrentFormatVersion,
                SourceDirectory = sourceDirectory,
                AnchorX = anchorX,
                AnchorY = anchorY,
                Nodes = nodeDocuments,
                Connections = connectionDocuments
            };
        }


        private ClipboardPasteResult CreatePastedGraph(ClipboardGraphDocument document, Point insertionLocation)
        {
            ValidateClipboardDocument(document);

            string sourceDirectory = Path.GetFullPath(document.SourceDirectory);

            var createdNodes = new List<BaseNodeViewModel>(document.Nodes.Count);

            var createdConnections = new List<ConnectionViewModel>(document.Connections.Count);

            var warnings = new List<string>();

            var nodesByClipboardId = new Dictionary<Guid, BaseNodeViewModel>();

            var originalIds = new HashSet<Guid>();

            try
            {
                foreach (NodeDocument nodeDocument in document.Nodes)
                {
                    if (nodeDocument.Id == Guid.Empty)
                    {
                        throw new InvalidDataException("The clipboard contains a node with an empty identifier.");
                    }

                    if (!originalIds.Add(nodeDocument.Id))
                    {
                        throw new InvalidDataException($"The clipboard contains duplicated node identifier '{nodeDocument.Id}'.");
                    }

                    BaseNodeViewModel node = nodeFactory.Create(nodeDocument.TypeId);

                    if (!CanInsertPastedNode(node, createdNodes, out string? warning))
                    {
                        DisposeNode(node);

                        if (!string.IsNullOrWhiteSpace(warning))
                        {
                            warnings.Add(warning);
                        }
                        continue;
                    }

                    /*
                     * Add the node to the temporary collection before
                     * restoring its state. 
                     */
                    createdNodes.Add(node);

                    double relativeX = nodeDocument.X - document.AnchorX;
                    double relativeY = nodeDocument.Y - document.AnchorY;

                    node.Location = new Point(insertionLocation.X + relativeX, insertionLocation.Y + relativeY);

                    nodeStateSerializer.RestoreState(node, nodeDocument.State, sourceDirectory, warnings);

                    nodesByClipboardId.Add(nodeDocument.Id, node);
                }

                var connectedTargets = new HashSet<ConnectorViewModel>(ReferenceComparer<ConnectorViewModel>.Instance);

                var connectionKeys = new HashSet<string>(StringComparer.Ordinal);

                foreach (ConnectionDocument connectionDocument in document.Connections)
                {
                    /*
                     * A node may have been intentionally skipped,
                     * Connections attached to skipped nodes are skipped.
                     */
                    if (!nodesByClipboardId.TryGetValue(connectionDocument.SourceNodeId, out BaseNodeViewModel? sourceNode) ||
                        !nodesByClipboardId.TryGetValue(connectionDocument.TargetNodeId, out BaseNodeViewModel? targetNode))
                    {
                        continue;
                    }

                    ConnectorViewModel sourceConnector =
                        FindClipboardConnector(
                            sourceNode.Outputs,
                            connectionDocument.SourcePortId,
                            sourceNode,
                            "output");

                    ConnectorViewModel targetConnector =
                        FindClipboardConnector(
                            targetNode.Inputs,
                            connectionDocument.TargetPortId,
                            targetNode,
                            "input");

                    if (!connectedTargets.Add(targetConnector))
                    {
                        throw new InvalidDataException(
                            $"Input port '{targetConnector.Id}' " +
                            $"of node '{targetNode.Title}' " +
                            "contains more than one copied connection.");
                    }

                    string connectionKey = $"{sourceNode.Id:N}: {sourceConnector.Id}>" +
                        $"{targetNode.Id:N}: {targetConnector.Id}";

                    if (!connectionKeys.Add(connectionKey))
                    {
                        throw new InvalidDataException("The clipboard contains a duplicated connection.");
                    }

                    createdConnections.Add(new ConnectionViewModel(sourceConnector, targetConnector));
                }

                EnsurePastedGraphIsAcyclic(createdNodes, createdConnections);

                return new ClipboardPasteResult(createdNodes, createdConnections, warnings);
            }
            catch
            {
                foreach (BaseNodeViewModel node in createdNodes)
                {
                    DisposeNode(node);
                }
                throw;
            }
        }


        private bool CanInsertPastedNode(BaseNodeViewModel node, IReadOnlyCollection<BaseNodeViewModel> nodesBeingCreated, out string? warning)
        {
            warning = null;

            if (node is TextureSizeNodeViewModel)
            {
                warning = "Texture Size was not pasted because the graph must contain exactly one instance.";
                return false;
            }

            if (node is MaterialOutputNodeViewModel &&
                (Nodes.OfType<MaterialOutputNodeViewModel>().Any() ||
                 nodesBeingCreated.OfType<MaterialOutputNodeViewModel>().Any()))
            {
                warning = "Material Output was not pasted because the graph already contains one.";
                return false;
            }

            if (node is TexturePreviewNodeViewModel &&
                (Nodes.OfType<TexturePreviewNodeViewModel>().Any() ||
                 nodesBeingCreated.OfType<TexturePreviewNodeViewModel>().Any()))
            {
                warning = "Texture Preview was not pasted because the graph already contains one.";
                return false;
            }

            return true;
        }


        private static ConnectorViewModel FindClipboardConnector(IEnumerable<ConnectorViewModel> connectors, string connectorId,
            BaseNodeViewModel node, string connectorKind)
        {
            ConnectorViewModel[] matches = connectors.Where(connector =>string.Equals(connector.Id, connectorId, StringComparison.Ordinal)).ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidDataException($"The copied {connectorKind} port '{connectorId}' of node '{node.Title}' does not exist.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Node class '{node.GetType().FullName}' contains duplicated {connectorKind} port identifier '{connectorId}'.");
            }
            return matches[0];
        }


        private static void ValidateClipboardDocument(ClipboardGraphDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            if (document.FormatVersion != ClipboardGraphDocument.CurrentFormatVersion)
            {
                throw new NotSupportedException($"Clipboard format version '{document.FormatVersion}' is not supported. " +
                    $"Supported version: {ClipboardGraphDocument.CurrentFormatVersion}.");
            }

            if (string.IsNullOrWhiteSpace(document.SourceDirectory))
            {
                throw new InvalidDataException("The clipboard does not contain a source directory.");
            }

            if (!double.IsFinite(document.AnchorX) || !double.IsFinite(document.AnchorY))
            {
                throw new InvalidDataException("The clipboard contains an invalid anchor.");
            }

            if (document.Nodes is null)
            {
                throw new InvalidDataException("The clipboard does not contain a node collection.");
            }

            if (document.Connections is null)
            {
                throw new InvalidDataException("The clipboard does not contain a connection collection.");
            }

            foreach (NodeDocument node in document.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.TypeId))
                {
                    throw new InvalidDataException($"Copied node '{node.Id}' does not contain a type identifier.");
                }

                if (!double.IsFinite(node.X) || !double.IsFinite(node.Y))
                {
                    throw new InvalidDataException($"Copied node '{node.Id}' contains an invalid location.");
                }
            }
        }


        private static void EnsurePastedGraphIsAcyclic(IReadOnlyCollection<BaseNodeViewModel> nodes, IReadOnlyCollection<ConnectionViewModel> connections)
        {
            var adjacency = nodes.ToDictionary(node => node,
                    _ => new List<BaseNodeViewModel>(),
                    ReferenceComparer<BaseNodeViewModel>.Instance);

            foreach (ConnectionViewModel connection in connections)
            {
                adjacency[connection.Source.Node].Add(connection.Target.Node);
            }

            var states = new Dictionary<BaseNodeViewModel, ClipboardVisitState>(ReferenceComparer<BaseNodeViewModel>.Instance);

            foreach (BaseNodeViewModel node in nodes)
            {
                VisitPastedNode(node, adjacency, states);
            }
        }


        private static void VisitPastedNode(BaseNodeViewModel node, IReadOnlyDictionary<BaseNodeViewModel,
                List<BaseNodeViewModel>> adjacency, IDictionary<BaseNodeViewModel,ClipboardVisitState> states)
        {
            if (states.TryGetValue(node, out ClipboardVisitState state))
            {
                if (state == ClipboardVisitState.Visiting)
                {
                    throw new InvalidDataException("The copied graph contains a cycle.");
                }

                if (state == ClipboardVisitState.Visited)
                {
                    return;
                }
            }

            states[node] = ClipboardVisitState.Visiting;

            foreach (BaseNodeViewModel child in adjacency[node])
            {
                VisitPastedNode(child, adjacency, states);
            }

            states[node] = ClipboardVisitState.Visited;
        }


        private void EnsureContextNodeSelected(BaseNodeViewModel? contextNode)
        {
            if (contextNode is null || !Nodes.Contains(contextNode) || SelectedNodes.Contains(contextNode))
            {
                return;
            }

            /*
             * Right-clicking an unselected node means:
             * operate only on that node.
             *
             * Right-clicking a node already belonging to a group
             * keeps the entire group selected.
             */
            SelectedConnections.Clear();
            SelectedNodes.Clear();
            SelectedNodes.Add(contextNode);
        }


        private static bool IsClipboardEligible(BaseNodeViewModel node)
        {
            return node is not TextureSizeNodeViewModel;
        }


        private string GetClipboardSourceDirectory()
        {
            if (!string.IsNullOrWhiteSpace(CurrentProjectPath))
            {
                string fullProjectPath = Path.GetFullPath(CurrentProjectPath);

                string? projectDirectory = Path.GetDirectoryName(fullProjectPath);

                if (!string.IsNullOrWhiteSpace(projectDirectory))
                {
                    return projectDirectory;
                }
            }

            return Path.GetFullPath(Environment.CurrentDirectory);
        }


        private static bool TryWriteClipboardPayload(string payload, out string? errorMessage)
        {
            errorMessage = null;
            Exception? lastException = null;

            /*
             * The Windows clipboard may be temporarily locked by
             * another application. A few short retries make copying
             * more reliable.
             */
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try
                {
                    var dataObject = new DataObject();
                    dataObject.SetData(GraphClipboardFormat, payload);
                    Clipboard.SetDataObject(dataObject, copy: true);

                    return true;
                }
                catch (ExternalException exception)
                {
                    lastException = exception;
                    Thread.Sleep(25);
                }
            }

            errorMessage = lastException?.Message ?? "The Windows clipboard is unavailable.";
            return false;
        }


        private static bool TryReadClipboardDocument(out ClipboardGraphDocument? document, out string? errorMessage)
        {
            document = null;

            errorMessage = null;

            try
            {
                if (!Clipboard.ContainsData(GraphClipboardFormat))
                {
                    return false;
                }

                object? clipboardData = Clipboard.GetData(GraphClipboardFormat);

                if (clipboardData is not string payload || string.IsNullOrWhiteSpace(payload))
                {
                    errorMessage = "The clipboard contains an empty SmartExchanger graph fragment.";
                    return false;
                }

                document = JsonSerializer.Deserialize<ClipboardGraphDocument>(payload, GraphJsonOptions.Instance);

                if (document is null)
                {
                    errorMessage = "The clipboard contains an empty SmartExchanger graph document.";
                    return false;
                }
                return true;
            }
            catch (ExternalException exception)
            {
                errorMessage =
                    "The Windows clipboard is currently unavailable." +
                    Environment.NewLine + Environment.NewLine + exception.Message;
                return false;
            }
            catch (JsonException exception)
            {
                errorMessage =
                    "The clipboard contains an invalid " +
                    "SmartExchanger graph document." +
                    Environment.NewLine + Environment.NewLine + exception.Message;
                return false;
            }
        }


        /// <summary>
        /// Removes a collection of nodes and every connection attached
        /// to them. Does not invalidate the graph by itself.
        /// </summary>
        private bool RemoveNodesInternal(IEnumerable<BaseNodeViewModel> nodes)
        {
            List<BaseNodeViewModel> removableNodes = nodes.Where(
                        node =>
                            node is not null && node is not TextureSizeNodeViewModel && Nodes.Contains(node))
                    .Distinct(ReferenceComparer<BaseNodeViewModel>.Instance).ToList();

            if (removableNodes.Count == 0)
            {
                return false;
            }

            var nodeSet = new HashSet<BaseNodeViewModel>(removableNodes, ReferenceComparer<BaseNodeViewModel>.Instance);

            if (_pendingSourceConnector is not null && nodeSet.Contains(_pendingSourceConnector.Node))
            {
                _pendingSourceConnector = null;
            }

            List<ConnectionViewModel> connectionsToRemove =
                Connections .Where(
                        connection =>
                            nodeSet.Contains(connection.Source.Node) ||
                            nodeSet.Contains(connection.Target.Node)).ToList();

            foreach (ConnectionViewModel connection in connectionsToRemove)
            {
                RemoveConnectionInternal(connection);
            }

            foreach (BaseNodeViewModel node in removableNodes)
            {
                SelectedNodes.Remove(node);

                if (node is OutputNodeViewModel outputNode)
                {
                    outputNode.ClearPreview();
                }
                DetachNode(node);
                DisposeNode(node);
                Nodes.Remove(node);
            }

            return true;
        }


        private static bool IsTextInputFocused()
        {
            return Keyboard.FocusedElement is TextBoxBase or PasswordBox;
        }


        private static void ShowPasteWarnings(IReadOnlyCollection<string> warnings)
        {
            if (warnings.Count == 0)
            {
                return;
            }

            string warningText = string.Join(Environment.NewLine + Environment.NewLine,warnings);

            MessageBox.Show(
                "The nodes were pasted, but some data " +
                "could not be restored:" +
                Environment.NewLine +
                Environment.NewLine +
                warningText,
                "Paste completed with warnings",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private sealed record ClipboardPasteResult(IReadOnlyList<BaseNodeViewModel> Nodes, IReadOnlyList<ConnectionViewModel> Connections, IReadOnlyList<string> Warnings);
        private enum ClipboardVisitState
        {
            Visiting,
            Visited
        }
    }
}