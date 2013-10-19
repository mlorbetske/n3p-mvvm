using System;
using System.Windows.Input;
using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Dirty;
using N3P.MVVM.Undo;

namespace N3P.MVVM.WPFTest.ViewModels
{
    [NotifyOnChange, Undoable, Dirtyable]
    public class MainWindowViewModel : BindableBase<MainWindowViewModel>
    {
        public MainWindowViewModel()
        {
            ChangeValueCommand = new Command(() =>
            {
                Value = Guid.NewGuid().ToString();
            });
            UndoCommand = new Command(this.Undo);
        }

        [Initialize.Initialize(initializationParametersStaticPropertyName: "Chickens")]
        public string Value
        {
            get { return Get(x => x.Value); }
            set { Set(x => x.Value, value); }
        }

        public ICommand ChangeValueCommand
        {
            get { return Get(x => ChangeValueCommand); }
            set { Set(x => x.ChangeValueCommand, value); }
        }

        public ICommand UndoCommand
        {
            get { return Get(x => x.UndoCommand); }
            set { Set(x => x.UndoCommand, value); }
        }
    }
}
