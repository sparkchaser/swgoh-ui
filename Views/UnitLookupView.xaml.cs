using goh_ui.Viewmodels;

namespace goh_ui.Views
{
    public partial class UnitLookupView : ToolWindow
    {
        public UnitLookupView(UnitLookupViewmodel vm)
        {
            DataContext = vm;

            InitializeComponent();
        }
    }
}
