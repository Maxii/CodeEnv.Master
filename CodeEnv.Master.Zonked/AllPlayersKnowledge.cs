// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AllPlayersKnowledge.cs
// Holds the current knowledge base for all players.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Holds the current knowledge base for all players.
/// </summary>
[Obsolete]
public class AllPlayersKnowledge : AGenericSingleton<AllPlayersKnowledge>, IAllPlayersKnowledge, IDisposable {

    private IList<Player> Players { get { return _gameMgr.AllPlayers; } }

    private IDictionary<Player, HashSet<APlanetoidItem>> _planetoidsLookup;
    private IDictionary<Player, HashSet<StarItem>> _starsLookup;
    private IDictionary<Player, HashSet<SystemItem>> _systemsLookup;

    private HashSet<APlanetoidItem> _planetoidsRecorded;
    private GameManager _gameMgr;
    private bool _isInitialized;

    private AllPlayersKnowledge() {
        Initialize();
    }

    protected override void Initialize() {
        _gameMgr = GameManager.Instance;
    }

    public void Add(Player player, ISensorDetectable detectableItem) {
        D.Log("{0} is adding {1} for Player {2}.", GetType().Name, detectableItem.FullName, player.LeaderName);
        if (!_isInitialized) { InitializeCollections(); }

        if (detectableItem is StarItem) {
            Add(player, detectableItem as StarItem);
        }
        else if (detectableItem is APlanetoidItem) {
            Add(player, detectableItem as APlanetoidItem);
        }
        else {
            D.Warn("{0} cannot yet add {1} for Player {2}.", GetType().Name, detectableItem.FullName, player.LeaderName);
        }
    }

    public void Add(Player player, APlanetoidItem planetoid) {
        Arguments.Validate(_isInitialized);
        bool isAdded = _planetoidsLookup[player].Add(planetoid);
        if (!isAdded) {
            D.Warn("{0} tried to add duplicate {1}.", GetType().Name, planetoid.FullName);
            return;
        }
        isAdded = _planetoidsRecorded.Add(planetoid);
        if (isAdded) {
            planetoid.onDeathOneShot += OnPlanetoidDeath;   // subscribe once only no matter player
        }

        D.Assert(planetoid.GetIntelCoverage(player) > IntelCoverage.None);
        _systemsLookup[player].Add(planetoid.System);
        // adding system can fail as it can already be present because of star or other planetoids
    }

    public void Add(Player player, StarItem star) {
        Arguments.Validate(_isInitialized);
        bool isAdded = _starsLookup[player].Add(star);
        if (!isAdded) {
            D.Warn("{0} tried to add duplicate {1}.", GetType().Name, star.FullName);
            return;
        }

        if (star.GetIntelCoverage(player) > IntelCoverage.Basic) {
            _systemsLookup[player].Add(star.System);
            // adding system can fail as it can already be present because of planetoids
        }
    }

    /// <summary>
    /// Returns the Planetoids this player has knowledge of.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public IList<APlanetoidItem> GetPlanetoids(Player player) {
        Arguments.Validate(_isInitialized);
        return _planetoidsLookup[player].ToList();
    }

    /// <summary>
    /// Returns the Stars this player has knowledge of.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public IList<StarItem> GetStars(Player player) {
        Arguments.Validate(_isInitialized);
        return _starsLookup[player].ToList();
    }

    /// <summary>
    /// Returns the Systems this player has knowledge of. A player has knowledge of
    /// a System if they have knowledge of any planetoid in the system or more than
    /// IntelCoverage.Basic knowledge of the System's star.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public IList<SystemItem> GetSystems(Player player) {
        Arguments.Validate(_isInitialized);
        D.Log("{0}.GetSystems() called. Player: {1}.", GetType().Name, player.LeaderName);
        D.Log("{0} System Lookup Keys: {1}.", GetType().Name, _systemsLookup.Keys.Select(k => k.LeaderName).Concatenate());
        return _systemsLookup[player].ToList();
    }

    private void OnPlanetoidDeath(IMortalItem deadItem) {
        var deadPlanetoid = deadItem as APlanetoidItem;
        RemoveDeadPlanetoid(deadPlanetoid);
    }

    /// <summary>
    /// Removes the dead planetoid from the knowledge of all players.
    /// If the deadPlanetoid is the only source of knowledge a player has
    /// about a System, then that System is also removed from that player's
    /// knowledge.
    /// </summary>
    /// <param name="deadPlanetoid">The dead planetoid.</param>
    private void RemoveDeadPlanetoid(APlanetoidItem deadPlanetoid) {
        _planetoidsRecorded.Remove(deadPlanetoid);
        Players.ForAll(player => {
            _planetoidsLookup[player].Remove(deadPlanetoid);
            var system = deadPlanetoid.System;
            if (ShouldSystemBeRemoved(player, system)) {
                _systemsLookup[player].Remove(system);
            }
        });
    }

    /// <summary>
    /// Determines if the System should be removed from the player's knowledge.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="system">The system.</param>
    /// <returns></returns>
    private bool ShouldSystemBeRemoved(Player player, SystemItem system) {
        var remainingPlayerPlanetoidsInSystem = _planetoidsLookup[player].Where(p => p.System == system);
        var playerKnowledgeableStarInSystem = _starsLookup[player].SingleOrDefault(star => star.System == system);
        return playerKnowledgeableStarInSystem == null && !remainingPlayerPlanetoidsInSystem.Any();
    }

    /// <summary>
    /// Lazily initializes the collections so Players are already set.
    /// </summary>
    private void InitializeCollections() {
        //D.Log("{0}.InitializeCollections() called.", GetType().Name);
        _planetoidsRecorded = new HashSet<APlanetoidItem>();
        _planetoidsLookup = new Dictionary<Player, HashSet<APlanetoidItem>>();
        _starsLookup = new Dictionary<Player, HashSet<StarItem>>();
        _systemsLookup = new Dictionary<Player, HashSet<SystemItem>>();
        Players.ForAll(player => {
            _planetoidsLookup.Add(player, new HashSet<APlanetoidItem>());
            _starsLookup.Add(player, new HashSet<StarItem>());
            _systemsLookup.Add(player, new HashSet<SystemItem>());

        });
        _isInitialized = true;
    }

    private void Cleanup() {
        OnDispose();
        // other cleanup here including any tracking Gui2D elements
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


