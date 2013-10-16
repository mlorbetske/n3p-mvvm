using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace N3P.MVVM.Undo
{
    public static class UndoableExtensions
    {
        public static void Undo<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            if (svc != null)
            {
                svc.Undo();
            }
        }

        public static void Redo<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            if (svc != null)
            {
                svc.Redo();
            }
        }

        public static void SuspendAutoUndoStateCapture<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            if (svc != null)
            {
                svc.SuspendAutoStateCapture = true;
            }
        }

        public static void ResumeAutoUndoStateCapture<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            if (svc != null)
            {
                svc.SuspendAutoStateCapture = false;
            }
        }

        public static void MakeVolatile<TModel>(this TModel model)
            where TModel:BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            if (svc != null)
            {
                svc.MakeVolatile();
            }
        }

        public static void ResetUndoState<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            if (svc != null)
            {
                svc.Reset();
            }
        }

        public static bool CanUndo<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            return svc != null && svc.CanUndo;
        }

        public static bool CanRedo<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<UndoHandler>();

            return svc != null && svc.CanRedo;
        }
    }
}
