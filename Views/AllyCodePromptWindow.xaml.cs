using goh_ui.Models;
using System;
using System.Windows;

namespace goh_ui.Views
{
    /// <summary>
    /// Window to prompt the user to enter an ally code.
    /// </summary>
    public partial class AllyCodePromptWindow : Window
    {
        public AllyCodePromptWindow()
        {
            OkCmd = new SimpleCommand(Ok);
            CancelCmd = new SimpleCommand(Cancel);

            DataContext = this;

            InitializeComponent();
        }

        /// <summary> Ally code provided by the user. </summary>
        public AllyCode Ally = AllyCode.None;

        public SimpleCommand OkCmd { get; private set; }
        public SimpleCommand CancelCmd { get; private set; }

        #region Dependency properties

        public string Code
        {
            get { return (string)GetValue(CodeProperty); }
            set { SetValue(CodeProperty, value); }
        }
        public static readonly DependencyProperty CodeProperty =
            DependencyProperty.Register("Code", typeof(string), typeof(AllyCodePromptWindow), new PropertyMetadata(""));

        #endregion

        private void Ok()
        {
            // Validate code
            AllyCode ac;
            try
            {
                ac = new AllyCode(Code);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Invalid ally code.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Return code to caller
            Ally = ac;
            Close();
        }

        private void Cancel()
        {
            Ally = AllyCode.None;
            Close();
        }
    }
}
