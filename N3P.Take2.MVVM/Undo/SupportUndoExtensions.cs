using System.Linq;

namespace N3P.Take2.MVVM.Undo
{
    public static class SupportUndoExtensions
    {
        public static void Undo(this ISupportUndo undoable)
        {
            if (undoable.StackPosition > 0)
            {
                var targetVersion = undoable.VersionIdentifierStack.ElementAt(undoable.StackPosition - 1);
                undoable.RevertToVersion(targetVersion);
            }
        }

        public static void Redo(this ISupportUndo undoable)
        {
            var list = undoable.VersionIdentifierStack.ToList();

            if (undoable.StackPosition < list.Count - 2)
            {
                var targetVersion = list[undoable.StackPosition + 1];
                undoable.RevertToVersion(targetVersion);
            }
        }
    }
}