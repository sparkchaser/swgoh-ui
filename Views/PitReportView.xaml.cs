using goh_ui.Viewmodels;
using System;

namespace goh_ui.Views
{
    public partial class PitReportView : ToolWindow
    {
        public PitReportView(PitReportViewmodel vm)
        {
            DataContext = vm ?? throw new ArgumentNullException("vm");

            InitializeComponent();
        }
    }
}
