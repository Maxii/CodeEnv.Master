// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayersKnowledge.cs
// Wrapper that holds a collection of PlayerKnowledge instances organized by the player.
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
    /// Wrapper that holds a collection of PlayerKnowledge instances organized by the player.
    /// </summary>
    public class PlayersKnowledge : IDisposable {

        private IDictionary<Player, PlayerKnowledge> _playersKnowledgeLookup;

        private Player _userPlayer;

        public PlayersKnowledge(IEnumerable<Player> allPlayers) {
            int playerCount = allPlayers.Count();
            _playersKnowledgeLookup = new Dictionary<Player, PlayerKnowledge>(playerCount);
            allPlayers.ForAll(p => {
                _playersKnowledgeLookup.Add(p, new PlayerKnowledge(p));
                if (p.IsUser) {
                    _userPlayer = p;
                }
            });
        }

        /// <summary>
        /// Initializes the knowledge that all players have at the beginning of the game.
        /// </summary>
        /// <param name="universeCenter">The universe center.</param>
        /// <param name="allStars">All stars.</param>
        public void InitializeAllPlayersStartingKnowledge(IUniverseCenterItem universeCenter, IEnumerable<IStarItem> allStars) {
            D.Warn(universeCenter == null, "UniverseCenter is not activated.");
            _playersKnowledgeLookup.Values.ForAll(pk => {
                pk.AddUniverseCenter(universeCenter);
                allStars.ForAll(s => pk.AddStar(s));
            });
        }

        /// <summary>
        /// Gets the knowledge known to the user.
        /// </summary>
        /// <returns></returns>
        public PlayerKnowledge GetUserKnowledge() {
            return GetKnowledge(_userPlayer);
        }

        /// <summary>
        /// Gets the knowledge known to the provided player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public PlayerKnowledge GetKnowledge(Player player) {
            return _playersKnowledgeLookup[player];
        }

        private void Cleanup() {
            _playersKnowledgeLookup.Values.ForAll(pk => pk.Dispose());
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
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

    }
}

