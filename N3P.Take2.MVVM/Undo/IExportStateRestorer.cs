using System;

namespace N3P.MVVM.Undo
{
    public interface IExportStateRestorer
    {
        Action GetStateRestorer();
    }
}