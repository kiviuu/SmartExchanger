using CommunityToolkit.Mvvm.Input;
using SmartExchanger.Models;

namespace SmartExchanger.ViewModels
{
    public partial class EditorViewModel
    {
        private void SetupDefaultScene()
        {
            AddNodeInternal(CreateDefaultTextureSizeNode());
        }

        private void AddNodeInternal(BaseNodeViewModel node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Action renderStateHandler = () => OnNodePropertiesChanged(node);

            _nodePropertyHandlers.Add(node, renderStateHandler);
            node.PropsChanged += renderStateHandler;

            TrackNodeForHistory(node);
            Nodes.Add(node);
        }

        private void DetachNode(BaseNodeViewModel node)
        {
            if (_nodePropertyHandlers.Remove(node,out Action? renderStateHandler))
            {
                node.PropsChanged -= renderStateHandler;
            }

            UntrackNodeFromHistory(node);
        }

        private void OnNodePropertiesChanged(BaseNodeViewModel node)
        {
            HandleNodeStateChangedForHistory(node);

            if (IsHistoryReplay)
            {
                return;
            }
            InvalidateGraph(requestGpuPurge: node is TextureSizeNodeViewModel);
        }

        [RelayCommand]
        private void StartConnection(object? parameter)
        {
            if (!_isDisposed)
            {
                _pendingSourceConnector = parameter as ConnectorViewModel;
            }
        }

        [RelayCommand]
        private void CompleteConnection(object? parameter)
        {
            if (_isDisposed)
            {
                _pendingSourceConnector = null;
                return;
            }

            try
            {
                if (parameter is not ValueTuple<object, object> tuple ||
                    tuple.Item2 is not ConnectorViewModel targetConnector)
                {
                    return;
                }

                var sourceConnector = _pendingSourceConnector;
                if (sourceConnector is null || sourceConnector == targetConnector)
                {
                    return;
                }

                if (sourceConnector.Node == targetConnector.Node)
                {
                    return;
                }

                bool isSourceOutput = sourceConnector.Node.Outputs.Contains(sourceConnector);
                bool isTargetInput = targetConnector.Node.Inputs.Contains(targetConnector);
                if (!isSourceOutput || !isTargetInput)
                {
                    return;
                }

                if (WouldCreateCycle(sourceConnector.Node, targetConnector.Node))
                {
                    return;
                }

                var previous = Connections.FirstOrDefault(c => c.Target == targetConnector);

                EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();
                if (previous is not null)
                {
                    RemoveConnectionInternal(previous);
                }

                Connections.Add(new ConnectionViewModel(sourceConnector, targetConnector));
                InvalidateGraph(requestGpuPurge: true);

                CommitGraphMutation(previous is null ? "Create connection" : "Replace connection", historyBefore);
            }
            finally
            {
                _pendingSourceConnector = null;
            }
        }

        [RelayCommand]
        private void DisconnectConnector(object? parameter)
        {
            if (_isDisposed || parameter is not ConnectorViewModel connector)
            {
                return;
            }

            var toRemove = Connections
                .Where(c => c.Source == connector || c.Target == connector)
                .ToList();

            if (toRemove.Count == 0)
            {
                return;
            }

            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();

            foreach (var connection in toRemove)
            {
                RemoveConnectionInternal(connection);
            }

            InvalidateGraph(requestGpuPurge: true);

            CommitGraphMutation(toRemove.Count == 1 ? "Disconnect connector" : $"Disconnect {toRemove.Count} connections", historyBefore);
        }

        [RelayCommand]
        private void RemoveConnection(object? parameter)
        {
            if (_isDisposed || parameter is not ConnectionViewModel connection)
            {
                return;
            }

            if (!Connections.Contains(connection))
            {
                SelectedConnections.Remove(connection);
                return;
            }
            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();

            RemoveConnectionInternal(connection);
            InvalidateGraph(requestGpuPurge: true);
            CommitGraphMutation("Remove connection", historyBefore);
        }

        private void RemoveConnectionInternal(ConnectionViewModel connection)
        {
            Connections.Remove(connection);
            SelectedConnections.Remove(connection);
        }

        private int GetTextureSize()
        {
            return Nodes.OfType<TextureSizeNodeViewModel>().FirstOrDefault()?
                    .SelectedSize ??
                    _renderingOptions.DefaultTextureSize;
        }

        private void UpdateConnectorStates()
        {
            foreach (var node in Nodes)
            {
                foreach (var input in node.Inputs)
                {
                    input.IsConnected = Connections.Any(c => c.Target == input);
                }

                foreach (var output in node.Outputs)
                {
                    output.IsConnected = Connections.Any(c => c.Source == output);
                }
            }
        }

        [RelayCommand]
        private void DeleteSelection()
        {
            if (_isDisposed)
            {
                return;
            }

            List<ConnectionViewModel> selectedConnections = SelectedConnections.ToList();

            List<BaseNodeViewModel> selectedNodes = SelectedNodes.Where(node => node is not TextureSizeNodeViewModel).ToList();

            if (selectedConnections.Count == 0 && selectedNodes.Count == 0)
            {
                return;
            }

            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();
            bool graphChanged = false;

            foreach (ConnectionViewModel connection in selectedConnections)
            {
                if (!Connections.Contains(connection))
                {
                    continue;
                }

                RemoveConnectionInternal(connection);
                graphChanged = true;
            }

            if (RemoveNodesInternal(selectedNodes))
            {
                graphChanged = true;
            }

            SelectedConnections.Clear();
            SelectedNodes.Clear();

            if (!graphChanged)
            {
                return;
            }

            InvalidateGraph(requestGpuPurge: true);

            CommitGraphMutation(
                selectedNodes.Count > 0
                    ? selectedNodes.Count == 1
                        ? "Delete node"
                        : $"Delete {selectedNodes.Count} nodes"
                    : selectedConnections.Count == 1
                        ? "Delete connection"
                        : $"Delete {selectedConnections.Count} connections",
                historyBefore);
        }


        //private Point? _pendingNodeCreationLocation;
        //public void CaptureNodeCreationLocation(Point graphLocation)
        //{
        //    if (!double.IsFinite(graphLocation.X) || !double.IsFinite(graphLocation.Y))
        //    {
        //        return;
        //    }
        //    this._pendingNodeCreationLocation = graphLocation;
        //}


        [RelayCommand]
        private void CreateNode(NodeType nodeType)
        {
            if (_isDisposed)
            {
                return;
            }

            Point? requestedLocation = _graphPointerLocation;
            //this._pendingNodeCreationLocation = null;
            if (requestedLocation is null)
            {
                return;
            }

            // Nodes that may exist only once in the graph
            if (nodeType == NodeType.TextureSizeNode &&
                Nodes.OfType<TextureSizeNodeViewModel>().Any())
            {
                return;
            }
            if (nodeType == NodeType.MaterialOutputNode &&
                Nodes.OfType<MaterialOutputNodeViewModel>().Any())
            {
                return;
            }
            if (nodeType == NodeType.TexturePreviewNode && Nodes.OfType<TexturePreviewNodeViewModel>().Any())
            {
                return;
            }
            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();

            BaseNodeViewModel newNode = nodeFactory.Create(nodeType);

            newNode.Location = requestedLocation.Value;
            AddNodeInternal(newNode);
            SelectedConnections.Clear();
            SelectedNodes.Clear();
            SelectedNodes.Add(newNode);

            InvalidateGraph(requestGpuPurge: false);

            CommitGraphMutation($"Create {newNode.Title}", historyBefore);
        }

        [RelayCommand]
        private void DeleteNode(BaseNodeViewModel node)
        {
            if (_isDisposed || node is null || !Nodes.Contains(node) || node is TextureSizeNodeViewModel)
            {
                return;
            }
            string nodeTitle = node.Title;

            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();
            bool graphChanged = RemoveNodesInternal(new[] { node });

            if (!graphChanged)
            {
                return;
            }
            InvalidateGraph(requestGpuPurge: true);

            CommitGraphMutation($"Delete {nodeTitle}", historyBefore);
        }

        [RelayCommand]
        private void ClearWorkspace()
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to clear the workspace? This process can be undone.",
                "Clear workspace", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes || _isDisposed)
            {
                return;
            }

            EditorHistorySnapshot historyBefore = CaptureBeforeGraphMutation();

            _pendingSourceConnector = null;

            foreach (OutputNodeViewModel outputNode in Nodes.OfType<OutputNodeViewModel>())
            {
                outputNode.ClearPreview();
            }

            foreach (BaseNodeViewModel node in Nodes.ToList())
            {
                DetachNode(node);
                DisposeNode(node);
            }

            Connections.Clear();
            SelectedConnections.Clear();
            SelectedNodes.Clear();
            Nodes.Clear();

            AddNodeInternal(CreateDefaultTextureSizeNode());
            CurrentProjectPath = null;
            InvalidateGraph(requestGpuPurge: true);
            CommitGraphMutation("Clear workspace", historyBefore);
        }

        private TextureSizeNodeViewModel CreateDefaultTextureSizeNode()
        {
            var node = (TextureSizeNodeViewModel)nodeFactory.Create(NodeType.TextureSizeNode);
            node.Location = new Point(0, 0);
            return node;
        }

        private static void DisposeNode(BaseNodeViewModel node)
        {
            if (node is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

}
