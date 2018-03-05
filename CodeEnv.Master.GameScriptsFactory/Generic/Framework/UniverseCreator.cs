// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCreator.cs
// New game UniverseCreator handling UCenter, Systems and initial units.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// New game UniverseCreator handling UCenter, Systems and initial units.
/// </summary>
public class UniverseCreator {

    public UniverseCenterItem UniverseCenter { get; private set; }

    public NewGameSystemConfigurator SystemConfigurator { get; private set; }

    public NewGameUnitConfigurator UnitConfigurator { get; private set; }

    private string DebugName { get { return GetType().Name; } }

    private bool ShowDebugLog { get { return _debugCntls.ShowDeploymentDebugLogs; } }

    private IDictionary<DiplomaticRelationship, IList<Player>> _aiPlayerInitialUserRelationsLookup;
    private IDictionary<Player, IntVector3> _playersHomeSectorLookup;
    private List<AUnitCreator> _unitCreators;
    private List<SystemCreator> _systemCreators;
    //private SectorGrid _sectorGrid;   // ref to SectorGrid complicates things as SectorGrid is not persistent
    private GameManager _gameMgr;
    private DebugControls _debugCntls;

    public UniverseCreator() {
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _debugCntls = DebugControls.Instance;
        SystemConfigurator = new NewGameSystemConfigurator();
        UnitConfigurator = new NewGameUnitConfigurator();
    }

    ////public void Initialize() {
    ////    SystemConfigurator = new NewGameSystemConfigurator();
    ////    UnitConfigurator = new NewGameUnitConfigurator();
    ////    InitializeUniverseCenter();
    ////}

    public void InitializeUniverseCenter() {
        UniverseCenter = UniverseFolder.Instance.GetComponentInChildren<UniverseCenterItem>();
        if (UniverseCenter != null) {
            float radius = TempGameValues.UniverseCenterRadius;
            float closeOrbitInnerRadius = radius + 5F;
            UniverseCenterData data = new UniverseCenterData(UniverseCenter, radius, closeOrbitInnerRadius) {
                // Name assignment must follow after Data assigned to Item so Item is subscribed to the change
            };
            FocusableItemCameraStat cameraStat = __MakeUCenterCameraStat(radius, closeOrbitInnerRadius);
            UniverseCenter.CameraStat = cameraStat;
            UniverseCenter.Data = data;
            UniverseCenter.Name = "UniverseCenter";
            // UC will be enabled when CommenceOperations() called
        }
    }

    public void BuildSectors() {
        SectorGrid.Instance.BuildSectors();
    }

    public void DeployAndConfigureSystemCreators() {
        GameSettings gameSettings = _gameMgr.GameSettings;

        if (gameSettings.__UseDebugCreatorsOnly) {
            _systemCreators = __ConfigureExistingDebugCreatorsOnly_System();
            return;
        }

        var sectorGrid = SectorGrid.Instance;
        var deployableSectorIDs = sectorGrid.NonPeripheralSectorIDs;
        int systemQty = CalcUniverseSystemsQty(deployableSectorIDs.Count());

        // Deploy intended Home SystemCreators first

        // IMPROVE existing UnitDebugCreator sectors could be included in occupiedSectors if I want them to take priority.
        // However, this would potentially impede the deployment of a reqd system?
        List<IntVector3> occupiedSectorIDs = new List<IntVector3>();
        var playersHomeCreatorLookup = SystemConfigurator.DeployAndConfigurePlayersHomeSystemCreators(gameSettings, occupiedSectorIDs);

        _playersHomeSectorLookup = new Dictionary<Player, IntVector3>(playersHomeCreatorLookup.Count);
        _systemCreators = new List<SystemCreator>(systemQty);
        foreach (var player in playersHomeCreatorLookup.Keys) {
            var homeCreator = playersHomeCreatorLookup[player];
            _playersHomeSectorLookup.Add(player, homeCreator.SectorID);
            _systemCreators.Add(homeCreator);
            occupiedSectorIDs.Add(homeCreator.SectorID);
        }

        // Now deploy any additional SystemCreators that need to be around each intended Home SystemCreator
        var userPlayer = gameSettings.UserPlayer;
        var startLevel = gameSettings.UserStartLevel;
        var homeSectorID = _playersHomeSectorLookup[userPlayer];    // UNCLEAR rqmt for occupied sectors here?
        var deployedSystemCreators = SystemConfigurator.DeployAndConfigureAdditionalCreatorsAround(homeSectorID, startLevel);
        _systemCreators.AddRange(deployedSystemCreators);
        occupiedSectorIDs.AddRange(deployedSystemCreators.Select(c => c.SectorID));

        var aiPlayers = gameSettings.AIPlayers;
        for (int i = 0; i < aiPlayers.Length; i++) {
            var aiPlayer = aiPlayers[i];
            startLevel = gameSettings.AIPlayersStartLevels[i];
            homeSectorID = _playersHomeSectorLookup[aiPlayer];
            deployedSystemCreators = SystemConfigurator.DeployAndConfigureAdditionalCreatorsAround(homeSectorID, startLevel);
            _systemCreators.AddRange(deployedSystemCreators);
            occupiedSectorIDs.AddRange(deployedSystemCreators.Select(c => c.SectorID));
        }

        int remainingSystemQty = systemQty - _systemCreators.Count;
        D.Assert(remainingSystemQty >= 0);

        // Now deploy any DebugSystemCreators taking into account sectors already occupied
        deployedSystemCreators = SystemConfigurator.ConfigureExistingDebugCreators(remainingSystemQty, occupiedSectorIDs).Cast<SystemCreator>();
        _systemCreators.AddRange(deployedSystemCreators);
        occupiedSectorIDs.AddRange(deployedSystemCreators.Select(c => c.SectorID));
        //D.Log(ShowDebugLog, "{0} _systemCreators contents: {1}.", DebugName, _systemCreators.Select(c => c.DebugName).Concatenate());
        remainingSystemQty = systemQty - _systemCreators.Count;
        D.Assert(remainingSystemQty >= 0);
        if (remainingSystemQty == 0) {
            return; // no more creators to deploy
        }

        // Find and destroy any existing DebugStarbaseCreators that reside in
        var sectorIDsOccupiedByStarbaseCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugStarbaseCreator>().Select(sbc => sbc.SectorID);
        var unoccupiedSectorIDs = deployableSectorIDs.Except(occupiedSectorIDs).Except(sectorIDsOccupiedByStarbaseCreators);

        unoccupiedSectorIDs = unoccupiedSectorIDs.Shuffle();
        var sectorIDsToDeployTo = unoccupiedSectorIDs.Take(remainingSystemQty);
        var sectorPositionsToDeployTo = sectorIDsToDeployTo.Select(index => sectorGrid.GetSectorPosition(index));
        sectorPositionsToDeployTo.ForAll(position => {
            var creator = SystemConfigurator.DeployAndConfigureRandomSystemCreatorTo(position);
            _systemCreators.Add(creator);
        });

        int randomSystemQtyDeployed = sectorIDsToDeployTo.Count();
        D.Log(ShowDebugLog, "{0} deployed and configured {1} additional random {2}s.", DebugName, randomSystemQtyDeployed, typeof(SystemCreator).Name);
        int systemQtyNotDeployed = remainingSystemQty - randomSystemQtyDeployed;
        if (systemQtyNotDeployed > 0) {
            D.Warn("{0} ran out of sectors to deploy {1} remaining systems.", DebugName, systemQtyNotDeployed);
        }
    }

    /// <summary>
    /// Initiates BuildAndDeploy in all SystemCreators.
    /// </summary>
    public void BuildSystems() {
        _systemCreators.ForAll(sc => {
            //D.Log(ShowDebugLog, "{0} is about to call {1}.BuildAndDeploySystem().", DebugName, sc.DebugName);
            sc.BuildAndDeploySystem();
        });
    }

    /// <summary>
    /// Deploys and configures all unit creators that should be placed before runtime begins.
    /// <remarks>Handles __UseDebugCreatorsOnly, __DeployAdditionalAICreators and __DeployAdditionalAICreators from GameSettings.</remarks>
    /// </summary>
    public void DeployAndConfigureInitialUnitCreators() {
        ADebugUnitCreator[] existingDebugCreators = UniverseFolder.Instance.GetComponentsInChildren<ADebugUnitCreator>();
        _aiPlayerInitialUserRelationsLookup = AssignAllPlayerInitialRelationships(existingDebugCreators);
        _unitCreators = new List<AUnitCreator>();

        IEnumerable<ADebugUnitCreator> fleetDebugCreators = existingDebugCreators.Where(c => c is DebugFleetCreator);
        IEnumerable<ADebugUnitCreator> starbaseDebugCreators = existingDebugCreators.Where(c => c is DebugStarbaseCreator);
        IEnumerable<ADebugUnitCreator> settlementDebugCreators = existingDebugCreators.Where(c => c is DebugSettlementCreator);

        UnitConfigurator.CreateAndRegisterRequiredDesigns();

        GameSettings gameSettings = _gameMgr.GameSettings;
        if (gameSettings.__UseDebugCreatorsOnly) {
            var deployedCreators = __ConfigureExistingDebugCreatorsOnly_Fleet(fleetDebugCreators);
            _unitCreators.AddRange(deployedCreators);
            deployedCreators = __ConfigureExistingDebugCreatorsOnly_Starbase(starbaseDebugCreators);
            _unitCreators.AddRange(deployedCreators);
            deployedCreators = __ConfigureExistingDebugCreatorsOnly_Settlement(settlementDebugCreators);
            _unitCreators.AddRange(deployedCreators);
            return;
        }

        // Deploy HomeSystem Settlements for all players
        HashSet<ISystem> usedSystems = new HashSet<ISystem>();
        var userPlayer = gameSettings.UserPlayer;
        var userPlayerStartLevel = gameSettings.UserStartLevel;
        int settlementQty = userPlayerStartLevel.SettlementStartQty();
        if (settlementQty > Constants.Zero) {
            var deployedUserHomeSettlementCreator = DeployAndConfigureHomeSettlementCreator(userPlayer, settlementDebugCreators, ref usedSystems);
            _unitCreators.Add(deployedUserHomeSettlementCreator);
            var deployedUserHomeSettlementDebugCreator = deployedUserHomeSettlementCreator as ADebugUnitCreator;
            if (deployedUserHomeSettlementDebugCreator != null) {
                settlementDebugCreators = settlementDebugCreators.Except(deployedUserHomeSettlementDebugCreator);
            }
        }

        var aiPlayers = gameSettings.AIPlayers;
        int aiPlayerQty = aiPlayers.Length;
        for (int i = 0; i < aiPlayerQty; i++) {
            var aiPlayer = aiPlayers[i];
            var aiPlayerStartLevel = gameSettings.AIPlayersStartLevels[i];

            settlementQty = aiPlayerStartLevel.SettlementStartQty();
            if (settlementQty > Constants.Zero) {
                var deployedAiHomeSettlementCreator = DeployAndConfigureHomeSettlementCreator(aiPlayer, settlementDebugCreators, ref usedSystems);
                _unitCreators.Add(deployedAiHomeSettlementCreator);
                var deployedAiHomeSettlementDebugCreator = deployedAiHomeSettlementCreator as ADebugUnitCreator;
                if (deployedAiHomeSettlementDebugCreator != null) {
                    settlementDebugCreators = settlementDebugCreators.Except(deployedAiHomeSettlementDebugCreator);
                }
            }
        }

        // Handle the user's fleets, starbases and additional settlements ...
        int fleetQty = userPlayerStartLevel.FleetStartQty();
        var fleetCreatorsDeployed = DeployAndConfigureStartLevelFleetCreators(userPlayer, fleetDebugCreators, fleetQty);
        _unitCreators.AddRange(fleetCreatorsDeployed);
        var fleetDebugCreatorsDeployed = fleetCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
        fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

        int starbaseQty = userPlayerStartLevel.StarbaseStartQty();
        var starbaseCreatorsDeployed = DeployAndConfigureStartLevelStarbaseCreators(userPlayer, starbaseDebugCreators, starbaseQty);
        _unitCreators.AddRange(starbaseCreatorsDeployed);
        var starbaseDebugCreatorsDeployed = starbaseCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
        starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

        settlementQty = userPlayerStartLevel.SettlementStartQty() - Constants.One;  // Home Settlement already deployed
        var settlementCreatorsDeployed = DeployAndConfigureNonHomeStartLevelSettlementCreators(userPlayer, settlementDebugCreators, settlementQty, ref usedSystems);
        _unitCreators.AddRange(settlementCreatorsDeployed);
        var settlementDebugCreatorsDeployed = settlementCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
        settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);

        // ... including any additional User creators to deploy
        if (gameSettings.__DeployAdditionalUserCreators) {
            int additionalDeployedCreatorCount = Constants.Zero;

            fleetQty = gameSettings.__AdditionalFleetCreatorQty;
            fleetCreatorsDeployed = DeployAndConfigureAdditionalFleetCreators(userPlayer, fleetDebugCreators, fleetQty);
            additionalDeployedCreatorCount += fleetCreatorsDeployed.Count;
            _unitCreators.AddRange(fleetCreatorsDeployed);
            fleetDebugCreatorsDeployed = fleetCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

            starbaseQty = gameSettings.__AdditionalStarbaseCreatorQty;
            starbaseCreatorsDeployed = DeployAndConfigureAdditionalStarbaseCreators(userPlayer, starbaseDebugCreators, starbaseQty);
            additionalDeployedCreatorCount += starbaseCreatorsDeployed.Count;
            _unitCreators.AddRange(starbaseCreatorsDeployed);
            starbaseDebugCreatorsDeployed = starbaseCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

            settlementQty = gameSettings.__AdditionalSettlementCreatorQty;
            settlementCreatorsDeployed = DeployAndConfigureAdditionalSettlementCreators(userPlayer, settlementDebugCreators, settlementQty, ref usedSystems);
            additionalDeployedCreatorCount += settlementCreatorsDeployed.Count;
            _unitCreators.AddRange(settlementCreatorsDeployed);
            settlementDebugCreatorsDeployed = settlementCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);
            D.Log(/*ShowDebugLog,*/ "{0} deployed an additional {1} User unit creators.", DebugName, additionalDeployedCreatorCount);
        }

        // Handle the AIPlayer's fleets, starbases and additional settlements ...
        for (int i = 0; i < aiPlayerQty; i++) {
            var aiPlayer = aiPlayers[i];
            var aiPlayerStartLevel = gameSettings.AIPlayersStartLevels[i];

            fleetQty = aiPlayerStartLevel.FleetStartQty();
            fleetCreatorsDeployed = DeployAndConfigureStartLevelFleetCreators(aiPlayer, fleetDebugCreators, fleetQty);
            _unitCreators.AddRange(fleetCreatorsDeployed);
            fleetDebugCreatorsDeployed = fleetCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

            starbaseQty = aiPlayerStartLevel.StarbaseStartQty();
            starbaseCreatorsDeployed = DeployAndConfigureStartLevelStarbaseCreators(aiPlayer, starbaseDebugCreators, starbaseQty);
            _unitCreators.AddRange(starbaseCreatorsDeployed);
            starbaseDebugCreatorsDeployed = starbaseCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

            settlementQty = aiPlayerStartLevel.SettlementStartQty() - Constants.One;  // Home Settlement already deployed
            settlementCreatorsDeployed = DeployAndConfigureNonHomeStartLevelSettlementCreators(aiPlayer, settlementDebugCreators, settlementQty, ref usedSystems);
            _unitCreators.AddRange(settlementCreatorsDeployed);
            settlementDebugCreatorsDeployed = settlementCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);
        }

        // ... including any additional AIPlayer creators to deploy
        if (gameSettings.__DeployAdditionalAICreators) {
            int additionalDeployedCreatorCount = Constants.Zero;
            for (int i = 0; i < aiPlayerQty; i++) {
                var aiPlayer = aiPlayers[i];

                fleetQty = gameSettings.__AdditionalFleetCreatorQty;
                fleetCreatorsDeployed = DeployAndConfigureAdditionalFleetCreators(aiPlayer, fleetDebugCreators, fleetQty);
                additionalDeployedCreatorCount += fleetCreatorsDeployed.Count;
                _unitCreators.AddRange(fleetCreatorsDeployed);
                fleetDebugCreatorsDeployed = fleetCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
                fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

                starbaseQty = gameSettings.__AdditionalStarbaseCreatorQty;
                starbaseCreatorsDeployed = DeployAndConfigureAdditionalStarbaseCreators(aiPlayer, starbaseDebugCreators, starbaseQty);
                additionalDeployedCreatorCount += starbaseCreatorsDeployed.Count;
                _unitCreators.AddRange(starbaseCreatorsDeployed);
                starbaseDebugCreatorsDeployed = starbaseCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
                starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

                settlementQty = gameSettings.__AdditionalSettlementCreatorQty;
                settlementCreatorsDeployed = DeployAndConfigureAdditionalSettlementCreators(aiPlayer, settlementDebugCreators, settlementQty, ref usedSystems);
                additionalDeployedCreatorCount += settlementCreatorsDeployed.Count;
                _unitCreators.AddRange(settlementCreatorsDeployed);
                settlementDebugCreatorsDeployed = settlementCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
                settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);
            }
            D.Log(/*ShowDebugLog,*/ "{0} deployed an additional {1} AI unit creators.", DebugName, additionalDeployedCreatorCount);
        }

        List<ADebugUnitCreator> debugCreatorsToDestroy = new List<ADebugUnitCreator>();
        debugCreatorsToDestroy.AddRange(fleetDebugCreators);    // those that are left over, if any
        debugCreatorsToDestroy.AddRange(starbaseDebugCreators);
        debugCreatorsToDestroy.AddRange(settlementDebugCreators);
        //D.Log(ShowDebugLog, "{0} is about to destroy {1} excess debug unit creators.", DebugName, debugCreatorsToDestroy.Count);
        debugCreatorsToDestroy.ForAll(c => {
            //D.Log(ShowDebugLog, "{0} is about to destroy excess {1}.", DebugName, c.DebugName);
            GameUtility.Destroy(c.gameObject);
        });
    }

    /// <summary>
    /// Assigns all player's initial relationships returning a lookup table of initial relationships of all AIPlayers with the User.
    /// </summary>
    /// <param name="existingDebugUnitCreators">The existing debug unit creators.</param>
    /// <returns></returns>
    private IDictionary<DiplomaticRelationship, IList<Player>> AssignAllPlayerInitialRelationships(ADebugUnitCreator[] existingDebugUnitCreators) {
        Player userPlayer = _gameMgr.UserPlayer;
        IList<Player> aiPlayers = _gameMgr.AIPlayers;
        int aiPlayerQty = aiPlayers.Count;
        Dictionary<DiplomaticRelationship, IList<Player>> aiPlayerInitialUserRelationsLookup =
            new Dictionary<DiplomaticRelationship, IList<Player>>(aiPlayerQty, DiplomaticRelationshipEqualityComparer.Default);

        if (_debugCntls.FleetsAutoAttackAsDefault) {
            // Setup initial AIPlayer <-> User relationships as War and record in lookup...
            IList<Player> aiPlayersAtWarWithUser = new List<Player>(aiPlayerQty);
            aiPlayerInitialUserRelationsLookup.Add(DiplomaticRelationship.War, aiPlayersAtWarWithUser);
            foreach (var aiPlayer in aiPlayers) {
                userPlayer.SetInitialRelationship(aiPlayer, DiplomaticRelationship.War);
                aiPlayersAtWarWithUser.Add(aiPlayer);
            }
            // ... then set initial AIPlayer <-> AIPlayer relationship to War
            for (int j = 0; j < aiPlayerQty; j++) {
                for (int k = j + 1; k < aiPlayerQty; k++) {
                    Player jAiPlayer = aiPlayers[j];
                    Player kAiPlayer = aiPlayers[k];
                    jAiPlayer.SetInitialRelationship(kAiPlayer, DiplomaticRelationship.War);    // Will auto handle both assignments
                }
            }
            return aiPlayerInitialUserRelationsLookup;
        }

        var aiOwnedDebugUnitCreators = existingDebugUnitCreators.Where(uc => !uc.EditorSettings.IsOwnerUser);
        var desiredAiUserRelationships = aiOwnedDebugUnitCreators.Select(uc => uc.EditorSettings.DesiredRelationshipWithUser.Convert());

        HashSet<DiplomaticRelationship> uniqueDesiredAiUserRelationships =
            new HashSet<DiplomaticRelationship>(desiredAiUserRelationships, DiplomaticRelationshipEqualityComparer.Default);
        //D.Log(ShowDebugLog, "{0}: Unique desired AI/User Relationships = {1}.", DebugName, uniqueDesiredAiUserRelationships.Select(r => r.GetValueName()).Concatenate());

        // Setup initial AIPlayer <-> User relationships derived from editorCreators..
        Stack<Player> unassignedAIPlayers = new Stack<Player>(aiPlayers);
        uniqueDesiredAiUserRelationships.ForAll(aiUserRelationship => {
            if (unassignedAIPlayers.Count > Constants.Zero) {
                var aiPlayer = unassignedAIPlayers.Pop();
                //D.Log(ShowDebugLog, "{0} about to set {1}'s user relationship to {2}.", DebugName, aiPlayer, aiUserRelationship.GetValueName());
                userPlayer.SetInitialRelationship(aiPlayer, aiUserRelationship);  // will auto handle both assignments
                aiPlayerInitialUserRelationsLookup.Add(aiUserRelationship, new List<Player>() { aiPlayer });
            }
        });
        // ..then assign any aiPlayers that have not been assigned an initial user relationship to Neutral
        if (unassignedAIPlayers.Count > Constants.Zero) {
            IList<Player> neutralAiPlayers;
            if (!aiPlayerInitialUserRelationsLookup.TryGetValue(DiplomaticRelationship.Neutral, out neutralAiPlayers)) {
                neutralAiPlayers = new List<Player>(unassignedAIPlayers.Count);
                aiPlayerInitialUserRelationsLookup.Add(DiplomaticRelationship.Neutral, neutralAiPlayers);
            }
            unassignedAIPlayers.ForAll(aiPlayer => {
                //D.Log(ShowDebugLog, "{0} about to set {1}'s user relationship to {2}.", DebugName, aiPlayer, DiplomaticRelationship.Neutral.GetValueName());
                userPlayer.SetInitialRelationship(aiPlayer); // Neutral, will auto handle both assignments
                neutralAiPlayers.Add(aiPlayer);
            });
        }

        // Set initial AIPlayer <-> AIPlayer relationships to Neutral
        for (int j = 0; j < aiPlayerQty; j++) {
            for (int k = j + 1; k < aiPlayerQty; k++) {
                Player jAiPlayer = aiPlayers[j];
                Player kAiPlayer = aiPlayers[k];
                jAiPlayer.SetInitialRelationship(kAiPlayer);    // Neutral, will auto handle both assignments
            }
        }

        return aiPlayerInitialUserRelationsLookup;
    }

    /// <summary>
    /// Deploys and configures the starbase creators specified by the start level for this player around the player's home sector. 
    /// Returns all the Creators that were deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingStarbaseCreators">The existing starbase creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureStartLevelStarbaseCreators(Player player, IEnumerable<ADebugUnitCreator> existingStarbaseCreators, int qtyToDeploy) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {
            SectorGrid sectorGrid = SectorGrid.Instance;
            var gameKnowledge = _gameMgr.GameKnowledge;
            IList<IntVector3> candidateSectorIDs = new List<IntVector3>();
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), player.DebugName);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringSectorIDs(homeSectorID, includePeriphery: false);
            foreach (var neighborSectorID in homeNeighborSectorIDs) {
                ISystem unusedSystem;
                if (!gameKnowledge.TryGetSystem(neighborSectorID, out unusedSystem)) {
                    candidateSectorIDs.Add(neighborSectorID);
                }
            }
            if (candidateSectorIDs.Count < qtyToDeploy) {
                D.Warn(@"{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Starbases.
                    Fixing by expanding criteria to include sectors with systems.", DebugName, candidateSectorIDs.Count, qtyToDeploy, player);
                candidateSectorIDs = homeNeighborSectorIDs.ToList();
                if (candidateSectorIDs.Count < qtyToDeploy) {
                    D.Error("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Starbases.", DebugName, candidateSectorIDs.Count, qtyToDeploy, player);
                    // Unlikely to occur, but if it does, I'll need to find other sectors to add
                }
            }

            GameDate deployDate = GameTime.GameStartDate;   // all startLevel creators always deploy on GameStartDate

            bool toDeployAutoCreators = true;
            IntVector3 deployedSectorID;
            Vector3 deployedLocation;
            foreach (var debugCreator in existingStarbaseCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    deployedSectorID = RandomExtended.Choice(candidateSectorIDs);
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugStarbaseCreator, player, deployedLocation, deployDate);
                    candidateSectorIDs.Remove(deployedSectorID);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    deployedSectorID = RandomExtended.Choice(candidateSectorIDs);
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    AutoStarbaseCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoStarbaseCreator(player, deployedLocation, deployDate);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoStarbaseCreator(player, deployedLocation, deployDate);
                    }
                    creatorsDeployed.Add(autoCreator);
                    candidateSectorIDs.Remove(deployedSectorID);
                }
            }
        }

        //__ReportDeployedUnitCreators(typeof(AutoStarbaseCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    /// <summary>
    /// Deploys and configures the fleet creators specified by the start level for this player. Returns all of the Creators that were deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingFleetCreators">The existing fleet creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureStartLevelFleetCreators(Player player, IEnumerable<ADebugUnitCreator> existingFleetCreators, int qtyToDeploy) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {

            SectorGrid sectorGrid = SectorGrid.Instance;
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<IntVector3> sectorIDsToDeployTo = new Stack<IntVector3>(qtyToDeploy);
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), player.DebugName);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            ISystem unused;
            if (!gameKnowledge.TryGetSystem(homeSectorID, out unused)) {
                D.Error("{0} couldn't find a system in {1}'s home sector.", DebugName, player);
            }

            sectorIDsToDeployTo.Push(homeSectorID); // if start with fleet(s), always deploy one in the home sector
            if (qtyToDeploy > Constants.One) {
                // there are additional fleets to deploy around the home system
                IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringSectorIDs(homeSectorID, includePeriphery: false);
                foreach (var neighborSectorID in homeNeighborSectorIDs) {
                    if (!gameKnowledge.TryGetSystem(neighborSectorID, out unused)) {
                        sectorIDsToDeployTo.Push(neighborSectorID);
                        if (sectorIDsToDeployTo.Count == qtyToDeploy) {
                            break;
                        }
                    }
                }

                if (sectorIDsToDeployTo.Count < qtyToDeploy) {
                    D.Warn(@"{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Fleets.
                        Fixing by expanding criteria to include sectors with systems.", DebugName, sectorIDsToDeployTo.Count, qtyToDeploy, player);
                    foreach (var neighborSectorID in homeNeighborSectorIDs) {
                        if (gameKnowledge.TryGetSystem(neighborSectorID, out unused)) {
                            sectorIDsToDeployTo.Push(neighborSectorID);
                            if (sectorIDsToDeployTo.Count == qtyToDeploy) {
                                break;
                            }
                        }
                    }
                    if (sectorIDsToDeployTo.Count < qtyToDeploy) {
                        D.Error("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Fleets.", DebugName, sectorIDsToDeployTo.Count, qtyToDeploy, player);
                        // Unlikely to occur, but if it does, I'll need to find other sectors to add
                    }
                }
            }

            GameDate deployDate = GameTime.GameStartDate;   // all startLevel creators always deploy on GameStartDate

            bool toDeployAutoCreators = true;
            IntVector3 deployedSectorID;
            Vector3 deployedLocation;
            foreach (var debugCreator in existingFleetCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    deployedSectorID = sectorIDsToDeployTo.Pop();
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugFleetCreator, player, deployedLocation, deployDate);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);
                D.Assert(sectorIDsToDeployTo.Count > Constants.Zero);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    deployedSectorID = sectorIDsToDeployTo.Pop();
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    AutoFleetCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoFleetCreator(player, deployedLocation, deployDate);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoFleetCreator(player, deployedLocation, deployDate);
                    }
                    creatorsDeployed.Add(autoCreator);
                }
            }
            D.AssertEqual(Constants.Zero, sectorIDsToDeployTo.Count);
        }
        //__ReportDeployedUnitCreators(typeof(AutoFleetCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    /// <summary>
    /// Deploys and configures the Home settlement creator in the player's Home System. 
    /// Returns the Creator that was deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the requirement
    /// then AutoUnitCreators are deployed and configured to fill in.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingSettlementCreators">The existing settlement creators.</param>
    /// <param name="usedSystems">Any systems that should not be used when selecting the system to deploy too.
    /// Warning: This method adds to this list when it selects the system to deploy too. As List is passed by reference,
    /// the list used by the caller will also be added too.</param>
    /// <returns></returns>
    private AUnitCreator DeployAndConfigureHomeSettlementCreator(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators,
        ref HashSet<ISystem> usedSystems) {  // OPTIMIZE ref not really necessary as List passed by Reference anyhow

        var gameKnowledge = _gameMgr.GameKnowledge;
        D.Assert(_playersHomeSectorLookup.ContainsKey(player), player.DebugName);
        IntVector3 homeSectorID = _playersHomeSectorLookup[player];

        AUnitCreator homeSystemCreator = null;
        ISystem homeSystem;
        if (!gameKnowledge.TryGetSystem(homeSectorID, out homeSystem)) {
            D.Error("{0} couldn't find a system in {1}'s home sector.", DebugName, player);
        }

        //D.Log("GameStartDate = {0}.", GameTime.GameStartDate.DebugName);
        GameDate deployDate = GameTime.GameStartDate;   // all startLevel creators always deploy on GameStartDate

        bool toDeployAutoCreator = true;
        foreach (var debugCreator in existingSettlementCreators) {
            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, homeSystem as SystemItem, deployDate);
                bool isAdded = usedSystems.Add(homeSystem);
                D.Assert(isAdded);
                homeSystemCreator = debugCreator;
                toDeployAutoCreator = false;
            }
        }

        if (toDeployAutoCreator) {
            AutoSettlementCreator autoCreator;
            if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                autoCreator = UnitConfigurator.GenerateRandomAutoSettlementCreator(player, homeSystem as SystemItem, deployDate);
            }
            else {
                autoCreator = UnitConfigurator.GeneratePresetAutoSettlementCreator(player, homeSystem as SystemItem, deployDate);
            }
            bool isAdded = usedSystems.Add(homeSystem);
            D.Assert(isAdded);
            homeSystemCreator = autoCreator;
        }

        D.AssertNotNull(homeSystemCreator);
        return homeSystemCreator;
    }

    /// <summary>
    /// Deploys and configures the non-Home settlement creators specified by the start level for this player 
    /// around the player's home Settlement that has already been deployed. 
    /// Returns all of the Creators that were deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingSettlementCreators">The existing settlement creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <param name="usedSystems">Any systems that should not be used when selecting the system to deploy too.
    /// Warning: This method adds to this list when it selects the system to deploy too. As List is passed by reference,
    /// the list used by the caller will also be added too.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureNonHomeStartLevelSettlementCreators(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators,
        int qtyToDeploy, ref HashSet<ISystem> usedSystems) {  // OPTIMIZE ref not really necessary as List passed by Reference anyhow

        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) { // there are additional settlements to deploy around the home system
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<ISystem> systemsToDeployTo = new Stack<ISystem>(qtyToDeploy);
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), player.DebugName);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            ISystem system;
            SectorGrid sectorGrid = SectorGrid.Instance;
            IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringSectorIDs(homeSectorID, includePeriphery: false);
            foreach (var neighborSectorID in homeNeighborSectorIDs) {
                if (gameKnowledge.TryGetSystem(neighborSectorID, out system)) {
                    if (!usedSystems.Contains(system)) {
                        systemsToDeployTo.Push(system);
                        if (systemsToDeployTo.Count == qtyToDeploy) {
                            break;
                        }
                    }
                }
            }
            // IMPROVE It seems theoretically possible that I won't be able to find enough systems...
            if (systemsToDeployTo.Count != qtyToDeploy) {
                D.Error("{0} only found {1} rather than {2} additional systems to deploy {3}'s Settlements.", DebugName, systemsToDeployTo.Count, qtyToDeploy, player);
            }

            GameDate deployDate = GameTime.GameStartDate;   // all startLevel creators always deploy on GameStartDate

            bool toDeployAutoCreators = true;
            foreach (var debugCreator in existingSettlementCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    system = systemsToDeployTo.Pop();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, system as SystemItem, deployDate);
                    bool isAdded = usedSystems.Add(system);
                    D.Assert(isAdded);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);
                D.Assert(systemsToDeployTo.Count > Constants.Zero);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    system = systemsToDeployTo.Pop();
                    AutoSettlementCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoSettlementCreator(player, system as SystemItem, deployDate);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoSettlementCreator(player, system as SystemItem, deployDate);
                    }
                    creatorsDeployed.Add(autoCreator);
                    bool isAdded = usedSystems.Add(system);
                    D.Assert(isAdded);
                }
            }
            D.AssertEqual(Constants.Zero, systemsToDeployTo.Count);
        }

        //__ReportDeployedUnitCreators(typeof(AutoSettlementCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    /// <summary>
    /// Deploys and configures the settlement creators specified by the start level for this player in and/or around the player's home sector. 
    /// Returns all of the Creators that were deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingSettlementCreators">The existing settlement creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <param name="usedSystems">Any systems that should not be used when selecting the system to deploy too.
    /// Warning: This method adds to this list when it selects the system to deploy too. As List is passed by reference,
    /// the list used by the caller will also be added too.</param>
    /// <returns></returns>
    [Obsolete("Could deploy a prior player's additional settlements to a system reserved for an upcoming player's HomeSystem")]
    private IList<AUnitCreator> DeployAndConfigureStartLevelSettlementCreators(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators,
        int qtyToDeploy, ref HashSet<ISystem> usedSystems) {  // OPTIMIZE ref not really necessary as List passed by Reference anyhow

        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<ISystem> systemsToDeployTo = new Stack<ISystem>(qtyToDeploy);
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), player.DebugName);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            ISystem system;
            if (!gameKnowledge.TryGetSystem(homeSectorID, out system)) {
                D.Error("{0} couldn't find a system in {1}'s home sector.", DebugName, player);
            }
            systemsToDeployTo.Push(system); // places home system on bottom but who cares?
            if (qtyToDeploy > Constants.One) {
                // there are additional settlements to deploy around the home system
                SectorGrid sectorGrid = SectorGrid.Instance;
                IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringSectorIDs(homeSectorID, includePeriphery: false);
                foreach (var neighborSectorID in homeNeighborSectorIDs) {
                    if (gameKnowledge.TryGetSystem(neighborSectorID, out system)) {
                        if (!usedSystems.Contains(system)) {
                            systemsToDeployTo.Push(system);
                            if (systemsToDeployTo.Count == qtyToDeploy) {
                                break;
                            }
                        }
                    }
                }
                // IMPROVE It seems theoretically possible that I won't be able to find enough systems...
                if (systemsToDeployTo.Count != qtyToDeploy) {
                    D.Error("{0} only found {1} rather than {2} additional systems to deploy {3}'s Settlements.", DebugName, systemsToDeployTo.Count, qtyToDeploy, player);
                }
            }

            GameDate deployDate = GameTime.GameStartDate;   // all startLevel creators always deploy on GameStartDate

            bool toDeployAutoCreators = true;
            foreach (var debugCreator in existingSettlementCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    system = systemsToDeployTo.Pop();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, system as SystemItem, deployDate);
                    bool isAdded = usedSystems.Add(system);
                    D.Assert(isAdded);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);
                D.Assert(systemsToDeployTo.Count > Constants.Zero);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    system = systemsToDeployTo.Pop();
                    AutoSettlementCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoSettlementCreator(player, system as SystemItem, deployDate);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoSettlementCreator(player, system as SystemItem, deployDate);
                    }
                    creatorsDeployed.Add(autoCreator);
                    bool isAdded = usedSystems.Add(system);
                    D.Assert(isAdded);
                }
            }
            D.AssertEqual(Constants.Zero, systemsToDeployTo.Count);
        }

        //__ReportDeployedUnitCreators(typeof(AutoSettlementCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    /// <summary>
    /// Determines whether the editor settings of the DebugUnitCreator provided allows the candidateOwner to
    /// become the owner. This is determined by comparing the desiredUserRelationship of the creator to
    /// the candidateOwner's InitialRelationship with the user.
    /// </summary>
    /// <param name="creatorSettings">The creator settings.</param>
    /// <param name="candidateOwner">The candidate owner.</param>
    /// <returns>
    ///   <c>true</c> if [is owner of creator] [the specified creator settings]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsOwnerOfCreator(AUnitCreatorEditorSettings creatorSettings, Player candidateOwner) {
        if (creatorSettings.IsOwnerUser != candidateOwner.IsUser) {
            return false;
        }
        if (candidateOwner.IsUser) {
            return true;
        }
        D.Assert(!creatorSettings.IsOwnerUser && !candidateOwner.IsUser);

        DiplomaticRelationship desiredUserRelationshipOfCreator = creatorSettings.DesiredRelationshipWithUser.Convert();
        IList<Player> aiOwnerCandidates;
        if (_aiPlayerInitialUserRelationsLookup.TryGetValue(desiredUserRelationshipOfCreator, out aiOwnerCandidates)) {
            return aiOwnerCandidates.Contains(candidateOwner);
        }
        return false;
    }

    /// <summary>
    /// Deploys and configures additional starbase creators for this player that deploy on a random date.
    /// Returns all the Creators that were deployed. Limit of 1 additional starbase deployed for this player per sector without a system.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingStarbaseCreators">The existing starbase creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureAdditionalStarbaseCreators(Player player, IEnumerable<ADebugUnitCreator> existingStarbaseCreators, int qtyToDeploy) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {
            SectorGrid sectorGrid = SectorGrid.Instance;
            var gameKnowledge = _gameMgr.GameKnowledge;

            Stack<IntVector3> sectorIDsToDeployTo = new Stack<IntVector3>(qtyToDeploy);
            HashSet<IntVector3> excludedSectorIDs = new HashSet<IntVector3>();

            ISystem unused;
            IntVector3 sectorID;
            for (int i = 0; i < qtyToDeploy; i++) {
                if (sectorGrid.TryGetRandomSectorID(out sectorID, includePeriphery: false, excludedIDs: excludedSectorIDs)) {
                    if (!gameKnowledge.TryGetSystem(sectorID, out unused)) {
                        sectorIDsToDeployTo.Push(sectorID);
                    }
                    else {
                        i--;
                    }
                    excludedSectorIDs.Add(sectorID);
                    continue;
                }
                break;
            }

            if (sectorIDsToDeployTo.Count < qtyToDeploy) {
                D.Warn("{0} only found {1} rather than {2} available sectors without systems to deploy {3}'s additional Starbases.", DebugName, sectorIDsToDeployTo.Count, qtyToDeploy, player);
                qtyToDeploy = sectorIDsToDeployTo.Count;
            }

            bool toDeployAutoCreators = true;
            Vector3 deployedLocation;
            foreach (var debugCreator in existingStarbaseCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    sectorID = sectorIDsToDeployTo.Pop();
                    deployedLocation = sectorGrid.GetSector(sectorID).GetClearRandomPointInsideSector();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugStarbaseCreator, player, deployedLocation);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    sectorID = sectorIDsToDeployTo.Pop();
                    deployedLocation = sectorGrid.GetSector(sectorID).GetClearRandomPointInsideSector();
                    AutoStarbaseCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoStarbaseCreator(player, deployedLocation);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoStarbaseCreator(player, deployedLocation);
                    }
                    creatorsDeployed.Add(autoCreator);
                }
            }
        }

        //__ReportDeployedUnitCreators(typeof(AutoStarbaseCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    /// <summary>
    /// Deploys and configures additional fleet creators for this player that deploy on a random date.
    /// Returns all of the Creators that were deployed. Limit of 1 additional fleet deployed for this player per sector.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingFleetCreators">The existing fleet creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureAdditionalFleetCreators(Player player, IEnumerable<ADebugUnitCreator> existingFleetCreators, int qtyToDeploy) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {

            SectorGrid sectorGrid = SectorGrid.Instance;
            Stack<IntVector3> sectorIDsToDeployTo = new Stack<IntVector3>(qtyToDeploy);

            IntVector3 sectorID;
            for (int i = 0; i < qtyToDeploy; i++) {
                if (sectorGrid.TryGetRandomSectorID(out sectorID, includePeriphery: false, excludedIDs: sectorIDsToDeployTo)) {
                    sectorIDsToDeployTo.Push(sectorID);
                    continue;
                }
                break;
            }

            if (sectorIDsToDeployTo.Count < qtyToDeploy) {
                D.Warn("{0} only found {1} rather than {2} available sectors to deploy {3}'s additional Fleets.", DebugName, sectorIDsToDeployTo.Count, qtyToDeploy, player);
                qtyToDeploy = sectorIDsToDeployTo.Count;
            }

            bool toDeployAutoCreators = true;
            IntVector3 deployedSectorID;
            Vector3 deployedLocation;
            foreach (var debugCreator in existingFleetCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    deployedSectorID = sectorIDsToDeployTo.Pop();
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugFleetCreator, player, deployedLocation);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);
                D.Assert(sectorIDsToDeployTo.Count > Constants.Zero);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    deployedSectorID = sectorIDsToDeployTo.Pop();
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    AutoFleetCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoFleetCreator(player, deployedLocation);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoFleetCreator(player, deployedLocation);
                    }
                    creatorsDeployed.Add(autoCreator);
                }
            }
            D.AssertEqual(Constants.Zero, sectorIDsToDeployTo.Count);
        }
        //__ReportDeployedUnitCreators(typeof(AutoFleetCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    /// <summary>
    /// Deploys and configures additional settlement creators for this player that deploy on a random date.
    /// Returns all of the Creators that were deployed. Limit of 1 settlement deployed for this player per unused system.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingSettlementCreators">The existing settlement creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <param name="usedSystems">Any systems that should not be used when selecting the system to deploy too.
    /// Warning: This method adds to this list when it selects the system to deploy too. As List is passed by reference,
    /// the list used by the caller will also be added too.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureAdditionalSettlementCreators(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators,
        int qtyToDeploy, ref HashSet<ISystem> usedSystems) {  // OPTIMIZE ref not really necessary as List passed by Reference anyhow
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<ISystem> systemsToDeployTo = new Stack<ISystem>(qtyToDeploy);

            ISystem system;
            for (int i = 0; i < qtyToDeploy; i++) {
                if (gameKnowledge.TryGetRandomSystem(usedSystems, out system)) {
                    systemsToDeployTo.Push(system);
                    bool isAdded = usedSystems.Add(system);
                    D.Assert(isAdded);
                    continue;
                }
                break;
            }

            if (systemsToDeployTo.Count < qtyToDeploy) {
                D.Warn("{0} only found {1} rather than {2} available systems to deploy {3}'s additional Settlements.", DebugName, systemsToDeployTo.Count, qtyToDeploy, player);
                qtyToDeploy = systemsToDeployTo.Count;
            }

            bool toDeployAutoCreators = true;
            foreach (var debugCreator in existingSettlementCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    system = systemsToDeployTo.Pop();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, system as SystemItem);

                    creatorsDeployed.Add(debugCreator);
                    if (creatorsDeployed.Count == qtyToDeploy) {
                        toDeployAutoCreators = false;
                        break;
                    }
                }
            }

            if (toDeployAutoCreators) {
                D.Assert(creatorsDeployed.Count < qtyToDeploy);
                D.Assert(systemsToDeployTo.Count > Constants.Zero);

                for (int deployedCount = creatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
                    system = systemsToDeployTo.Pop();
                    AutoSettlementCreator autoCreator;
                    if (_debugCntls.EquipmentPlan == DebugControls.EquipmentLoadout.Random) {
                        autoCreator = UnitConfigurator.GenerateRandomAutoSettlementCreator(player, system as SystemItem);
                    }
                    else {
                        autoCreator = UnitConfigurator.GeneratePresetAutoSettlementCreator(player, system as SystemItem);
                    }
                    creatorsDeployed.Add(autoCreator);
                }
            }
            D.AssertEqual(Constants.Zero, systemsToDeployTo.Count);
        }

        //__ReportDeployedUnitCreators(typeof(AutoSettlementCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }

    public void CompleteInitializationOfAllCelestialItems() {
        if (UniverseCenter != null) {
            UniverseCenter.FinalInitialize();
        }
        _systemCreators.ForAll(sc => sc.CompleteSystemInitialization());
    }

    public void CommenceOperationOfAllCelestialItems() {
        if (UniverseCenter != null) {
            UniverseCenter.CommenceOperations();
        }
        _systemCreators.ForAll(sc => sc.CommenceSystemOperations());
    }

    /// <summary>
    /// Builds the Commands and Elements of each Unit from their Creator and positions the Unit to its deployment location.
    /// <remarks>The Unit will not be operational until CommenceUnitOperationsOnDeployDate() is called.</remarks>
    /// </summary>
    public void BuildAndPositionUnits() {
        _unitCreators.ForAll(uc => uc.PrepareUnitForDeployment());
    }

    /// <summary>
    /// Commences operations of the previously built and positioned Unit on its deploy date.
    /// </summary>
    public void CommenceUnitOperationsOnDeployDate() {
        // 10.15.16 Initiate deployment of Settlements first so system ownership can be established before fleet orders are issued.
        // TODO Currently it doesn't matter as all Creators have either 'editor set' or 'random' DeployDates assigned. For release,
        // initial Settlement DeployDates will be on the game start date.
        var settlementCreators = _unitCreators.Where(creator => creator is AutoSettlementCreator || creator is DebugSettlementCreator);
        settlementCreators.ForAll(sc => sc.AuthorizeDeployment());
        var starbaseCreators = _unitCreators.Where(creator => creator is AutoStarbaseCreator || creator is DebugStarbaseCreator);
        starbaseCreators.ForAll(sc => sc.AuthorizeDeployment());
        var fleetCreators = _unitCreators.Except(settlementCreators).Except(starbaseCreators);
        fleetCreators.ForAll(fc => fc.AuthorizeDeployment());
    }

    /// <summary>
    /// Attempts to focus the camera on the primary user command. 
    /// If there is a User-owned settlement in the User's Home sector, it will become the
    /// focus. If not, then one of the user-owned fleets in the sector will become the focus.
    /// <remarks>
    /// If GameSettings indicates that only debug creators can be used, then this method does 
    /// nothing since there is no User HomeSector to inspect for the user's starting units.
    /// </remarks>
    /// </summary>
    public void AttemptFocusOnPrimaryUserCommand() {
        var gameSettings = _gameMgr.GameSettings;
        if (gameSettings.__UseDebugCreatorsOnly || !gameSettings.__ZoomOnUser) {
            return;
        }

        IntVector3 userHomeSectorID = _playersHomeSectorLookup[_gameMgr.UserPlayer];
        IUnitCmd_Ltd primaryUserCmd;
        bool isPrimaryUnitFound = TryFindPrimaryUserCommand(userHomeSectorID, out primaryUserCmd);
        D.Assert(isPrimaryUnitFound);
        (primaryUserCmd as ICameraFocusable).IsFocus = true;
    }

    private bool TryFindPrimaryUserCommand(IntVector3 userHomeSectorID, out IUnitCmd_Ltd primaryUserCmd) {
        var userKnowledge = _gameMgr.UserAIManager.Knowledge;
        bool isCmdFound = false;
        primaryUserCmd = null;
        ISettlementCmd_Ltd userHomeSettlement;
        if (userKnowledge.TryGetSettlement(userHomeSectorID, out userHomeSettlement)) {
            D.AssertEqual(_gameMgr.UserPlayer, (userHomeSettlement as ISettlementCmd).Owner);
            primaryUserCmd = userHomeSettlement;
            isCmdFound = true;
        }
        else {
            IEnumerable<IFleetCmd_Ltd> userHomeSectorFleets;
            if (userKnowledge.TryGetFleets(userHomeSectorID, out userHomeSectorFleets)) {
                var myHomeSectorFleets =
                    from fleet in userHomeSectorFleets
                    let myFleet = fleet as IFleetCmd
                    where myFleet.Owner == _gameMgr.UserPlayer
                    select fleet;

                if (myHomeSectorFleets.Any()) {
                    isCmdFound = true;
                    primaryUserCmd = myHomeSectorFleets.First();
                }
            }
        }
        return isCmdFound;
    }

    public void ResetForReuse() {
        SystemConfigurator.ResetForReuse();
        UnitConfigurator.ResetForReuse();
        UniverseCenter = null;
        _systemCreators = null;
        _unitCreators = null;
        _aiPlayerInitialUserRelationsLookup = null;
        _playersHomeSectorLookup = null;
    }

    private int CalcUniverseSystemsQty(int nonPeripheralSectorQty) {
        SystemDensity systemDensity = _gameMgr.GameSettings.SystemDensity;
        UniverseSize universeSize = _gameMgr.GameSettings.UniverseSize;
        int result = Mathf.FloorToInt(nonPeripheralSectorQty * systemDensity.SystemsPerSector(universeSize));
        int minReqdSystemQty = universeSize.MinReqdSystemQty();
        if (result < minReqdSystemQty) {
            D.Warn("{0}: Calculated System Qty {1} < Min Reqd System Qty {2}. Correcting. SystemDensity = {3}, UniverseSize = {4}.",
                DebugName, result, minReqdSystemQty, systemDensity.GetValueName(), universeSize.GetValueName());
            result = minReqdSystemQty;
        }
        D.Log(ShowDebugLog, "{0} calculated a need for {1} Systems in a universe of {2} non-peripheral sectors.", DebugName, result, nonPeripheralSectorQty);
        return result;
    }

    private FocusableItemCameraStat __MakeUCenterCameraStat(float radius, float closeOrbitInnerRadius) {
        float minViewDistance = radius + 1F;
        float closeOrbitOuterRadius = closeOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        float optViewDistance = closeOrbitOuterRadius + 1F;
        return new FocusableItemCameraStat(minViewDistance, optViewDistance, fov: 80F);
    }

    #region Debug

    private void __ReportDeployedUnitCreators(Type deployedType, Player player, IList<AUnitCreator> creatorsDeployed) {
        int qtyDeployed = creatorsDeployed.Count;
        int debugCreatorDeployedQty = creatorsDeployed.Select(c => c is ADebugUnitCreator).Count();
        int autoCreatorDeployedQty = qtyDeployed - debugCreatorDeployedQty;
        D.Log(ShowDebugLog, "{0} deployed {1} {2} for {3}. DebugCreators: {4}, AutoCreators: {5}.", DebugName, qtyDeployed, deployedType.Name, player, debugCreatorDeployedQty, autoCreatorDeployedQty);
    }

    /// <summary>
    /// Gets the AIPlayers that the User has not yet met, that have been assigned the initialUserRelationship to begin with when they do meet.
    /// </summary>
    /// <param name="initialUserRelationship">The initial user relationship.</param>
    /// <returns></returns>
    [Obsolete]
    public IEnumerable<Player> __GetUnmetAiPlayersWithInitialUserRelationsOf(DiplomaticRelationship initialUserRelationship) {
        D.Assert(_gameMgr.IsRunning, "This method should only be called when the User manually changes a unit's user relationship in the editor.");
        Player userPlayer = _gameMgr.UserPlayer;
        IList<Player> aiPlayersWithSpecifiedInitialUserRelations;
        if (_aiPlayerInitialUserRelationsLookup.TryGetValue(initialUserRelationship, out aiPlayersWithSpecifiedInitialUserRelations)) {
            return aiPlayersWithSpecifiedInitialUserRelations.Except(userPlayer.OtherKnownPlayers);
        }
        return Enumerable.Empty<Player>();
    }

    /// <summary>
    /// Only configures the existing debug system creators. Used when GameSettingsDebugControl has
    /// instructions to only configure and deploy existing debug system and unit creators.
    /// </summary>
    /// <returns></returns>
    private List<SystemCreator> __ConfigureExistingDebugCreatorsOnly_System() {
        DebugSystemCreator[] existingDebugCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugSystemCreator>();
        D.Log(ShowDebugLog, "{0} found {1} existing {2}s to configure.", DebugName, existingDebugCreators.Count(), typeof(DebugSystemCreator).Name);

        existingDebugCreators.ForAll(c => {
            string existingSystemName = c.SystemName;
            SystemConfigurator.AssignConfigurationToExistingDebugCreator(c, existingSystemName);
        });
        return existingDebugCreators.Cast<SystemCreator>().ToList();
    }

    /// <summary>
    /// Only configures the existing debug settlement creators. Used when GameSettingsDebugControl has
    /// instructions to only configure and deploy existing debug system and unit creators.
    /// </summary>
    /// <returns></returns>
    private IList<AUnitCreator> __ConfigureExistingDebugCreatorsOnly_Settlement(IEnumerable<ADebugUnitCreator> existingSettlementCreators) {
        var gameSettings = _gameMgr.GameSettings;
        D.Assert(gameSettings.__UseDebugCreatorsOnly);

        int existingSettlementCreatorQty = existingSettlementCreators.Count();
        IList<AUnitCreator> configuredSettlementCreators = new List<AUnitCreator>(existingSettlementCreatorQty);

        if (existingSettlementCreatorQty > Constants.Zero) {
            IEnumerable<ADebugUnitCreator> remainingSettlementCreators = new List<ADebugUnitCreator>(existingSettlementCreators);
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<ISystem> availableSystems = new Stack<ISystem>(gameKnowledge.Systems);
            if (availableSystems.Any()) {
                List<Player> allPlayers = new List<Player>(gameSettings.PlayerCount);
                allPlayers.Add(gameSettings.UserPlayer);
                allPlayers.AddRange(gameSettings.AIPlayers);

                foreach (var creator in existingSettlementCreators) {
                    if (!availableSystems.Any()) {
                        break;
                    }
                    foreach (var player in allPlayers) {
                        if (IsOwnerOfCreator(creator.EditorSettings, player)) {
                            ISystem system = availableSystems.Pop();
                            UnitConfigurator.AssignConfigurationToExistingCreator(creator as DebugSettlementCreator, player, system as SystemItem);
                            configuredSettlementCreators.Add(creator);
                            break;
                        }
                    }
                }
                remainingSettlementCreators = remainingSettlementCreators.Except(configuredSettlementCreators.Cast<ADebugUnitCreator>());
            }

            remainingSettlementCreators.ForAll(c => {
                D.Log(ShowDebugLog, "{0} is about to destroy excess Creator {1}.", DebugName, c.DebugName);
                GameUtility.Destroy(c.gameObject);
            });
        }
        return configuredSettlementCreators;
    }

    /// <summary>
    /// Only configures the existing debug starbase creators. Used when GameSettingsDebugControl has
    /// instructions to only configure and deploy existing debug system and unit creators.
    /// </summary>
    /// <returns></returns>
    private IList<AUnitCreator> __ConfigureExistingDebugCreatorsOnly_Starbase(IEnumerable<ADebugUnitCreator> existingStarbaseCreators) {
        var gameSettings = _gameMgr.GameSettings;
        D.Assert(gameSettings.__UseDebugCreatorsOnly);

        int existingStarbaseCreatorQty = existingStarbaseCreators.Count();
        IList<AUnitCreator> configuredStarbaseCreators = new List<AUnitCreator>(existingStarbaseCreatorQty);

        if (existingStarbaseCreatorQty > Constants.Zero) {
            IEnumerable<ADebugUnitCreator> remainingStarbaseCreators = new List<ADebugUnitCreator>(existingStarbaseCreators);

            List<Player> allPlayers = new List<Player>(gameSettings.PlayerCount);
            allPlayers.Add(gameSettings.UserPlayer);
            allPlayers.AddRange(gameSettings.AIPlayers);

            foreach (var creator in existingStarbaseCreators) {
                Vector3 location = creator.transform.position;
                foreach (var player in allPlayers) {
                    if (IsOwnerOfCreator(creator.EditorSettings, player)) {
                        UnitConfigurator.AssignConfigurationToExistingCreator(creator as DebugStarbaseCreator, player, location);
                        configuredStarbaseCreators.Add(creator);
                        break;
                    }
                }
            }
            remainingStarbaseCreators = remainingStarbaseCreators.Except(configuredStarbaseCreators.Cast<ADebugUnitCreator>());

            remainingStarbaseCreators.ForAll(c => {
                D.Log(ShowDebugLog, "{0} is about to destroy excess Creator {1}.", DebugName, c.DebugName);
                GameUtility.Destroy(c.gameObject);
            });
        }
        return configuredStarbaseCreators;
    }

    /// <summary>
    /// Only configures the existing debug fleet creators. Used when GameSettingsDebugControl has
    /// instructions to only configure and deploy existing debug system and unit creators.
    /// </summary>
    /// <returns></returns>
    private IList<AUnitCreator> __ConfigureExistingDebugCreatorsOnly_Fleet(IEnumerable<ADebugUnitCreator> existingFleetCreators) {
        var gameSettings = _gameMgr.GameSettings;
        D.Assert(gameSettings.__UseDebugCreatorsOnly);

        int existingFleetCreatorQty = existingFleetCreators.Count();
        IList<AUnitCreator> configuredFleetCreators = new List<AUnitCreator>(existingFleetCreatorQty);

        if (existingFleetCreatorQty > Constants.Zero) {
            IEnumerable<ADebugUnitCreator> remainingFleetCreators = new List<ADebugUnitCreator>(existingFleetCreators);

            List<Player> allPlayers = new List<Player>(gameSettings.PlayerCount);
            allPlayers.Add(gameSettings.UserPlayer);
            allPlayers.AddRange(gameSettings.AIPlayers);

            foreach (var creator in existingFleetCreators) {
                Vector3 location = creator.transform.position;
                foreach (var player in allPlayers) {
                    if (IsOwnerOfCreator(creator.EditorSettings, player)) {
                        UnitConfigurator.AssignConfigurationToExistingCreator(creator as DebugFleetCreator, player, location);
                        configuredFleetCreators.Add(creator);
                        break;
                    }
                }
            }
            remainingFleetCreators = remainingFleetCreators.Except(configuredFleetCreators.Cast<ADebugUnitCreator>());

            remainingFleetCreators.ForAll(c => {
                D.Log(ShowDebugLog, "{0} is about to destroy excess Creator {1}.", DebugName, c.DebugName);
                GameUtility.Destroy(c.gameObject);
            });
        }
        return configuredFleetCreators;
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }

}

