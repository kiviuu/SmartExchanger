namespace SmartExchanger.Services
{
    public interface INodeFactory
    {
        BaseNodeViewModel Create(NodeType nodeType);
        BaseNodeViewModel Create(string typeId);
        string GetTypeId(BaseNodeViewModel node);
    }
}