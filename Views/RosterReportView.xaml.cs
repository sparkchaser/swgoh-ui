using goh_ui.Viewmodels;
using System;
using System.Windows;

namespace goh_ui.Views
{
    public partial class RosterReportView : ToolWindow
    {
        private RosterReportViewmodel vm;

        public RosterReportView(RosterReportViewmodel vm)
        {
            DataContext = vm ?? throw new ArgumentNullException("vm");
            this.vm = vm;

            InitializeComponent();
        }
    }
}
