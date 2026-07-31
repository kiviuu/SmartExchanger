using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;

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
            Action handler = () => OnNodePropertiesChanged(node);
            _nodePropertyHandlers.Add(node, handler);
            node.PropsChanged += handler;
            Nodes.Add(node);
        }

        private void DetachNode(BaseNodeViewModel node)
        {
            if (_nodePropertyHandlers.Remove(node, out var handler))
            {
                node.PropsChanged -= handler;
            }
        }

        private void OnNodePropertiesChanged(BaseNodeViewModel node)
        {
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
                if (previous is not null)
                {
                    RemoveConnectionInternal(previous);
                }

                Connections.Add(new ConnectionViewModel(sourceConnector, targetConnector));
                InvalidateGraph(requestGpuPurge: true);
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

            foreach (var connection in toRemove)
            {
                RemoveConnectionInternal(connection);
            }

            InvalidateGraph(requestGpuPurge: true);
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

            RemoveConnectionInternal(connection);
            InvalidateGraph(requestGpuPurge: true);
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

            List<BaseNodeViewModel> selectedNodes =SelectedNodes.Where(node => node is not TextureSizeNodeViewModel).ToList();

            bool graphChanged = false;

            /*
             * Remove explicitly selected connections first.
             * Connections attached to selected nodes will be removed
             * by RemoveNodesInternal.
             */
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

            if (graphChanged)
            {
                InvalidateGraph(requestGpuPurge: true);
            }
        }


        private Point? _pendingNodeCreationLocation;
        public void CaptureNodeCreationLocation(Point graphLocation)
        {
            if (!double.IsFinite(graphLocation.X) || !double.IsFinite(graphLocation.Y))
            {
                return;
            }
            this._pendingNodeCreationLocation = graphLocation;
        }


        [RelayCommand]
        private void CreateNode(NodeType nodeType)
        {
            if (_isDisposed)
            {
                return;
            }

            Point? requestedLocation = _pendingNodeCreationLocation;
            this._pendingNodeCreationLocation = null;
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

            BaseNodeViewModel newNode = nodeFactory.Create(nodeType);

            newNode.Location = requestedLocation.Value;
            AddNodeInternal(newNode);
            InvalidateGraph(requestGpuPurge: false);
        }

        [RelayCommand]
        private void DeleteNode(BaseNodeViewModel node)
        {
            if (_isDisposed || node is null || !Nodes.Contains(node) || node is TextureSizeNodeViewModel)
            {
                return;
            }

            bool graphChanged = RemoveNodesInternal(new[] { node });

            if (graphChanged)
            {
                InvalidateGraph(requestGpuPurge: true);
            }
        }

        [RelayCommand]
        private void ClearWorkspace()
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to clear the workspace? This process cannot be undone!",
                "Clear workspace", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (_isDisposed)
            {
                return;
            }
            _pendingSourceConnector = null;
            foreach (var node in Nodes.OfType<OutputNodeViewModel>())
            {
                node.ClearPreview();
            }
            foreach (var node in Nodes.ToList())
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

            InvalidateGraph(true);
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
