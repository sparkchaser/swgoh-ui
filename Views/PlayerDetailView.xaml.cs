using goh_ui.Viewmodels;
using System.Windows.Controls;

namespace goh_ui.Views
{
    public partial class PlayerDetailView : ToolWindow
    {
        public PlayerDetailView(PlayerDetailViewmodel vm)
        {
            DataContext = vm;

            InitializeComponent();
        }


        /// <summary> Update a character/ship details view. </summary>
        /// <param name="b">Object in which to place the details view.</param>
        /// <param name="c">Character to create view for, or null to hide view.</param>
        private void ShowDetail(Border b, Character c)
        {
            if (b == null)
                return;

            if (c == null)
            {
                b.Child = null;
            }
            else
            {
                var vm = new CharacterDetailsViewmodel(c);
                var view = new CharacterDetails(vm);
                b.Child = view;
            }
        }

        private void ListView_SelectionChangedChar(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
            {
                ShowDetail(CharDetailsPanel, null);
                return;
            }

            if (!(e.AddedItems[0] is Character c))
                return;

            ShowDetail(CharDetailsPanel, c);
        }

        private void ListView_SelectionChangedShip(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
            {
                ShowDetail(ShipDetailsPanel, null);
                return;
            }

            if (!(e.AddedItems[0] is Character c))
                return;

            ShowDetail(ShipDetailsPanel, c);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                // Clear any detail panes when the selected tab changes
                CharacterTable.SelectedItem = null;
                ShipTable.SelectedItem = null;
            }
        }
    }
}
