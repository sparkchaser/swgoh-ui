using goh_ui.Viewmodels;

namespace goh_ui.Views
{
    public partial class SquadFinderView : ToolWindow
    {
        private SquadFinderViewmodel vm;

        public SquadFinderView(SquadFinderViewmodel vm)
        {
            this.vm = vm;
            DataContext = vm;

            PresetClickHandler = new SimpleArgCommand(LoadPreset);

            InitializeComponent();
        }

        public SimpleArgCommand PresetClickHandler { get; private set; }

        private void LoadPreset(object arg)
        {
            if (arg is string name)
                vm.LoadPreset(name);
        }

        /// <summary> Display detailed character information for the specified character. </summary>
        /// <param name="c">Character to display, or null to hide panel.</param>
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

        private void DataGrid_SelectionChanged(object sender, System.EventArgs e)
        {
            if (ResultsGrid.CurrentCell != null || ResultsGrid.CurrentCell.IsValid || ResultsGrid.CurrentCell.Item != null)
            {
                if (ResultsGrid.CurrentCell.Item is SquadLookupResult squad)
                {
                    Character c = null;
                    // Figure out which column is selected
                    switch (ResultsGrid.CurrentColumn.DisplayIndex)
                    {
                        case 2: c = squad.Unit1; break;
                        case 3: c = squad.Unit2; break;
                        case 4: c = squad.Unit3; break;
                        case 5: c = squad.Unit4; break;
                        case 6: c = squad.Unit5; break;
                    }

                    // Display details for currently-selected team member
                    ShowCharacterDetail(c ?? null);
                    return;
                }
            }

            // Otherwise, hide the details panel
            ShowCharacterDetail(null);
        }
    }
}
