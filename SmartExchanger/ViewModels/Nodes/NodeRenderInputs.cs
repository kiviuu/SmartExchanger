using SkiaSharp;

namespace SmartExchanger.ViewModels.Nodes
{
    /// <summary>
    /// Input collection of images inputted in node during one single render.
    /// EditorVM is the owner of inputted images
    /// </summary>
    public sealed class NodeRenderInputs
    {
        private readonly IReadOnlyDictionary<ConnectorViewModel, SKImage> _images;

        internal NodeRenderInputs(IReadOnlyDictionary<ConnectorViewModel, SKImage> images)
        {
            _images = images ?? throw new ArgumentNullException(nameof(images));
        }

        public SKImage? Get(ConnectorViewModel connector)
        {
            ArgumentNullException.ThrowIfNull(connector);
            return _images.TryGetValue(connector, out var image) ? image : null;
        }

        public bool TryGet(ConnectorViewModel connector, out SKImage? image)
        {
            ArgumentNullException.ThrowIfNull(connector);

            if (_images.TryGetValue(connector, out var found))
            {
                image = found;
                return true;
            }

            image = null;
            return false;
        }
    }
}
