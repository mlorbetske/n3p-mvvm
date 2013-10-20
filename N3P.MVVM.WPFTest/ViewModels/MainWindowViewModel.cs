using System;
using System.Windows.Input;
using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Dirty;
using N3P.MVVM.Undo;
using N3P.MVVM.Initialize;

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

    [NotifyOnChange, Undoable, Dirtyable]
    public class MainWindowViewModel : BindableBase<MainWindowViewModel>
    {
        public MainWindowViewModel()
        {
            ChangeValueCommand = new Command(() =>
            {
                SubModel.Value = Guid.NewGuid().ToString();
            });
            UndoCommand = new Command(this.Undo);
            RedoCommand = new Command(this.Redo);
        }
        
        public DateTime Chickens
        {
            get { return Get(x => x.Chickens); }
            set { Set(x => x.Chickens, value); }
        }
        
        [Initialize]
        public SubModel SubModel
        {
            get { return Get(x => x.SubModel); }
            set { Set(x => x.SubModel, value); }
        }
	    

        public ICommand ChangeValueCommand
        {
            get { return Get(x => x.ChangeValueCommand); }
            set { Set(x => x.ChangeValueCommand, value); }
        }

        public ICommand UndoCommand
        {
            get { return Get(x => x.UndoCommand); }
            set { Set(x => x.UndoCommand, value); }
        }

        public ICommand RedoCommand
        {
            get { return Get(x => x.RedoCommand); }
            set { Set(x => x.RedoCommand, value); }
        }
    }
}
