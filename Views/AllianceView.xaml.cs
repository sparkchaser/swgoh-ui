using goh_ui.Models;
using goh_ui.Viewmodels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace goh_ui.Views
{
    public partial class AllianceView : ToolWindow
    {
        private readonly AllianceViewModel vm;

        public AllianceView(ApiWrapper api)
        {
            vm = new AllianceViewModel(api);
            DataContext = vm;

            AddAlly = new SimpleCommand(AddAllyCode);
            RemoveAlly = new SimpleCommand(RemoveAllyCode);

            InitializeComponent();
        }

        /// <summary> List of ally codes. </summary>
        public List<AllyCode> AllyCodeList
        {
            get => vm.AllyCodes.ToList();
            set => vm.AllyCodes = new ObservableCollection<AllyCode>(value.ToList());
        }

        public SimpleCommand AddAlly { get; private set; }
        public SimpleCommand RemoveAlly { get; private set; }

        /// <summary> Prompt the user to add an ally code to the list. </summary>
        private void AddAllyCode()
        {
            var win = new AllyCodePromptWindow()
            {
                Owner = this
            };

            win.ShowDialog();

            if (win.Ally != AllyCode.None)
            {
                vm.AllyCodes.Add(win.Ally);
            }
        }

        /// <summary> Remove the currently-selected ally code. </summary>
        private void RemoveAllyCode()
        {
            if (AllyList.SelectedItem == null)
                return;

            vm.AllyCodes.Remove((AllyCode)AllyList.SelectedItem);
            AllyList.SelectedItem = null;
        }
    }
}
