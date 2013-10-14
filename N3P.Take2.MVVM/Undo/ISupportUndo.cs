using System;
using System.Collections.Generic;

namespace N3P.Take2.MVVM.Undo
{
    public interface ISupportUndo
    {
        Guid CreateVersion();

        void RevertToVersion(Guid version);

        IEnumerable<Guid> VersionIdentifierStack { get; }

        int StackPosition { get; }
    }
}
