using goh_ui.Viewmodels;
using System.Windows.Controls;

namespace goh_ui.Views
{
    public partial class CharacterDetails : UserControl
    {
        public CharacterDetails(CharacterDetailsViewmodel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
