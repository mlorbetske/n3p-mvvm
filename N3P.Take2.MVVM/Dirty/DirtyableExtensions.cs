using System;

namespace N3P.MVVM.Dirty
{
    public static class DirtyableExtensions
    {
        public static bool GetIsDirty<TModel>(this TModel model)
            where TModel : class, IBindable<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            return svc != null && svc.IsDirty;
        }

        public static bool GetIsDirtyTracked<TModel>(this TModel model)
            where TModel : class, IBindable<TModel>
        {
            return  model.GetService<DirtyableService>() != null;
        }

        public static void Clean<TModel>(this TModel model)
            where TModel : class, IBindable<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                svc.Clean();
            }
        }

        public static void MarkDirty<TModel>(this TModel model)
            where TModel : class, IBindable<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                svc.MarkDirty();
            }
        }

        public static void AddDirtyStateChangedHandler<TModel>(this TModel model, EventHandler handler)
            where TModel : class, IBindable<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                svc.DirtyStateChanged += handler;
            }
        }

        public static void RemoveDirtyStateChangedHandler<TModel>(this TModel model, EventHandler handler)
            where TModel : class, IBindable<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                svc.DirtyStateChanged -= handler;
            }
        }

        public static void SetDirtyState<TModel>(this TModel model, IExportedState exportedState)
            where TModel : class, IBindable<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                if (Equals(svc.CleanVersion, exportedState))
                {
                    model.Clean();
                }
                else
                {
                    model.MarkDirty();
                }
            }
        }
    }
}