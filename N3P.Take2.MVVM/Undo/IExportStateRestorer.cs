using System;

namespace N3P.MVVM.Undo
{
    public interface IExportStateRestorer
    {
        IExportedState ExportState();
    }

    public interface IExportStateRestorer<in TModel> : IExportStateRestorer
        where TModel : class
    {
        new IExportedState<TModel> ExportState();
    }
}