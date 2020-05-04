using goh_ui.Viewmodels;

namespace goh_ui.Views
{
    public partial class UnitLookupView : ToolWindow
    {
        public UnitLookupView(UnitLookupViewmodel vm)
        {
            vm.Parent = this;
            DataContext = vm;

            InitializeComponent();
        }


        public void ShowCharacterDetail(Character c)
        {
            if (c == null)
            {
                DetailsPanel.Child = null;
            }
            else
            {
                var vm = new CharacterDetailsViewmodel(c);
                var view = new CharacterDetails(vm);
                DetailsPanel.Child = view;
            }
        }

        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0)
            {
                ShowCharacterDetail(null);
                return;
            }

            if (!(e.AddedItems[0] is UnitLookupResult c))
                return;

            ShowCharacterDetail(c._char);
        }
    }
}
