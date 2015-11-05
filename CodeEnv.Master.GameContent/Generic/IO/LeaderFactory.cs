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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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
        private IList<LeaderStat> _leadersInUse;
        private LeaderStatXmlReader _xmlReader;

        private LeaderFactory() {
            Initialize();
        }

        protected override void Initialize() {
            _xmlReader = LeaderStatXmlReader.Instance;
            _leaderStatsCache = new Dictionary<Species, IList<LeaderStat>>();
            _leadersInUse = new List<LeaderStat>();
            Subscribe();
            // WARNING: Donot use Instance or _instance in here as this is still part of Constructor
        }

        private void Subscribe() {
            References.GameManager.onNewGameBuilding += OnNewGameBuilding;
        }

        public LeaderStat MakeInstance(Species species) {
            IList<LeaderStat> stats;
            if (!_leaderStatsCache.TryGetValue(species, out stats)) {
                stats = _xmlReader.CreateStats(species);
                _leaderStatsCache.Add(species, stats);
            }
            var randomSpeciesLeader = stats.Shuffle().First();
            if (_leadersInUse.Contains(randomSpeciesLeader)) {
                return MakeInstance(species);
            }
            _leadersInUse.Add(randomSpeciesLeader);
            return randomSpeciesLeader;
        }

        public void Reset() {
            _leadersInUse.Clear();
        }

        private void OnNewGameBuilding() {
            Reset();
        }

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            References.GameManager.onNewGameBuilding -= OnNewGameBuilding;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable

        private bool _alreadyDisposed = false;
        protected bool _isDisposing = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (_alreadyDisposed) {
                return;
            }

            _isDisposing = true;
            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            _alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
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


