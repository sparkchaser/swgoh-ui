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
    }
}
