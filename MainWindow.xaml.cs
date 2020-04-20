using System.Windows;
using System.Windows.Controls;

namespace goh_ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewmodel vm = null;

        public MainWindow()
        {
            vm = new MainWindowViewmodel(this);
            vm.ExitEvent += new System.EventHandler((x,y) => Close());

            DataContext = vm;
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Switch to manual sizing to prevent window from growing when text box is populated
            SizeToContent = SizeToContent.Manual;
        }

        /// <summary> Push password changes to viewmodel. </summary>
        private void PasswordField_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update viewmodel with password
            if (sender is PasswordBox pb)
                vm.Password = pb.Password;
        }
    }
}
