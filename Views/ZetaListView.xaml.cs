using System.Collections.Generic;
using System.Linq;

namespace goh_ui.Views
{
    public partial class ZetaListView : ToolWindow
    {
        public IEnumerable<ZetaStats> Zetas { get; private set; }

        public ZetaListView(IEnumerable<ZetaStats> zetas)
        {
            Zetas = zetas.Where(z => z.versa > 0);
            DataContext = this;
            InitializeComponent();
        }
    }
}
