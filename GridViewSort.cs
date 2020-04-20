using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

// Attached properties for making it easier to sort a GridView.
// Source: https://thomaslevesque.com/2009/03/27/wpf-automatically-sort-a-gridview-when-a-column-header-is-clicked/

namespace goh_ui
{
    class GridViewSort
    {
        #region Attached properties

        // Set "AutoSort" true to enable click-to-sort functionality (false by default)
        public static bool GetAutoSort(DependencyObject obj) => (bool)obj.GetValue(AutoSortProperty);
        public static void SetAutoSort(DependencyObject obj, bool value) => obj.SetValue(AutoSortProperty, value);
        public static readonly DependencyProperty AutoSortProperty =
            DependencyProperty.RegisterAttached("AutoSort", typeof(bool), typeof(GridViewSort), new UIPropertyMetadata(false, AutoSortPropertyChanged));
        private static void AutoSortPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is ListView listView)
            {
                bool oldValue = (bool)e.OldValue;
                bool newValue = (bool)e.NewValue;
                if (oldValue && !newValue)
                {
                    listView.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                }
                if (!oldValue && newValue)
                {
                    listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(ColumnHeader_Click));
                }
            }
        }

        // "PropertyName" must be set to the name of the data field to use for sorting
        public static string GetPropertyName(DependencyObject obj) => (string)obj.GetValue(PropertyNameProperty);
        public static void SetPropertyName(DependencyObject obj, string value) => obj.SetValue(PropertyNameProperty, value);
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.RegisterAttached("PropertyName", typeof(string), typeof(GridViewSort), new UIPropertyMetadata(null));

        #endregion


        #region Private attached properties

        private static GridViewColumnHeader GetSortedColumnHeader(DependencyObject obj) => (GridViewColumnHeader)obj.GetValue(SortedColumnHeaderProperty);
        private static void SetSortedColumnHeader(DependencyObject obj, GridViewColumnHeader value) => obj.SetValue(SortedColumnHeaderProperty, value);
        private static readonly DependencyProperty SortedColumnHeaderProperty =
            DependencyProperty.RegisterAttached("SortedColumnHeader", typeof(GridViewColumnHeader), typeof(GridViewSort), new UIPropertyMetadata(null));

        #endregion


        #region Column header click event handler

        private static void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                string propertyName = GetPropertyName(headerClicked.Column);
                if (!string.IsNullOrEmpty(propertyName))
                {
                    ListView listView = GetAncestor<ListView>(headerClicked);
                    if (listView != null)
                    {
                        if (GetAutoSort(listView))
                        {
                            ApplySort(listView.Items, propertyName, listView, headerClicked);
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper methods

        public static T GetAncestor<T>(DependencyObject reference) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(reference);
            while (!(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            if (parent != null)
                return (T)parent;
            else
                return null;
        }

        public static void ApplySort(ICollectionView view, string propertyName, ListView listView, GridViewColumnHeader sortedColumnHeader)
        {
            ListSortDirection direction = ListSortDirection.Ascending;
            if (view.SortDescriptions.Count > 0)
            {
                SortDescription currentSort = view.SortDescriptions[0];
                if (currentSort.PropertyName == propertyName)
                {
                    if (currentSort.Direction == ListSortDirection.Ascending)
                        direction = ListSortDirection.Descending;
                    else
                        direction = ListSortDirection.Ascending;
                }
                view.SortDescriptions.Clear();

                GridViewColumnHeader currentSortedColumnHeader = GetSortedColumnHeader(listView);
                if (currentSortedColumnHeader != null)
                {
                    RemoveSortGlyph(currentSortedColumnHeader);
                }
            }
            if (!string.IsNullOrEmpty(propertyName))
            {
                view.SortDescriptions.Add(new SortDescription(propertyName, direction));
                AddSortGlyph(sortedColumnHeader, direction);
                SetSortedColumnHeader(listView, sortedColumnHeader);
            }
        }

        private static void AddSortGlyph(GridViewColumnHeader columnHeader, ListSortDirection direction)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);
            adornerLayer.Add(new SortGlyphAdorner(columnHeader, direction));
        }

        private static void RemoveSortGlyph(GridViewColumnHeader columnHeader)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(columnHeader);
            Adorner[] adorners = adornerLayer.GetAdorners(columnHeader);
            if (adorners != null)
            {
                foreach (Adorner adorner in adorners)
                {
                    if (adorner is SortGlyphAdorner)
                        adornerLayer.Remove(adorner);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple arrow glyph for marking column sort direction.
    /// </summary>
    internal class SortGlyphAdorner : Adorner
    {
        private GridViewColumnHeader _columnHeader;
        private ListSortDirection _direction;

        public SortGlyphAdorner(GridViewColumnHeader columnHeader, ListSortDirection direction)
            : base(columnHeader)
        {
            _columnHeader = columnHeader;
            _direction = direction;
        }

        private Geometry GetDefaultGlyph()
        {
            double x1 = _columnHeader.ActualWidth - 13;
            double x2 = x1 + 10;
            double x3 = x1 + 5;
            double y1 = _columnHeader.ActualHeight / 2 - 3;
            double y2 = y1 + 5;

            if (_direction == ListSortDirection.Ascending)
            {
                double tmp = y1;
                y1 = y2;
                y2 = tmp;
            }

            PathSegmentCollection pathSegmentCollection = new PathSegmentCollection();
            pathSegmentCollection.Add(new LineSegment(new Point(x2, y1), true));
            pathSegmentCollection.Add(new LineSegment(new Point(x3, y2), true));

            PathFigure pathFigure = new PathFigure(
                new Point(x1, y1),
                pathSegmentCollection,
                true);

            PathFigureCollection pathFigureCollection = new PathFigureCollection();
            pathFigureCollection.Add(pathFigure);

            PathGeometry pathGeometry = new PathGeometry(pathFigureCollection);
            return pathGeometry;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawGeometry(Brushes.LightGray, new Pen(Brushes.Gray, 1.0), GetDefaultGlyph());
        }
    }
}
