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

namespace CodeEnv.Master.GameLauncherApp {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Windows;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common.Unity;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private static string[] cultures = { "en-US", "fr-FR", "en-CA", "en-AU" };
        private static Dictionary<string, string> unityProjectPaths = new Dictionary<string, string>(3) {
            {"UnityEntry (default)", UnityConstants.UnityEntryProjectDir},
            {"UnityPOC", UnityDebugConstants.UnityPocProjectDir},
            {"UnityTrials", UnityDebugConstants.UnityTrialsProjectDir}
        };


        public MainWindow() {
            InitializeComponent();
            SetupEventHandlers();
        }

        /// <summary>
        /// Setups the event handlers for the main window.
        /// </summary>
        private void SetupEventHandlers() {
            IEnumerator enumerator = unityProjectPaths.GetEnumerator();
            string selectedProjectPath = UnityConstants.UnityEntryProjectDir;


            // the lambda event handler w/anonomous method
            chgCultureButton.Click += (sender, eventArgs) => {
                System.Random rng = new System.Random();
                //string cultureString = RandomExtended<string>.Choice(cultures);  // Can't use this as RandomExtended.Choice calls UnityEngine.Random from outside Unity
                string cultureString = cultures[rng.Next(cultures.Length)];

                CultureInfo newCulture = new CultureInfo(cultureString);

                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;

                System.Diagnostics.Debug.WriteLine(String.Format("Current culture of thread is now {0}", cultureString));

                greetingsLabel.Content = UIMessages.Hello;
            };

            nextProjectButton.Click += (s, e) => {
                if (!enumerator.MoveNext()) {
                    enumerator.Reset();
                    enumerator.MoveNext();
                }
                KeyValuePair<string, string> keyValuePair = (KeyValuePair<string, string>)enumerator.Current;
                textBlock.Text = keyValuePair.Key as string;
                selectedProjectPath = keyValuePair.Value as string;
            };

            launchUnityButton.Click += (sender, eventArgs) => {
                textBlock.Text = "Launching Unity Project: " + selectedProjectPath;
                /**
                                    *  NOTE: UnityEngine and UnityEditor classes and methods cannot be called from outside of a
                                    *  running Unity instance as they are all attributed as Internal access only. They CAN be referenced,
                                    *  reflected against and used to build a file reference, but not accessed. Doing so causes a runtime 
                                    *  Security exception.
                                    */
                LaunchUnity(selectedProjectPath);
            };
        }

        private void LaunchUnity(string projectPath) {
            System.Diagnostics.Debug.WriteLine(String.Format("Current culture of thread is now {0}", Thread.CurrentThread.CurrentUICulture.DisplayName));

            ProcessStartInfo startInfo = new ProcessStartInfo();
            // startInfo.UseShellExecute = false;  // required to use Environment Variables
            startInfo.FileName = UnityConstants.UnityInstallPath;
            startInfo.Arguments = "-projectPath " + projectPath;   // -batchmode is headless, aka no display
            //startInfo.WorkingDirectory = "";    
            startInfo.CreateNoWindow = true;    // no console flashes
            Process.Start(startInfo);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            greetingsLabel.Content = UIMessages.Hello;
            textBlock.Text = "From Window_Loaded event";
        }
    }
}
