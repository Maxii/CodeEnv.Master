// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LeaderFactory.cs
// Singleton. Factory that makes unique LeaderStat instances from values externally acquired via Xml.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Singleton. Factory that makes unique LeaderStat instances from values externally acquired via Xml.
    /// </summary>
    public class LeaderFactory : AGenericSingleton<LeaderFactory>, IDisposable {

        private IDictionary<Species, IList<LeaderStat>> _leaderStatsCache;
        private IList<LeaderStat> _leaderStatsInUse;
        private LeaderStatXmlReader _xmlReader;

        private LeaderFactory() {
            Initialize();
        }

        protected sealed override void Initialize() {
            _xmlReader = LeaderStatXmlReader.Instance;
            _leaderStatsCache = new Dictionary<Species, IList<LeaderStat>>(SpeciesEqualityComparer.Default);
            _leaderStatsInUse = new List<LeaderStat>();
            Subscribe();
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
        }

        private void Subscribe() {
            GameReferences.GameManager.newGameBuilding += NewGameBuildingEventHandler;
        }

        /// <summary>
        /// Makes a unique instance of a LeaderStat for the specified species.
        /// <remarks></remarks>
        /// </summary>
        /// <param name="species">The species.</param>
        /// <returns></returns>
        public LeaderStat MakeInstance(Species species) {
            IList<LeaderStat> stats;
            if (!_leaderStatsCache.TryGetValue(species, out stats)) {
                stats = _xmlReader.CreateStats(species);
                _leaderStatsCache.Add(species, stats);
            }
            var randomSpeciesLeaderStat = stats.Shuffle().First();
            if (_leaderStatsInUse.Contains(randomSpeciesLeaderStat)) {
                return MakeInstance(species);
            }
            _leaderStatsInUse.Add(randomSpeciesLeaderStat);
            return randomSpeciesLeaderStat;
        }

        public void Reset() {
            //D.Log("{0}.Reset() called.", DebugName);
            _leaderStatsInUse.Clear();
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
            GameReferences.GameManager.newGameBuilding -= NewGameBuildingEventHandler;
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

        private class LeaderStatXmlReader : AXmlReader<LeaderStatXmlReader> {

            private string _speciesTagName = "Species";
            private string _speciesAttributeTagName = "SpeciesName";
            private string _leaderTagName = "Leader";
            private string _leaderNameTagName = "LeaderName";
            private string _leaderImageAtlasIDTagName = "LeaderImageAtlasID";
            private string _leaderImageFilenameTagName = "LeaderImageFilename";

            protected override string XmlFilename { get { return "LeaderValues"; } }

            private LeaderStatXmlReader() {
                Initialize();
            }

            /// <summary>
            /// Creates all the alternative LeaderStats for this species.
            /// </summary>
            /// <param name="species">The species.</param>
            /// <returns></returns>
            internal IList<LeaderStat> CreateStats(Species species) {
                IList<LeaderStat> stats = new List<LeaderStat>();
                var speciesNodes = _xElement.Elements(_speciesTagName);
                foreach (var speciesNode in speciesNodes) {
                    var speciesAttribute = speciesNode.Attribute(_speciesAttributeTagName);
                    string speciesName = speciesAttribute.Value;
                    var speciesFound = Enums<Species>.Parse(speciesName);

                    if (speciesFound == species) {
                        // found the right species node
                        var leaderNodes = speciesNode.Elements(_leaderTagName);
                        foreach (var leaderNode in leaderNodes) {
                            string leaderName = leaderNode.Element(_leaderNameTagName).Value;
                            AtlasID leaderImageAtlasID = Enums<AtlasID>.Parse(leaderNode.Element(_leaderImageAtlasIDTagName).Value);
                            string leaderImageFilename = leaderNode.Element(_leaderImageFilenameTagName).Value;
                            var stat = new LeaderStat(leaderName, leaderImageAtlasID, leaderImageFilename);
                            stats.Add(stat);
                        }
                        break;
                    }
                }
                D.Assert(!stats.IsNullOrEmpty());
                return stats;
            }

            public override string ToString() {
                return new ObjectAnalyzer().ToString(this);
            }

        }

        #endregion

    }
}


