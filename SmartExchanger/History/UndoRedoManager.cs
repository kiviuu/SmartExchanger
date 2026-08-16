namespace SmartExchanger.History
{
    internal sealed class UndoRedoManager
    {
        private readonly LinkedList<IUndoableAction> _undoActions = new();
        private readonly LinkedList<IUndoableAction> _redoActions = new();
        private readonly int _capacity;
        public event EventHandler? StateChanged;
        public bool CanUndo => _undoActions.Count > 0;
        public bool CanRedo => _redoActions.Count > 0;
        public string? NextUndoDescription => this._undoActions.Last?.Value.Description;
        public string? NextRedoDescription => this._redoActions.Last?.Value.Description;

        public UndoRedoManager(int capacity)
        {
            if(capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _capacity = capacity;
        }

        /// <summary>
        /// Add an action whose has already been performed by the editor
        /// </summary>
        public void RecordExecuted(IUndoableAction action)
        {
            ArgumentNullException.ThrowIfNull(action);
            this._redoActions.Clear();
            this._undoActions.AddLast(action);
            while(this._undoActions.Count > this._capacity)
            {
                this._undoActions.RemoveFirst();
            }
            RaiseStateChanged();
        }

        public void ClearRedo()
        {
            if (this._redoActions.Count == 0)
            {
                return;
            }
            this._redoActions.Clear();
            RaiseStateChanged();
        }

        public void Undo()
        {
            if (this._undoActions.Last is null)
            {
                return;
            }

            var action = this._undoActions.Last.Value;
            this._undoActions.RemoveLast();
            try
            {
                action.Undo();
                this._redoActions.AddLast(action);
            }
            catch
            {
                this._undoActions.AddLast(action);
                RaiseStateChanged();
                throw;
            }
            RaiseStateChanged();
        }

        public void Redo()
        {
            if (this._redoActions.Last is null)
            {
                return;
            }
            var action = this._redoActions.Last.Value;
            this._redoActions.RemoveLast();
            try
            {
                action.Redo();
                this._undoActions.AddLast(action);
                while (this._undoActions.Count > this._capacity)
                {
                    this._undoActions.RemoveFirst();
                }
            }
            catch
            {
                this._redoActions.AddLast(action);
                RaiseStateChanged();
                throw;
            }
            RaiseStateChanged();
        }

        public void Clear()
        {
            bool hasAnyEntries = this._undoActions.Count > 0 || this._redoActions.Count > 0;
            this._undoActions.Clear();
            this._redoActions.Clear();
            if (hasAnyEntries)
            {
                RaiseStateChanged();
            }
        }

        private void RaiseStateChanged()
        {
            this.StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
