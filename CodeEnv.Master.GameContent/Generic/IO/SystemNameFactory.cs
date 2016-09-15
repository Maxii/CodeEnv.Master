// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemNameFactory.cs
// Singleton. Factory that delivers unique System names from values externally acquired via Xml.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Factory that delivers unique System names from values externally acquired via Xml.
    /// </summary>
    public class SystemNameFactory : AGenericSingleton<SystemNameFactory>, IDisposable {

        private const string DummyNameFormat = "System_{0}";

        private static int DummyNameCounter = Constants.One;

        private IList<string> _unusedNames;
        private SystemNameXmlReader _xmlReader;

        private SystemNameFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            _xmlReader = SystemNameXmlReader.Instance;
            Subscribe();
            InitializeUnusedNames();
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
        }

        private void Subscribe() {
            References.GameManager.newGameBuilding += NewGameBuildingEventHandler;
        }

        private void InitializeUnusedNames() {
            _unusedNames = _xmlReader.GetAllSystemNames().Shuffle().ToList();
        }

        public string GetUnusedName() {
            string unusedName;
            if (_unusedNames.Any()) {
                int lastNameIndex = _unusedNames.Count - 1;
                unusedName = _unusedNames[lastNameIndex];
                _unusedNames.RemoveAt(lastNameIndex);   // avoids reordering list
            }
            else {
                // not enough names
                unusedName = GetDummyName();
            }
            return unusedName;
        }

        public void MarkNameAsUsed(string usedSystemName) {
            bool isRemoved = _unusedNames.Remove(usedSystemName);
            D.Assert(isRemoved, "{0} did not find SystemName {1} in unused system names.", GetType().Name, usedSystemName);
        }

        public void Reset() {
            InitializeUnusedNames();
            DummyNameCounter = Constants.One;
        }

        private string GetDummyName() {
            string dummyName = DummyNameFormat.Inject(DummyNameCounter);
            DummyNameCounter++;
            return dummyName;
        }

        #region Event and Property Change Handlers

        private void NewGameBuildingEventHandler(object sender, EventArgs e) {
            Reset();
        }

        #endregion

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            References.GameManager.newGameBuilding -= NewGameBuildingEventHandler;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

        #region Nested Classes

        private class SystemNameXmlReader : AXmlReader<SystemNameXmlReader> {

            private string _systemNameTagName = "SystemName";

            protected override string XmlFilename { get { return "SystemNames"; } }

            private SystemNameXmlReader() {
                Initialize();
            }

            /// <summary>
            /// Gets all the potential names for systems from Xml at one time.
            /// </summary>
            /// <returns></returns>
            internal IList<string> GetAllSystemNames() {
                IList<string> names = new List<string>(10);
                var systemNameNodes = _xElement.Elements(_systemNameTagName);
                foreach (var nameNode in systemNameNodes) {
                    string systemName = nameNode.Value;
                    names.Add(systemName);
                }
                D.Assert(!names.IsNullOrEmpty());
                return names;
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion


    }
}


