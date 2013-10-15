namespace N3P.MVVM.Dirty
{
    public static class DirtyableExtensions
    {
        public static bool GetIsDirty<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            return svc != null && svc.IsDirty;
        }

        public static bool GetIsDirtyTracked<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            return  model.GetService<DirtyableService>() != null;
        }

        public static void Clean<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                svc.Clean();
            }
        }

        public static void MarkDirty<TModel>(this TModel model)
            where TModel : BindableBase<TModel>
        {
            var svc = model.GetService<DirtyableService>();

            if (svc != null)
            {
                svc.MarkDirty();
            }
        }
    }
}