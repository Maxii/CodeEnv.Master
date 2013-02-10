// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: App.xaml.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.GameLauncherApp {

    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Threading;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The main keyValuePair point extending the Application class, implementing code behind logic for App.xaml
    /// </summary>
    public partial class App : Application {
        //  The following code infrastructure is created and run to start the application but not shown
        //        public App() { }

        //        [STAThread]  // attribute is required for WPF applications
        //        public static void Main() {
        //            // Create new instance of application subclass
        //            App app = new App();

        //            app.InitializeComponent();

        //          an optional Splash screen can be started here http://msdn.microsoft.com/en-us/library/cc656886.aspx

        //            // Start running the application
        //            app.Run();
        //        }

        //        public void InitializeComponent()  {
        // Code is auto-generated to register events and set properties that were 
        //  defined in XAML in the application definition 
        // ...
        //        }

        /// <summary>
        /// Handles the Startup event signifying the Application is now running.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="gameEvent">The <see cref="StartupEventArgs" /> instance containing the event data.</param>
        private void App_Startup(object sender, StartupEventArgs e) {
            // Process command line args
            // IMPROVE For more robust command line processing approach, see http://msdn.microsoft.com/en-us/library/aa972153(v=vs.90).aspx
#pragma warning disable
            bool godMode = false;
            for (int i = 0; i != e.Args.Length; ++i) {
                if (e.Args[i] == Constants.GodMode) {
                    godMode = true;
                }
            }
#pragma warning restore

            // UNDONE do something with godMode

            // The singleton instance of App or Application can be acquired once the Application.Startup event has been received
            // App instance = (App)Application.Current;
            // Application instance = Application.Current;

            // Create main application window
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        //private void Initialize() {
        //    App app = (App)Application.Current;
        //    string appNamespace = app.GetType().Namespace;
        //    string resourceNamespace = appNamespace + Constants.Period + "Resources";
        //    ResourceMgrFactory.Instance.Initialize(resourceNamespace);
        //}

        /// <summary>
        /// Handles the Application.Activated event signifying either the MainWindow has initially shown or the user
        /// has switched from another application by selecting one of this Application'text Windows. This is not a Window.Activated event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="gameEvent">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void App_Activated(object sender, EventArgs e) {
            // an Application.Activated event has arrived
            // STUB
        }

        /// <summary>
        /// Handles the Application.Deactivated event signifying either the application has shut down or the user
        /// has switched from this application by selecting one of another application'text windows. This is not a Window.Deactivated event.       
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="gameEvent">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void App_Deactivated(object sender, EventArgs e) {
            // an Application.Deactivated event has arrived
            // STUB
        }

        /// <summary>
        /// Handles the Application.SessionEnding event indicating the Windows Session is ending. This is typically initiated by the user
        /// by logging off, shutting down, restarting or hibernating.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="gameEvent">The <see cref="SessionEndingCancelEventArgs" /> instance containing the event data.</param>
        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e) {
            // Ask the user if they want to allow the session to end 
            string msg = string.Format("{0}. End session?", e.ReasonSessionEnding);
            MessageBoxResult result = MessageBox.Show(msg, "Session Ending", MessageBoxButton.YesNo);

            // End session, if specified 
            if (result == MessageBoxResult.No) {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Handles the Application.Exit event indicating the application is shutting down. This is the time to execute any final 
        /// wrap-up work before termination.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="gameEvent">The <see cref="ExitEventArgs" /> instance containing the event data.</param>
        private void App_Exit(object sender, ExitEventArgs e) {
            // UNDONE Persist and store Application-scope propertites to storage?
            // see http://msdn.microsoft.com/en-us/library/ms746696.aspx

            // IMPROVE the default Application Exit Code of 0 can be changed here
        }

        /// <summary>
        /// Handles the Application.DispatcherUnhandledException event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="gameEvent">The <see cref="DispatcherUnhandledExceptionEventArgs" /> instance containing the event data.</param>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            // UNDONE Process unhandled exception

            // Prevent default unhandled exception processing, ala "checking for solution" and "send MS info?" message boxes
            e.Handled = true;
        }
    }
}
