namespace SmartExchanger.History
{
    internal interface IUndoableAction
    {
        string Description { get; }
        void Undo();
        void Redo();
    }
}
