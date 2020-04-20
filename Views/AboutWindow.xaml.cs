using System.Reflection;
using System.Windows;

namespace goh_ui.Views
{
    public partial class AboutWindow : ToolWindow
    {
        public AboutWindow()
        {
            DataContext = this;

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString(2);

            InitializeComponent();
        }

        /// <summary> Program version number. </summary>
        public string Version { get; private set; }

        /// <summary> Display a modal 'About' window. </summary>
        /// <param name="owner">Window that will own this dialog.</param>
        public static void Display(Window owner) => new AboutWindow() { Owner = owner }.ShowDialog();
    }
}
