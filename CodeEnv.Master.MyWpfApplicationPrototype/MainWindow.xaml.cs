// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MainWindow.xaml.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.MyWpfApplicationPrototype {

    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Windows;
    using CodeEnv.Master.Common.Resources;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        static string[] cultures = { "en-US", "fr-FR", "en-CA", "en-AU" };

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            MyLabel.Content = UIMessages.Hello;
        }

        private void ChgCultureBtn_Click(object sender, RoutedEventArgs e) {
            Random rng = new Random();
            string cultureString = cultures[rng.Next(cultures.Length)];
            CultureInfo newCulture = new CultureInfo(cultureString);

            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            Debug.WriteLine(String.Format("Current culture is now {0}", cultureString));

            MyLabel.Content = UIMessages.Hello;
        }



    }
}
