using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace N3P.MVVM.Undo
{
    public class UndoHandler : IExportStateRestorer, IInitializationCompleteCallback
    {
        private readonly Stack<IExportedState> _undoStack = new Stack<IExportedState>();
        private readonly Stack<IExportedState> _redoStack = new Stack<IExportedState>();
        private readonly IExportStateRestorer _target;
        private bool _operationInProgress;
        private readonly object _sync = new object();

        public UndoHandler(IExportStateRestorer target)
        {
            _target = target;
        }

        public bool CanUndo
        {
            get { return _undoStack.Count > 0; }
        }

        public bool CanRedo
        {
            get { return _redoStack.Count > 0; }
        }

        public Func<IExportedState> CurrentStateRestorer { get; private set; }

        public void Reset()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private static void MakeParentVolatile(IServiceProviderProvider parent)
        {
            var handler = parent.GetService<UndoHandler>();

            if (handler != null)
            {
                handler.MakeVolatileInternal();

                foreach (var p in parent.Parents)
                {
                    MakeParentVolatile(p);
                }
            }
        }

        public int CaptureSuspensionDepth { get; set; }

        private void MakeAllParentsVolatile()
        {
            var provider = _target as IServiceProviderProvider;

            if (provider != null)
            {
                foreach (var parent in provider.Parents)
                {
                    MakeParentVolatile(parent);
                }
            }
        }

        private void MakeVolatileInternal()
        {
            var newState = _target.ExportState();

            if (_undoStack.Count > 0 && newState.Equals(_undoStack.Peek()))
            {
                return;
            }

            CurrentStateRestorer = _target.ExportState;
            _undoStack.Push(newState);
            _redoStack.Clear();
        }

        public void MakeVolatile()
        {
            lock (_sync)
            {
                if (_operationInProgress || !_operationInProgress && CaptureSuspensionDepth != 0)
                {
                    return;
                }

                MakeVolatileInternal();
                MakeAllParentsVolatile();
            }
        }

        public void Undo()
        {
            lock (_sync)
            {
                if (_undoStack.Count == 0)
                {
                    return;
                }

                _operationInProgress = true;
                _redoStack.Push(CurrentStateRestorer());
                var frame = _undoStack.Pop();
                frame.Apply();
                CurrentStateRestorer = () => frame;
                MakeAllParentsVolatile();
                _operationInProgress = false;
            }
        }

        public void Redo()
        {
            lock (_sync)
            {
                if (_redoStack.Count == 0)
                {
                    return;
                }

                _operationInProgress = true;
                _undoStack.Push(CurrentStateRestorer());
                var frame = _redoStack.Pop();
                frame.Apply();
                CurrentStateRestorer = () => frame;
                MakeAllParentsVolatile();
                _operationInProgress = false;
            }
        }

        public Action GetStateRestorer()
        {
            var undo = _undoStack.ToList();
            var redo = _redoStack.ToList();
            var currentCurrent = CurrentStateRestorer;

            return () =>
            {
                CurrentStateRestorer = currentCurrent;
                _undoStack.Clear();

                foreach (var act in undo)
                {
                    _undoStack.Push(act);
                }

                foreach (var act in redo)
                {
                    _redoStack.Push(act);
                }
            };
        }

        public IExportedState ExportState()
        {
            return null;
        }

        internal readonly Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler> CollectionChangeHandlers = new Dictionary<INotifyCollectionChanged, NotifyCollectionChangedEventHandler>();
        
        public void OnInitializationComplete()
        {
            Reset();
        }
    }
}
