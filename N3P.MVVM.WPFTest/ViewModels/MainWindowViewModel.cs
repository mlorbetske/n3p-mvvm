using System;
using System.Windows.Input;
using N3P.MVVM.ChangeTracking;
using N3P.MVVM.Dirty;
using N3P.MVVM.Undo;
using N3P.MVVM.Initialize;

namespace N3P.MVVM.WPFTest.ViewModels
{
    [NotifyOnChange, Undoable, Dirtyable]
    public class MainWindowViewModel : BindableBase<MainWindowViewModel>
    {
        public MainWindowViewModel()
        {
            ChangeValueCommand = new Command(() =>
            {
                SubModel.Value = Guid.NewGuid().ToString();
            });

            UndoCommand = this.GetUndoCommand();
            RedoCommand = this.GetRedoCommand();
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
        }

        public ICommand ChangeValueCommand { get; private set; }

        public ICommand UndoCommand { get; private set; }

        public ICommand RedoCommand { get; private set; }
    }
}
