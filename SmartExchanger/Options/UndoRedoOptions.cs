namespace SmartExchanger.Options
{
    public sealed class UndoRedoOptions
    {
        public const string SectionName = "UndoRedo";
        public int Capacity { get; set; } = 100;

        public int CommitDelayMilliseconds { get; set; } = 500;
    }
}
