using System;
using N3P.MVVM.Dirty;
using N3P.MVVM.WPFTest.ViewModels;

namespace N3P.MVVM.WPFTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly string _origTitle;

        public MainWindow()
        {
            InitializeComponent();
            _origTitle = Title;
            ViewModel = new MainWindowViewModel();
            ViewModel.FinializeInitialization();
            ViewModel.GetService<DirtyableService>().DirtyStateChanged += OnDirtyStateChanged;
        }

        private void OnDirtyStateChanged(object sender, EventArgs eventArgs)
        {
            Title = _origTitle + (ViewModel.GetIsDirty()
                ? "*"
                : "");
        }

        public MainWindowViewModel ViewModel
        {
            get { return DataContext as MainWindowViewModel; }
            set { DataContext = value; }
        }
    }
}
