using goh_ui.Models;
using goh_ui.Viewmodels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace goh_ui.Views
{
    public partial class RosterView : ToolWindow
    {
        private RosterViewmodel vm;

        public RosterView(RosterViewmodel vm)
        {
            DataContext = vm ?? throw new ArgumentNullException("vm");
            this.vm = vm;

            InitializeComponent();
        }

        /// <summary> Forward table double-clicks to the VM. </summary>
        private void ListViewItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            // Extract player data
            if (sender is ListViewItem item)
            {
                if (item.Content is Player p)
                {
                    vm.OpenPlayerDetails(this, p);
                }
            }
        }
    }
}
