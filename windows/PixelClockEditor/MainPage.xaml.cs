using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PixelClockEditor
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled) return;
            var vm = new MainViewModel();
            vm.Initialize();
            this.DataContext = vm;
        }
    }
}
