using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SmartExchanger.Models;
using SmartExchanger.Persistence;
using System.IO;

namespace SmartExchanger.ViewModels
{
    public partial class EditorViewModel
    {
        private const string ProjectExtension = ".smxgraph";

        private const string ProjectFilter =
            "SmartExchanger graph (*.smxgraph)|*.smxgraph|" +
            "JSON document (*.json)|*.json";

        [ObservableProperty]
        private string? _currentProjectPath;

        [RelayCommand]
        private async Task SaveProject()
        {
            if (_isDisposed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentProjectPath))
            {
                await SaveProjectAs();
                return;
            }

            await SaveProjectToPath(CurrentProjectPath);
        }

        [RelayCommand]
        private async Task SaveProjectAs()
        {
            if (_isDisposed)
            {
                return;
            }

            var fileDialog = new SaveFileDialog
                {
                    Title = "Save SmartExchanger project",
                    FileName = GetSuggestedProjectFileName(),
                    DefaultExt = ProjectExtension,
                    AddExtension = true,
                    OverwritePrompt = true,
                    CheckPathExists = true,
                    Filter = ProjectFilter,
                    FilterIndex = 1
                };

            bool? dialogResult = Application.Current?.MainWindow is Window owner ? fileDialog.ShowDialog(owner) : fileDialog.ShowDialog();

            if (dialogResult != true)
            {
                return;
            }

            string filePath = NormalizeProjectExtension(fileDialog.FileName, fileDialog.FilterIndex);

            await SaveProjectToPath(filePath);
        }

        [RelayCommand]
        private async Task OpenProject()
        {
            if (_isDisposed)
            {
                return;
            }

            var fileDialog = new OpenFileDialog
                {
                    Title = "Open SmartExchanger project",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = false,
                    Filter = ProjectFilter,
                    FilterIndex = 1
                };

            bool? dialogResult = Application.Current?.MainWindow is Window owner ? fileDialog.ShowDialog(owner) : fileDialog.ShowDialog();

            if (dialogResult != true)
            {
                return;
            }

            MessageBoxResult replaceResult =
                MessageBox.Show(
                    "Opening a project will replace " +
                    "the current graph. Continue?",
                    "Open project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (replaceResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                GraphLoadResult loadedGraph = await graphPersistenceService.LoadAsync(fileDialog.FileName);

                ReplaceGraph(loadedGraph);

                CurrentProjectPath = Path.GetFullPath(fileDialog.FileName);

                if (loadedGraph.Warnings.Count > 0)
                {
                    string warningText = string.Join(Environment.NewLine + Environment.NewLine, loadedGraph.Warnings);

                    MessageBox.Show(
                        "The project was opened, but " +
                        "some resources could not be restored:" +
                        Environment.NewLine + Environment.NewLine + warningText,
                        "Project opened with warnings",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open the selected project." +
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "Project loading failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task SaveProjectToPath(string filePath)
        {
            try
            {
                await graphPersistenceService.SaveAsync(filePath, Nodes, Connections);

                CurrentProjectPath = Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not save the project." +
                    Environment.NewLine + Environment.NewLine + ex.Message,
                    "Project saving failure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReplaceGraph(GraphLoadResult loadedGraph)
        {
            ArgumentNullException.ThrowIfNull(loadedGraph);

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
            Nodes.Clear();

            foreach (BaseNodeViewModel node in loadedGraph.Nodes)
            {
                AddNodeInternal(node);
            }

            foreach (ConnectionViewModel connection in loadedGraph.Connections)
            {
                Connections.Add(connection);
            }

            InvalidateGraph(requestGpuPurge: true);
        }

        private string GetSuggestedProjectFileName()
        {
            if (!string.IsNullOrWhiteSpace(CurrentProjectPath))
            {
                return Path.GetFileName(CurrentProjectPath);
            }

            return "material" + ProjectExtension;
        }

        private static string NormalizeProjectExtension(string filePath, int filterIndex)
        {
            string extension = Path.GetExtension(filePath);

            if (filterIndex == 2)
            {
                return string.Equals(extension,".json", StringComparison.OrdinalIgnoreCase) ? filePath
                    : Path.ChangeExtension(filePath, ".json");
            }

            return string.Equals(extension, ProjectExtension, StringComparison.OrdinalIgnoreCase) ? filePath
                : Path.ChangeExtension(filePath, ProjectExtension);
        }
    }
}
