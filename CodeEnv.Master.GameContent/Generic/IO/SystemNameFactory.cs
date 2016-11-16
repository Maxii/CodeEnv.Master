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

        /// <summary>
        /// Proper names for Systems, loaded from Xml.
        /// <remarks>Systems can also be assigned programmatically created names when all ProperNames have been used.</remarks>
        /// </summary>
        private IList<string> _allProperNames;
        private IList<string> _unusedProperNames;
        private IDictionary<Species, Stack<string>> _speciesHomeSystemNamesLookup;
        private SystemNameXmlReader _xmlReader;

        private SystemNameFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            InitializeValuesAndReferences();
            Subscribe();
            InitializeProperNamesFromXml();
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
        }

        private void InitializeValuesAndReferences() {
            _xmlReader = SystemNameXmlReader.Instance;
            _speciesHomeSystemNamesLookup = new Dictionary<Species, Stack<string>>();
        }

        private void Subscribe() {
            References.GameManager.newGameBuilding += NewGameBuildingEventHandler;
        }

        private void InitializeProperNamesFromXml() {
            _allProperNames = _xmlReader.LoadAllProperSystemNames();
            _unusedProperNames = _allProperNames.Shuffle().ToList();
        }

        /// <summary>
        /// Returns an unused proper system name if any are left,
        /// otherwise returns a programmatically generated system name.
        /// </summary>
        /// <returns></returns>
        public string GetUnusedName() {
            string unusedName;
            if (_unusedProperNames.Any()) {
                int lastNameIndex = _unusedProperNames.Count - 1;
                unusedName = _unusedProperNames[lastNameIndex];
                _unusedProperNames.RemoveAt(lastNameIndex);   // avoids reordering list
            }
            else {
                // not enough names
                unusedName = GetDummyName();
            }
            return unusedName;
        }

        public string GetUnusedHomeSystemNameFor(Player player) {
            var unusedHomeSystemNames = _xmlReader.LoadHomeSystemNamesFor(player.Species);
            unusedHomeSystemNames = unusedHomeSystemNames.Intersect(_unusedProperNames);
            string homeSystemName = unusedHomeSystemNames.First();
            bool isRemoved = _unusedProperNames.Remove(homeSystemName);
            if (!isRemoved) {
                D.Error("{0} could not validate {1}'s HomeSystemName {2}.", Name, player.Species.GetValueName(), homeSystemName);
            }
            return homeSystemName;
        }

        /// <summary>
        /// Returns <c>true</c> if the provided name is from the list
        /// of predetermined, properly named system names acquired from Xml, <c>false</c> otherwise.
        /// </summary>
        /// <param name="systemName">Name of the system.</param>
        /// <returns>
        ///   <c>true</c> if [is system properly named] [the specified system name]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSystemProperlyNamed(string systemName) {
            if (!Utility.CheckForContent(systemName)) {
                return false;
            }
            return _allProperNames.Contains(systemName);
        }

        /// <summary>
        /// Marks the provided system name as used so it won't be used again.
        /// </summary>
        /// <param name="systemName">Name of the system.</param>
        [Obsolete]
        public void MarkNameAsUsed(string systemName) {
            bool isRemoved = _unusedProperNames.Remove(systemName);
            D.Assert(isRemoved, "{0} did not find SystemName {1} in unused system names.", Name, systemName);
        }

        public void Reset() {
            _speciesHomeSystemNamesLookup.Clear();
            InitializeProperNamesFromXml();
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

            private string _allProperSystemNamesTagName = "AllProperSystemNames";
            private string _systemNameTagName = "SystemName";
            private string _speciesHomeSystemNamesTagName = "SpeciesHomeSystemNames";
            private string _speciesAttributeTagName = "SpeciesName";


            protected override string XmlFilename { get { return "SystemNames"; } }

            private SystemNameXmlReader() {
                Initialize();
            }

            /// <summary>
            /// Loads all the potential proper names for systems from XML at one time.
            /// </summary>
            /// <returns></returns>
            internal IList<string> LoadAllProperSystemNames() {
                var allProperSystemNamesNode = _xElement.Element(_allProperSystemNamesTagName);
                var allProperSystemNameNodes = allProperSystemNamesNode.Elements(_systemNameTagName);
                int nameCount = allProperSystemNameNodes.Count();
                IList<string> names = new List<string>(nameCount);
                foreach (var nameNode in allProperSystemNameNodes) {
                    string systemName = nameNode.Value;
                    names.Add(systemName);
                }
                D.Assert(!names.IsNullOrEmpty());
                return names;
            }

            internal IEnumerable<string> LoadHomeSystemNamesFor(Species species) {
                IList<string> homeSystemNames = null;
                var speciesHomeSystemNameNodes = _xElement.Elements(_speciesHomeSystemNamesTagName);
                foreach (var speciesHomeSystemNameNode in speciesHomeSystemNameNodes) {
                    var speciesAttribute = speciesHomeSystemNameNode.Attribute(_speciesAttributeTagName);
                    string speciesName = speciesAttribute.Value;
                    if (speciesName == species.GetValueName()) {
                        // found the right HomeSystemNames
                        var nameNodes = speciesHomeSystemNameNode.Elements(_systemNameTagName);
                        homeSystemNames = new List<string>(nameNodes.Count());
                        foreach (var nameNode in nameNodes) {
                            string homeSystemName = nameNode.Value;
                            homeSystemNames.Add(homeSystemName);
                        }
                        break;
                    }
                }
                D.AssertNotNull(homeSystemNames);
                return homeSystemNames;
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}


