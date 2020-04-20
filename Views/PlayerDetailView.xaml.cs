using goh_ui.Viewmodels;

namespace goh_ui.Views
{
    public partial class PlayerDetailView : ToolWindow
    {
        public PlayerDetailView(PlayerDetailViewmodel vm)
        {
            DataContext = vm;

            InitializeComponent();
        }
    }
}
