using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Dirty;
using N3P.MVVM.Initialize;
using N3P.MVVM.Undo;

namespace N3P.MVVM.WPFTest.ViewModels
{
    [NotifyOnChange, Undoable, Dirtyable]
    public class SubModel : BindableBase<SubModel>
    {
        [Initialize(initializationParametersStaticPropertyName: "Chickens")]
        public string Value
        {
            get { return Get(x => x.Value); }
            set { Set(x => x.Value, value); }
        }
    }
}