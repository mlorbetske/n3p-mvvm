using System;
using System.Collections.Generic;
using System.Linq;

namespace N3P.MVVM.Undo
{
    public class UndoHandler : IExportStateRestorer
    {
        private readonly Stack<Action> _undoStack = new Stack<Action>();
        private readonly Stack<Action> _redoStack = new Stack<Action>();
        private readonly IExportStateRestorer _target;
        private bool _operationInProgress;

        public UndoHandler(IExportStateRestorer target)
        {
            _target = target;
        }

        public Func<Action> CurrentStateRestorer { get; private set; }

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
            CurrentStateRestorer = _target.GetStateRestorer;
            _undoStack.Push(CurrentStateRestorer());
            _redoStack.Clear();
        }

        public void MakeVolatile()
        {
            if (_operationInProgress)
            {
                return;
            }

            MakeVolatileInternal();
            MakeAllParentsVolatile();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            _operationInProgress = true;
            _redoStack.Push(CurrentStateRestorer());
            var frame = _undoStack.Pop();
            frame();
            CurrentStateRestorer = () => frame;
            MakeAllParentsVolatile();
            _operationInProgress = false;
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            _operationInProgress = true;
            _undoStack.Push(CurrentStateRestorer());
            var frame = _redoStack.Pop();
            frame();
            CurrentStateRestorer = () => frame;
            MakeAllParentsVolatile();
            _operationInProgress = false;
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
    }
}
