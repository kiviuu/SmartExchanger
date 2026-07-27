using System.Text.Json;

namespace SmartExchanger.Services
{
    public interface INodeStateSerializer
    {
        JsonElement CaptureState(BaseNodeViewModel node, string projectDirectory);
        void RestoreState(BaseNodeViewModel node, JsonElement state, string projectDirectory, ICollection<string> warnings);
    }
}
