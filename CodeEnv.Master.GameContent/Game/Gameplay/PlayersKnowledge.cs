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


    }
}

