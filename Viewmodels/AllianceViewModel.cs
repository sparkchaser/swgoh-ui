using goh_ui.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace goh_ui.Viewmodels
{
    public class AllianceViewModel : DependencyObject
    {
        /// <summary> Interface to use for pulling data, should already be logged in. </summary>
        private readonly ApiWrapper Api;

        public AllianceViewModel(ApiWrapper api)
        {
            Api = api ?? throw new ArgumentNullException("api");

            LookupCommand = new SimpleCommand(DoLookup);
        }

        #region Dependency properties

        /// <summary> Ally codes for one player in each guild to look up. </summary>
        public ObservableCollection<AllyCode> AllyCodes
        {
            get { return (ObservableCollection<AllyCode>)GetValue(AllyCodesProperty); }
            set { SetValue(AllyCodesProperty, value); }
        }
        public static readonly DependencyProperty AllyCodesProperty =
            DependencyProperty.Register("AllyCodes", typeof(ObservableCollection<AllyCode>), typeof(AllianceViewModel),
                new PropertyMetadata(new ObservableCollection<AllyCode>() { }));


        /// <summary> Guild summary info. </summary>
        public ObservableCollection<GuildSummary> Results
        {
            get { return (ObservableCollection<GuildSummary>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(ObservableCollection<GuildSummary>), typeof(AllianceViewModel),
                new PropertyMetadata(new ObservableCollection<GuildSummary>() { }));


        /// <summary> Whether the "fetch" button should be clickable. </summary>
        public bool ButtonsEnabled
        {
            get { return (bool)GetValue(ButtonsEnabledProperty); }
            set { SetValue(ButtonsEnabledProperty, value); }
        }
        public static readonly DependencyProperty ButtonsEnabledProperty =
            DependencyProperty.Register("ButtonsEnabled", typeof(bool), typeof(AllianceViewModel), new PropertyMetadata(true));

        #endregion

        #region Button handlers

        public SimpleCommand LookupCommand { get; private set; }

        /// <summary> Look up guild summary information for the specified ally codes. </summary>
        private async void DoLookup()
        {
            if (!AllyCodes.Any())
            {
                MessageBox.Show("Must supply one or more ally codes.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Pull guild summary data for each ally code
            ButtonsEnabled = false;
            GuildInfo[] retval;
            retval = await Api.GetGuildInfo(AllyCodes);
            if (retval == null)
            {
                ButtonsEnabled = true;
                return;
            }

            // Consolidate information
            var tmpdata = new ObservableCollection<GuildSummary>(retval.Select(x => new GuildSummary() { Name = x.name, Members = x.members, Power = x.gp }));

            // Update UI
            Results = tmpdata;
            ButtonsEnabled = true;
        }

        #endregion
    }


    public class GuildSummary
    {
        public GuildSummary() { }

        public string Name { get; set; }
        public int Members { get; set; }
        public long Power { get; set; }

    }
}
