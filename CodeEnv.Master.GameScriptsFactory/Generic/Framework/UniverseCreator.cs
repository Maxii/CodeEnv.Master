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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

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

    private string Name { get { return GetType().Name; } }


    private IDictionary<DiplomaticRelationship, IList<Player>> _aiPlayerInitialUserRelationsLookup;
    private IDictionary<Player, IntVector3> _playersHomeSectorLookup;
    private List<AUnitCreator> _unitCreators;
    private List<SystemCreator> _systemCreators;
    //private SectorGrid _sectorGrid;   // ref to SectorGrid complicates things as SectorGrid is not persistent
    private GameManager _gameMgr;

    public UniverseCreator() {
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        SystemConfigurator = new NewGameSystemConfigurator();
        UnitConfigurator = new NewGameUnitConfigurator();
    }

    public void InitializeUniverseCenter() {
        UniverseCenter = UniverseFolder.Instance.GetComponentInChildren<UniverseCenterItem>();
        if (UniverseCenter != null) {
            float radius = TempGameValues.UniverseCenterRadius;
            float closeOrbitInnerRadius = radius + 5F;
            UniverseCenter.Name = "UniverseCenter";
            UniverseCenterData data = new UniverseCenterData(UniverseCenter, radius, closeOrbitInnerRadius);
            FocusableItemCameraStat cameraStat = __MakeUCenterCameraStat(radius, closeOrbitInnerRadius);
            UniverseCenter.CameraStat = cameraStat;
            UniverseCenter.Data = data;
            // UC will be enabled when CommenceOperations() called
        }
    }

    public void BuildSectors() {
        SectorGrid.Instance.BuildSectors();
    }

    public void DeployAndConfigureSystemCreators() {
        GameSettings gameSettings = _gameMgr.GameSettings;
        var sectorGrid = SectorGrid.Instance;
        var deployableSectorIDs = sectorGrid.NonPeripheralSectorIndices;
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
            _playersHomeSectorLookup.Add(player, homeCreator.SectorIndex);
            _systemCreators.Add(homeCreator);
            occupiedSectorIDs.Add(homeCreator.SectorIndex);
        }

        // Now deploy any additional SystemCreators that need to be around each intended Home SystemCreator
        var userPlayer = gameSettings.UserPlayer;
        var startLevel = gameSettings.UserStartLevel;
        var homeSectorID = _playersHomeSectorLookup[userPlayer];    // UNCLEAR rqmt for occupied sectors here?
        var deployedSystemCreators = SystemConfigurator.DeployAndConfigureAdditionalCreatorsAround(homeSectorID, startLevel);
        _systemCreators.AddRange(deployedSystemCreators);
        occupiedSectorIDs.AddRange(deployedSystemCreators.Select(c => c.SectorIndex));

        var aiPlayers = gameSettings.AIPlayers;
        for (int i = 0; i < aiPlayers.Length; i++) {
            var aiPlayer = aiPlayers[i];
            startLevel = gameSettings.AIPlayersStartLevel[i];
            homeSectorID = _playersHomeSectorLookup[aiPlayer];
            deployedSystemCreators = SystemConfigurator.DeployAndConfigureAdditionalCreatorsAround(homeSectorID, startLevel);
            _systemCreators.AddRange(deployedSystemCreators);
            occupiedSectorIDs.AddRange(deployedSystemCreators.Select(c => c.SectorIndex));
        }

        int remainingSystemQty = systemQty - _systemCreators.Count;
        D.Assert(remainingSystemQty >= 0);

        // Now deploy any DebugSystemCreators taking into account sectors already occupied
        deployedSystemCreators = SystemConfigurator.ConfigureExistingDebugCreators(remainingSystemQty, occupiedSectorIDs).Cast<SystemCreator>();
        _systemCreators.AddRange(deployedSystemCreators);
        occupiedSectorIDs.AddRange(deployedSystemCreators.Select(c => c.SectorIndex));
        //D.Log("{0} _systemCreators contents: {1}.", Name, _systemCreators.Select(c => c.Name).Concatenate());
        remainingSystemQty = systemQty - _systemCreators.Count;
        D.Assert(remainingSystemQty >= 0);
        if (remainingSystemQty == 0) {
            return; // no more creators to deploy
        }

        // Find and destroy any existing DebugStarbaseCreators that reside in
        var sectorIDsOccupiedByStarbaseCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugStarbaseCreator>().Select(sbc => sbc.SectorIndex);
        var unoccupiedSectorIDs = deployableSectorIDs.Except(occupiedSectorIDs).Except(sectorIDsOccupiedByStarbaseCreators);

        unoccupiedSectorIDs = unoccupiedSectorIDs.Shuffle();
        var sectorIDsToDeployTo = unoccupiedSectorIDs.Take(remainingSystemQty);
        var sectorPositionsToDeployTo = sectorIDsToDeployTo.Select(index => sectorGrid.GetSectorPosition(index));
        sectorPositionsToDeployTo.ForAll(position => {
            var creator = SystemConfigurator.DeployAndConfigureRandomSystemCreatorTo(position);
            _systemCreators.Add(creator);
        });

        int randomSystemQtyDeployed = sectorIDsToDeployTo.Count();
        D.Log("{0} deployed and configured {1} additional random {2}s.", GetType().Name, randomSystemQtyDeployed, typeof(SystemCreator).Name);
        int systemQtyNotDeployed = remainingSystemQty - randomSystemQtyDeployed;
        D.Warn(systemQtyNotDeployed > 0, "{0} ran out of sectors to deploy {1} remaining systems.", Name, systemQtyNotDeployed);
    }
    //public void DeployAndConfigureSystemCreators() {
    //    var sectorGrid = SectorGrid.Instance;
    //    var deployableSectorIndices = sectorGrid.NonPeripheralSectorIndices;
    //    int systemQty = CalcUniverseSystemsQty(deployableSectorIndices.Count());

    //    _systemCreators = SystemConfigurator.DeployAndConfigurePlayersStartingSystemCreators(_gameMgr.GameSettings);
    //    List<IntVector3> occupiedSectors = _systemCreators.Select(c => c.SectorIndex).ToList();

    //    int remainingSystemQty = systemQty - _systemCreators.Count;
    //    D.Assert(remainingSystemQty >= 0);

    //    var deployedDebugCreators = SystemConfigurator.ConfigureExistingDebugCreators(remainingSystemQty, occupiedSectors).Cast<SystemCreator>();
    //    _systemCreators.AddRange(deployedDebugCreators);
    //    occupiedSectors.AddRange(deployedDebugCreators.Select(c => c.SectorIndex));
    //    //D.Log("{0} _systemCreators contents: {1}.", Name, _systemCreators.Select(c => c.Name).Concatenate());
    //    remainingSystemQty = systemQty - _systemCreators.Count;
    //    D.Assert(remainingSystemQty >= 0);
    //    if(remainingSystemQty == 0) {
    //        return; // no more creators to deploy
    //    }


    //    //int deployedDebugSystemCreatorQty = _systemCreators.Count;
    //    //D.Assert(deployedDebugSystemCreatorQty <= systemQty);
    //    //if (systemQty == deployedDebugSystemCreatorQty) {
    //    //    return; // no more creators to deploy
    //    //}
    //    //var sectorIndicesAlreadyOccupiedByDebugSystemCreators = _systemCreators.Select(sc => sc.SectorIndex);
    //    var sectorsOccupiedByStarbaseCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugStarbaseCreator>().Select(sbc => sbc.SectorIndex);
    //    var unoccupiedSectors = deployableSectorIndices.Except(occupiedSectors)            .Except(sectorsOccupiedByStarbaseCreators);

    //    unoccupiedSectors = unoccupiedSectors.Shuffle();
    //    var sectorsToDeployTo = unoccupiedSectors.Take(remainingSystemQty);
    //    var sectorPositionsToDeployTo = sectorsToDeployTo.Select(index => sectorGrid.GetSectorPosition(index));
    //    sectorPositionsToDeployTo.ForAll(position => {
    //        var creator = SystemConfigurator.DeployAndConfigureRandomSystemCreatorTo(position);
    //        _systemCreators.Add(creator);
    //    });

    //    int randomSystemQtyDeployed = sectorsToDeployTo.Count();
    //    D.Log("{0} deployed and configured {1} additional random {2}s.", GetType().Name, randomSystemQtyDeployed, typeof(SystemCreator).Name);
    //    int systemQtyNotDeployed = remainingSystemQty - randomSystemQtyDeployed;
    //    D.Warn(systemQtyNotDeployed > 0, "{0} ran out of sectors to deploy {1} remaining systems.", Name, systemQtyNotDeployed);
    //}
    //public void DeployAndConfigureSystemCreators() {
    //    var sectorGrid = SectorGrid.Instance;
    //    var deployableSectorIndices = sectorGrid.AllSectorIndexes.Where(index => !sectorGrid.GetSector(index).IsOnPeriphery);
    //    int systemQty = CalcUniverseSystemsQty(deployableSectorIndices.Count());
    //    _systemCreators = SystemConfigurator.ConfigureExistingDebugCreators(systemQty).Cast<SystemCreator>().ToList();
    //    //D.Log("{0} _systemCreators contents: {1}.", Name, _systemCreators.Select(c => c.Name).Concatenate());

    //    int deployedDebugSystemCreatorQty = _systemCreators.Count;
    //    D.Assert(deployedDebugSystemCreatorQty <= systemQty);
    //    if (systemQty == deployedDebugSystemCreatorQty) {
    //        return; // no more creators to deploy
    //    }
    //    var sectorIndicesAlreadyOccupiedByDebugSystemCreators = _systemCreators.Select(sc => sc.SectorIndex);
    //    var sectorIndicesOccupiedByStarbaseCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugStarbaseCreator>().Select(sbc => sbc.SectorIndex);
    //    var unoccupiedDeployableSectorIndices = deployableSectorIndices.Except(sectorIndicesAlreadyOccupiedByDebugSystemCreators)
    //        .Except(sectorIndicesOccupiedByStarbaseCreators);
    //    int additionalSystemQtyToDeploy = systemQty - deployedDebugSystemCreatorQty;

    //    var unoccupiedSectorIndices = unoccupiedDeployableSectorIndices.Shuffle();
    //    var sectorIndicesToDeployTo = unoccupiedSectorIndices.Take(additionalSystemQtyToDeploy);
    //    var sectorPositionsToDeployTo = sectorIndicesToDeployTo.Select(index => sectorGrid.GetSectorPosition(index));
    //    sectorPositionsToDeployTo.ForAll(position => {
    //        var creator = SystemConfigurator.DeployAndConfigureRandomSystemCreatorTo(position);
    //        _systemCreators.Add(creator);
    //    });
    //    D.Log("{0} deployed and configured {1} additional {2}s.", GetType().Name, additionalSystemQtyToDeploy, typeof(SystemCreator).Name);
    //}

    public void BuildSystems() {
        _systemCreators.ForAll(sc => {
            //D.Log("{0} is about to call {1}.BuildAndDeploySystem().", Name, sc.Name);
            sc.BuildAndDeploySystem();
        });
    }

    public void DeployAndConfigureInitialUnitCreators() {
        ADebugUnitCreator[] existingDebugCreators = UniverseFolder.Instance.GetComponentsInChildren<ADebugUnitCreator>();
        _aiPlayerInitialUserRelationsLookup = AssignAllPlayerInitialRelationships(existingDebugCreators);
        _unitCreators = new List<AUnitCreator>();

        IEnumerable<ADebugUnitCreator> fleetDebugCreators = existingDebugCreators.Where(c => c is DebugFleetCreator);
        IEnumerable<ADebugUnitCreator> starbaseDebugCreators = existingDebugCreators.Where(c => c is DebugStarbaseCreator);
        IEnumerable<ADebugUnitCreator> settlementDebugCreators = existingDebugCreators.Where(c => c is DebugSettlementCreator);

        GameSettings gameSettings = _gameMgr.GameSettings;

        // Handle the user first
        var userPlayer = gameSettings.UserPlayer;
        var userPlayerStartLevel = gameSettings.UserStartLevel;

        int fleetQty = userPlayerStartLevel.FleetStartQty();
        var fleetCreatorsDeployed = DeployAndConfigureInitialFleetCreators(userPlayer, fleetDebugCreators, fleetQty);
        _unitCreators.AddRange(fleetCreatorsDeployed);
        var fleetDebugCreatorsDeployed = fleetCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
        fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

        int starbaseQty = userPlayerStartLevel.StarbaseStartQty();
        var starbaseCreatorsDeployed = DeployAndConfigureInitialStarbaseCreators(userPlayer, starbaseDebugCreators, starbaseQty);
        _unitCreators.AddRange(starbaseCreatorsDeployed);
        var starbaseDebugCreatorsDeployed = starbaseCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
        starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

        IList<ISystem> usedSystems = new List<ISystem>();
        int settlementQty = userPlayerStartLevel.SettlementStartQty();
        var settlementCreatorsDeployed = DeployAndConfigureInitialSettlementCreators(userPlayer, settlementDebugCreators, settlementQty, usedSystems);
        _unitCreators.AddRange(settlementCreatorsDeployed);
        var settlementDebugCreatorsDeployed = settlementCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
        settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);

        // Handle the AIPlayers
        var aiPlayers = gameSettings.AIPlayers;
        int aiPlayerQty = aiPlayers.Length;
        for (int i = 0; i < aiPlayerQty; i++) {
            var aiPlayer = aiPlayers[i];
            var aiPlayerStartLevel = gameSettings.AIPlayersStartLevel[i];

            fleetQty = aiPlayerStartLevel.FleetStartQty();
            fleetCreatorsDeployed = DeployAndConfigureInitialFleetCreators(aiPlayer, fleetDebugCreators, fleetQty);
            _unitCreators.AddRange(fleetCreatorsDeployed);
            fleetDebugCreatorsDeployed = fleetCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

            starbaseQty = aiPlayerStartLevel.StarbaseStartQty();
            starbaseCreatorsDeployed = DeployAndConfigureInitialStarbaseCreators(aiPlayer, starbaseDebugCreators, starbaseQty);
            _unitCreators.AddRange(starbaseCreatorsDeployed);
            starbaseDebugCreatorsDeployed = starbaseCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

            settlementQty = aiPlayerStartLevel.SettlementStartQty();
            settlementCreatorsDeployed = DeployAndConfigureInitialSettlementCreators(aiPlayer, settlementDebugCreators, settlementQty, usedSystems);
            _unitCreators.AddRange(settlementCreatorsDeployed);
            settlementDebugCreatorsDeployed = settlementCreatorsDeployed.Where(c => c is ADebugUnitCreator).Cast<ADebugUnitCreator>();
            settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);
        }
    }
    //public void DeployAndConfigureInitialUnitCreators() {
    //    ADebugUnitCreator[] existingCreators = UniverseFolder.Instance.GetComponentsInChildren<ADebugUnitCreator>();
    //    _aiPlayerInitialUserRelationsLookup = AssignAllPlayerInitialRelationships(existingCreators);

    //    IEnumerable<ADebugUnitCreator> fleetDebugCreators = existingCreators.Where(c => c is DebugFleetCreator);
    //    IEnumerable<ADebugUnitCreator> starbaseDebugCreators = existingCreators.Where(c => c is DebugStarbaseCreator);
    //    IEnumerable<ADebugUnitCreator> settlementDebugCreators = existingCreators.Where(c => c is DebugSettlementCreator);

    //    GameSettings gameSettings = _gameMgr.GameSettings;

    //    // Handle the user first
    //    var userPlayer = gameSettings.UserPlayer;
    //    var userPlayerStartLevel = gameSettings.UserStartLevel;

    //    int fleetQty = userPlayerStartLevel.FleetStartQty();
    //    var fleetDebugCreatorsDeployed = DeployAndConfigureInitialFleetCreators(userPlayer, fleetDebugCreators, fleetQty);
    //    fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

    //    int starbaseQty = userPlayerStartLevel.StarbaseStartQty();
    //    var starbaseDebugCreatorsDeployed = DeployAndConfigureInitialStarbaseCreators(userPlayer, starbaseDebugCreators, starbaseQty);
    //    starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

    //    IList<ISystem> usedSystems = new List<ISystem>();
    //    int settlementQty = userPlayerStartLevel.SettlementStartQty();
    //    var settlementDebugCreatorsDeployed = DeployAndConfigureInitialSettlementCreators(userPlayer, settlementDebugCreators, settlementQty, usedSystems);
    //    settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);

    //    // Handle the AIPlayers
    //    var aiPlayers = gameSettings.AIPlayers;
    //    int aiPlayerQty = aiPlayers.Length;
    //    for (int i = 0; i < aiPlayerQty; i++) {
    //        var aiPlayer = aiPlayers[i];
    //        var aiPlayerStartLevel = gameSettings.AIPlayersStartLevel[i];

    //        fleetQty = aiPlayerStartLevel.FleetStartQty();
    //        fleetDebugCreatorsDeployed = DeployAndConfigureInitialFleetCreators(aiPlayer, fleetDebugCreators, fleetQty);
    //        fleetDebugCreators = fleetDebugCreators.Except(fleetDebugCreatorsDeployed);

    //        starbaseQty = aiPlayerStartLevel.StarbaseStartQty();
    //        starbaseDebugCreatorsDeployed = DeployAndConfigureInitialStarbaseCreators(aiPlayer, starbaseDebugCreators, starbaseQty);
    //        starbaseDebugCreators = starbaseDebugCreators.Except(starbaseDebugCreatorsDeployed);

    //        settlementQty = aiPlayerStartLevel.SettlementStartQty();
    //        settlementDebugCreatorsDeployed = DeployAndConfigureInitialSettlementCreators(aiPlayer, settlementDebugCreators, settlementQty, usedSystems);
    //        settlementDebugCreators = settlementDebugCreators.Except(settlementDebugCreatorsDeployed);
    //    }
    //}

    /// <summary>
    /// Assigns all player's initial relationships returning a lookup table of initial relationships of all AIPlayers with the User.
    /// </summary>
    /// <param name="existingDebugUnitCreators">The existing debug unit creators.</param>
    /// <returns></returns>
    private IDictionary<DiplomaticRelationship, IList<Player>> AssignAllPlayerInitialRelationships(ADebugUnitCreator[] existingDebugUnitCreators) {
        Player userPlayer = _gameMgr.UserPlayer;
        IList<Player> aiPlayers = _gameMgr.AIPlayers;
        int aiPlayerQty = aiPlayers.Count;

        var aiOwnedDebugUnitCreators = existingDebugUnitCreators.Where(uc => !uc.EditorSettings.IsOwnerUser);
        var desiredAiUserRelationships = aiOwnedDebugUnitCreators.Select(uc => uc.EditorSettings.DesiredRelationshipWithUser.Convert());

        HashSet<DiplomaticRelationship> uniqueDesiredAiUserRelationships = new HashSet<DiplomaticRelationship>(desiredAiUserRelationships);
        //D.Log("{0}: Unique desired AI/User Relationships = {1}.", Name, uniqueDesiredAiUserRelationships.Select(r => r.GetValueName()).Concatenate());

        // Setup initial AIPlayer <-> User relationships derived from editorCreators..
        Dictionary<DiplomaticRelationship, IList<Player>> aiPlayerInitialUserRelationsLookup = new Dictionary<DiplomaticRelationship, IList<Player>>(aiPlayerQty);
        Stack<Player> unassignedAIPlayers = new Stack<Player>(aiPlayers);
        uniqueDesiredAiUserRelationships.ForAll(aiUserRelationship => {
            if (unassignedAIPlayers.Count > Constants.Zero) {
                var aiPlayer = unassignedAIPlayers.Pop();
                //D.Log("{0} about to set {1}'s user relationship to {2}.", Name, aiPlayer, aiUserRelationship.GetValueName());
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
                //D.Log("{0} about to set {1}'s user relationship to {2}.", Name, aiPlayer, DiplomaticRelationship.Neutral.GetValueName());
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
    /// Deploys and configures the initial starbase creators for this player around the player's home sector. 
    /// Returns any of the provided existing DebugCreators that were deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingStarbaseCreators">The existing starbase creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureInitialStarbaseCreators(Player player, IEnumerable<ADebugUnitCreator> existingStarbaseCreators, int qtyToDeploy) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {
            SectorGrid sectorGrid = SectorGrid.Instance;
            var gameKnowledge = _gameMgr.GameKnowledge;
            IList<IntVector3> candidateSectorIDs = new List<IntVector3>();
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), "{0}: {1} has no assigned home system sector.", Name, player);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
            foreach (var neighborSectorID in homeNeighborSectorIDs) {
                ISystem unusedSystem;
                if (!gameKnowledge.TryGetSystem(neighborSectorID, out unusedSystem)) {
                    candidateSectorIDs.Add(neighborSectorID);
                }
            }
            if (candidateSectorIDs.Count < qtyToDeploy) {
                D.Warn("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Starbases. Fixing.", Name, candidateSectorIDs.Count, qtyToDeploy, player);
                candidateSectorIDs = homeNeighborSectorIDs.ToList();
                if (candidateSectorIDs.Count < qtyToDeploy) {
                    D.Error("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Starbases.", Name, candidateSectorIDs.Count, qtyToDeploy, player);
                    // Unlikely to occur, but if it does, I'll need to find other sectors to add
                }
            }

            bool toDeployAutoCreators = true;
            IntVector3 deployedSectorID;
            Vector3 deployedLocation;
            foreach (var debugCreator in existingStarbaseCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    deployedSectorID = RandomExtended.Choice(candidateSectorIDs);
                    deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugStarbaseCreator, player, deployedLocation);
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
                    StarbaseCreator autoCreator = UnitConfigurator.GenerateRandomAutoStarbaseCreator(player, deployedLocation);
                    creatorsDeployed.Add(autoCreator);
                    candidateSectorIDs.Remove(deployedSectorID);
                }
            }
        }

        __ReportDeployedUnitCreators(typeof(StarbaseCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }
    //private IList<ADebugUnitCreator> DeployAndConfigureInitialStarbaseCreators(Player player, IEnumerable<ADebugUnitCreator> existingStarbaseCreators, int qtyToDeploy) {
    //    IList<ADebugUnitCreator> debugCreatorsDeployed = new List<ADebugUnitCreator>(existingStarbaseCreators.Count());
    //    if (qtyToDeploy > Constants.Zero) {
    //        SectorGrid sectorGrid = SectorGrid.Instance;
    //        var gameKnowledge = _gameMgr.GameKnowledge;
    //        IList<IntVector3> candidateSectorIDs = new List<IntVector3>();
    //        D.Assert(_playersHomeSectorLookup.ContainsKey(player), "{0}: {1} has no assigned home system sector.", Name, player);
    //        IntVector3 homeSectorID = _playersHomeSectorLookup[player];

    //        IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
    //        foreach (var neighborSectorID in homeNeighborSectorIDs) {
    //            ISystem unusedSystem;
    //            if (!gameKnowledge.TryGetSystem(neighborSectorID, out unusedSystem)) {
    //                candidateSectorIDs.Add(neighborSectorID);
    //            }
    //        }
    //        if (candidateSectorIDs.Count < qtyToDeploy) {
    //            D.Warn("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Starbases. Fixing.", Name, candidateSectorIDs.Count, qtyToDeploy, player);
    //            candidateSectorIDs = homeNeighborSectorIDs.ToList();
    //            if (candidateSectorIDs.Count < qtyToDeploy) {
    //                D.Error("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Starbases.", Name, candidateSectorIDs.Count, qtyToDeploy, player);
    //                // Unlikely to occur, but if it does, I'll need to find other sectors to add
    //            }
    //        }

    //        bool toDeployAutoCreators = true;
    //        IntVector3 deployedSectorID;
    //        Vector3 deployedLocation;
    //        foreach (var debugCreator in existingStarbaseCreators) {
    //            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
    //                deployedSectorID = RandomExtended.Choice(candidateSectorIDs);
    //                deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
    //                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugStarbaseCreator, player, deployedLocation);
    //                candidateSectorIDs.Remove(deployedSectorID);

    //                debugCreatorsDeployed.Add(debugCreator);
    //                if (debugCreatorsDeployed.Count == qtyToDeploy) {
    //                    toDeployAutoCreators = false;
    //                    break;
    //                }
    //            }
    //        }

    //        if (toDeployAutoCreators) {
    //            D.Assert(debugCreatorsDeployed.Count < qtyToDeploy);

    //            for (int deployedCount = debugCreatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
    //                deployedSectorID = RandomExtended.Choice(candidateSectorIDs);
    //                deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
    //                UnitConfigurator.GenerateRandomAutoStarbaseCreator(player, deployedLocation);
    //                candidateSectorIDs.Remove(deployedSectorID);
    //            }
    //        }
    //    }

    //    //__ReportDeployedUnitCreators(qtyToDeploy, typeof(StarbaseCreator), player, debugCreatorsDeployed);
    //    return debugCreatorsDeployed;
    //}
    //private IList<ADebugUnitCreator> DeployAndConfigureInitialStarbaseCreators(Player player, IEnumerable<ADebugUnitCreator> existingStarbaseCreators, int qtyToDeploy) {
    //    IList<ADebugUnitCreator> debugCreatorsDeployed = new List<ADebugUnitCreator>(existingStarbaseCreators.Count());
    //    if (qtyToDeploy > Constants.Zero) {
    //        bool toDeployAutoCreators = true;
    //        Vector3 tgtLocation;
    //        foreach (var debugCreator in existingStarbaseCreators) {
    //            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
    //                tgtLocation = debugCreator.transform.position;
    //                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugStarbaseCreator, player, tgtLocation);

    //                debugCreatorsDeployed.Add(debugCreator);
    //                if (debugCreatorsDeployed.Count == qtyToDeploy) {
    //                    toDeployAutoCreators = false;
    //                    break;
    //                }
    //            }
    //        }

    //        if (toDeployAutoCreators) {
    //            D.Assert(debugCreatorsDeployed.Count < qtyToDeploy);
    //            SectorGrid sectorGrid = SectorGrid.Instance;

    //            for (int deployedCount = debugCreatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
    //                tgtLocation = sectorGrid.RandomSector.GetClearRandomPointInsideSector();
    //                UnitConfigurator.GenerateRandomAutoStarbaseCreator(player, tgtLocation);
    //            }
    //        }
    //    }

    //    //__ReportDeployedUnitCreators(qtyToDeploy, typeof(StarbaseCreator), player, debugCreatorsDeployed);
    //    return debugCreatorsDeployed;
    //}

    /// <summary>
    /// Deploys and configures the initial fleet creators for this player. Returns any of the provided
    /// existing DebugCreators that were deployed.
    /// <remarks>The existing DebugUnitCreators provided are configured first. If those do not fill the quantity requirement
    /// then AutoUnitCreators are deployed and configured to fill in the rest.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="existingFleetCreators">The existing fleet creators.</param>
    /// <param name="qtyToDeploy">The qty to deploy.</param>
    /// <returns></returns>
    private IList<AUnitCreator> DeployAndConfigureInitialFleetCreators(Player player, IEnumerable<ADebugUnitCreator> existingFleetCreators, int qtyToDeploy) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {

            SectorGrid sectorGrid = SectorGrid.Instance;
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<IntVector3> sectorIDsToDeployTo = new Stack<IntVector3>(qtyToDeploy);
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), "{0}: {1} has no assigned home system sector.", Name, player);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            ISystem unUsedSystem;
            if (!gameKnowledge.TryGetSystem(homeSectorID, out unUsedSystem)) {
                D.Error("{0} couldn't find a system in {1}'s home sector.", Name, player);
            }

            sectorIDsToDeployTo.Push(homeSectorID); // if start with fleet(s), always deploy one in the home sector
            if (qtyToDeploy > Constants.One) {
                // there are additional fleets to deploy around the home system
                IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
                foreach (var neighborSectorID in homeNeighborSectorIDs) {
                    if (!gameKnowledge.TryGetSystem(neighborSectorID, out unUsedSystem)) {
                        sectorIDsToDeployTo.Push(neighborSectorID);
                        if (sectorIDsToDeployTo.Count == qtyToDeploy) {
                            break;
                        }
                    }
                }

                if (sectorIDsToDeployTo.Count < qtyToDeploy) {
                    D.Warn("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Fleets. Fixing.", Name, sectorIDsToDeployTo.Count, qtyToDeploy, player);
                    foreach (var neighborSectorID in homeNeighborSectorIDs) {
                        if (gameKnowledge.TryGetSystem(neighborSectorID, out unUsedSystem)) {
                            sectorIDsToDeployTo.Push(neighborSectorID);
                            if (sectorIDsToDeployTo.Count == qtyToDeploy) {
                                break;
                            }
                        }
                    }
                    if (sectorIDsToDeployTo.Count < qtyToDeploy) {
                        D.Error("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Fleets.", Name, sectorIDsToDeployTo.Count, qtyToDeploy, player);
                        // Unlikely to occur, but if it does, I'll need to find other sectors to add
                    }
                }
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
                    FleetCreator autoCreator = UnitConfigurator.GenerateRandomAutoFleetCreator(player, deployedLocation);
                    creatorsDeployed.Add(autoCreator);
                }
            }
            D.Assert(sectorIDsToDeployTo.Count == Constants.Zero);
        }
        __ReportDeployedUnitCreators(typeof(FleetCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }
    //private IList<ADebugUnitCreator> DeployAndConfigureInitialFleetCreators(Player player, IEnumerable<ADebugUnitCreator> existingFleetCreators, int qtyToDeploy) {
    //    IList<ADebugUnitCreator> debugCreatorsDeployed = new List<ADebugUnitCreator>(existingFleetCreators.Count());
    //    if (qtyToDeploy > Constants.Zero) {

    //        SectorGrid sectorGrid = SectorGrid.Instance;
    //        var gameKnowledge = _gameMgr.GameKnowledge;
    //        Stack<IntVector3> sectorIDsToDeployTo = new Stack<IntVector3>(qtyToDeploy);
    //        D.Assert(_playersHomeSectorLookup.ContainsKey(player), "{0}: {1} has no assigned home system sector.", Name, player);
    //        IntVector3 homeSectorID = _playersHomeSectorLookup[player];

    //        ISystem unUsedSystem;
    //        if (!gameKnowledge.TryGetSystem(homeSectorID, out unUsedSystem)) {
    //            D.Error("{0} couldn't find a system in {1}'s home sector.", Name, player);
    //        }

    //        sectorIDsToDeployTo.Push(homeSectorID); // if start with fleet(s), always deploy one in the home sector
    //        if (qtyToDeploy > Constants.One) {
    //            // there are additional fleets to deploy around the home system
    //            IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
    //            foreach (var neighborSectorID in homeNeighborSectorIDs) {
    //                if (!gameKnowledge.TryGetSystem(neighborSectorID, out unUsedSystem)) {
    //                    sectorIDsToDeployTo.Push(neighborSectorID);
    //                    if (sectorIDsToDeployTo.Count == qtyToDeploy) {
    //                        break;
    //                    }
    //                }
    //            }

    //            if (sectorIDsToDeployTo.Count < qtyToDeploy) {
    //                D.Warn("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Fleets. Fixing.", Name, sectorIDsToDeployTo.Count, qtyToDeploy, player);
    //                foreach (var neighborSectorID in homeNeighborSectorIDs) {
    //                    if (gameKnowledge.TryGetSystem(neighborSectorID, out unUsedSystem)) {
    //                        sectorIDsToDeployTo.Push(neighborSectorID);
    //                        if (sectorIDsToDeployTo.Count == qtyToDeploy) {
    //                            break;
    //                        }
    //                    }
    //                }
    //                if (sectorIDsToDeployTo.Count < qtyToDeploy) {
    //                    D.Error("{0} only found {1} of the {2} sectors reqd to deploy all of {3}'s Fleets.", Name, sectorIDsToDeployTo.Count, qtyToDeploy, player);
    //                    // Unlikely to occur, but if it does, I'll need to find other sectors to add
    //                }
    //            }
    //        }

    //        bool toDeployAutoCreators = true;
    //        IntVector3 deployedSectorID;
    //        Vector3 deployedLocation;
    //        foreach (var debugCreator in existingFleetCreators) {
    //            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
    //                deployedSectorID = sectorIDsToDeployTo.Pop();
    //                deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
    //                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugFleetCreator, player, deployedLocation);

    //                debugCreatorsDeployed.Add(debugCreator);
    //                if (debugCreatorsDeployed.Count == qtyToDeploy) {
    //                    toDeployAutoCreators = false;
    //                    break;
    //                }
    //            }
    //        }

    //        if (toDeployAutoCreators) {
    //            D.Assert(debugCreatorsDeployed.Count < qtyToDeploy);
    //            D.Assert(sectorIDsToDeployTo.Count > Constants.Zero);

    //            for (int deployedCount = debugCreatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
    //                deployedSectorID = sectorIDsToDeployTo.Pop();
    //                deployedLocation = sectorGrid.GetSector(deployedSectorID).GetClearRandomPointInsideSector();
    //                UnitConfigurator.GenerateRandomAutoFleetCreator(player, deployedLocation);
    //            }
    //        }
    //        D.Assert(sectorIDsToDeployTo.Count == Constants.Zero);
    //    }
    //    //__ReportDeployedUnitCreators(qtyToDeploy, typeof(FleetCreator), player, debugCreatorsDeployed);
    //    return debugCreatorsDeployed;
    //}
    //private IList<ADebugUnitCreator> DeployAndConfigureInitialFleetCreators(Player player, IEnumerable<ADebugUnitCreator> existingFleetCreators, int qtyToDeploy) {
    //    IList<ADebugUnitCreator> debugCreatorsDeployed = new List<ADebugUnitCreator>(existingFleetCreators.Count());
    //    if (qtyToDeploy > Constants.Zero) {
    //        bool toDeployAutoCreators = true;
    //        Vector3 tgtLocation;
    //        foreach (var debugCreator in existingFleetCreators) {
    //            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
    //                tgtLocation = debugCreator.transform.position;
    //                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugFleetCreator, player, tgtLocation);

    //                debugCreatorsDeployed.Add(debugCreator);
    //                if (debugCreatorsDeployed.Count == qtyToDeploy) {
    //                    toDeployAutoCreators = false;
    //                    break;
    //                }
    //            }
    //        }

    //        if (toDeployAutoCreators) {
    //            D.Assert(debugCreatorsDeployed.Count < qtyToDeploy);
    //            SectorGrid sectorGrid = SectorGrid.Instance;

    //            for (int deployedCount = debugCreatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
    //                tgtLocation = sectorGrid.RandomSector.GetClearRandomPointInsideSector();
    //                UnitConfigurator.GenerateRandomAutoStarbaseCreator(player, tgtLocation);
    //            }
    //        }
    //    }

    //    //__ReportDeployedUnitCreators(qtyToDeploy, typeof(FleetCreator), player, debugCreatorsDeployed);
    //    return debugCreatorsDeployed;
    //}

    /// <summary>
    /// Deploys and configures the initial settlement creators for this player in and/or around the player's home sector. 
    /// Returns any of the provided existing DebugCreators that were deployed.
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
    private IList<AUnitCreator> DeployAndConfigureInitialSettlementCreators(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators, int qtyToDeploy, IList<ISystem> usedSystems) {
        IList<AUnitCreator> creatorsDeployed = new List<AUnitCreator>(qtyToDeploy);
        if (qtyToDeploy > Constants.Zero) {
            var gameKnowledge = _gameMgr.GameKnowledge;
            Stack<ISystem> systemsToDeployTo = new Stack<ISystem>(qtyToDeploy);
            D.Assert(_playersHomeSectorLookup.ContainsKey(player), "{0}: {1} has no assigned home system sector.", Name, player);
            IntVector3 homeSectorID = _playersHomeSectorLookup[player];

            ISystem system;
            if (!gameKnowledge.TryGetSystem(homeSectorID, out system)) {
                D.Error("{0} couldn't find a system in {1}'s home sector.", Name, player);
            }
            systemsToDeployTo.Push(system); // places home system on bottom but who cares?
            if (qtyToDeploy > Constants.One) {
                // there are additional settlements to deploy around the home system
                SectorGrid sectorGrid = SectorGrid.Instance;
                IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
                foreach (var neighborSectorID in homeNeighborSectorIDs) {
                    if (gameKnowledge.TryGetSystem(neighborSectorID, out system)) {
                        systemsToDeployTo.Push(system);
                        if (systemsToDeployTo.Count == qtyToDeploy) {
                            break;
                        }
                    }
                }
                D.Assert(systemsToDeployTo.Count == qtyToDeploy, "{0} only found {1} rather than {2} additional systems to deploy {3}'s Settlements.", Name, systemsToDeployTo.Count, qtyToDeploy, player);
            }

            bool toDeployAutoCreators = true;
            foreach (var debugCreator in existingSettlementCreators) {
                if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
                    system = systemsToDeployTo.Pop();
                    UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, system as SystemItem);
                    usedSystems.Add(system);

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
                    SettlementCreator autoCreator = UnitConfigurator.GenerateRandomAutoSettlementCreator(player, system as SystemItem);
                    creatorsDeployed.Add(autoCreator);
                    usedSystems.Add(system);
                }
            }
            D.Assert(systemsToDeployTo.Count == Constants.Zero);
        }

        __ReportDeployedUnitCreators(typeof(SettlementCreator), player, creatorsDeployed);
        return creatorsDeployed;
    }
    //private IList<ADebugUnitCreator> DeployAndConfigureInitialSettlementCreators(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators, int qtyToDeploy, IList<ISystem> usedSystems) {
    //    IList<ADebugUnitCreator> debugCreatorsDeployed = new List<ADebugUnitCreator>(existingSettlementCreators.Count());
    //    if (qtyToDeploy > Constants.Zero) {
    //        var gameKnowledge = _gameMgr.GameKnowledge;
    //        Stack<ISystem> systemsToDeployTo = new Stack<ISystem>(qtyToDeploy);
    //        D.Assert(_playersHomeSectorLookup.ContainsKey(player), "{0}: {1} has no assigned home system sector.", Name, player);
    //        IntVector3 homeSectorID = _playersHomeSectorLookup[player];

    //        ISystem system;
    //        if (!gameKnowledge.TryGetSystem(homeSectorID, out system)) {
    //            D.Error("{0} couldn't find a system in {1}'s home sector.", Name, player);
    //        }
    //        systemsToDeployTo.Push(system); // places home system on bottom but who cares?
    //        if (qtyToDeploy > Constants.One) {
    //            // there are additional settlements to deploy around the home system
    //            SectorGrid sectorGrid = SectorGrid.Instance;
    //            IEnumerable<IntVector3> homeNeighborSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
    //            foreach (var neighborSectorID in homeNeighborSectorIDs) {
    //                if (gameKnowledge.TryGetSystem(neighborSectorID, out system)) {
    //                    systemsToDeployTo.Push(system);
    //                    if (systemsToDeployTo.Count == qtyToDeploy) {
    //                        break;
    //                    }
    //                }
    //            }
    //            D.Assert(systemsToDeployTo.Count == qtyToDeploy, "{0} only found {1} rather than {2} additional systems to deploy {3}'s Settlements.", Name, systemsToDeployTo.Count, qtyToDeploy, player);
    //        }

    //        bool toDeployAutoCreators = true;
    //        foreach (var debugCreator in existingSettlementCreators) {
    //            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
    //                system = systemsToDeployTo.Pop();
    //                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, system as SystemItem);
    //                usedSystems.Add(system);

    //                debugCreatorsDeployed.Add(debugCreator);
    //                if (debugCreatorsDeployed.Count == qtyToDeploy) {
    //                    toDeployAutoCreators = false;
    //                    break;
    //                }
    //            }
    //        }

    //        if (toDeployAutoCreators) {
    //            D.Assert(debugCreatorsDeployed.Count < qtyToDeploy);
    //            D.Assert(systemsToDeployTo.Count > Constants.Zero);

    //            for (int deployedCount = debugCreatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
    //                system = systemsToDeployTo.Pop();
    //                UnitConfigurator.GenerateRandomAutoSettlementCreator(player, system as SystemItem);
    //                usedSystems.Add(system);
    //            }
    //        }
    //        D.Assert(systemsToDeployTo.Count == Constants.Zero);
    //    }

    //    //__ReportDeployedUnitCreators(qtyToDeploy, typeof(SettlementCreator), player, debugCreatorsDeployed);
    //    return debugCreatorsDeployed;
    //}
    //private IList<ADebugUnitCreator> DeployAndConfigureInitialSettlementCreators(Player player, IEnumerable<ADebugUnitCreator> existingSettlementCreators, int qtyToDeploy, IList<ISystem> usedSystems) {
    //    IList<ADebugUnitCreator> debugCreatorsDeployed = new List<ADebugUnitCreator>(existingSettlementCreators.Count());
    //    if (qtyToDeploy > Constants.Zero) {
    //        bool toDeployAutoCreators = true;
    //        ISystem tgtSystem;
    //        var gameKnowledge = _gameMgr.GameKnowledge;
    //        foreach (var debugCreator in existingSettlementCreators) {
    //            if (IsOwnerOfCreator(debugCreator.EditorSettings, player)) {
    //                if (!gameKnowledge.TryGetRandomSystem(usedSystems, out tgtSystem)) {
    //                    D.Error("{0} couldn't find a random system to deploy {1} for {2}.", Name, debugCreator.Name, player);
    //                    // Note: This shouldn't happen as I've made sure each UniverseSize/SystemDensity combo meets the min system rqmt
    //                    continue;
    //                }
    //                UnitConfigurator.AssignConfigurationToExistingCreator(debugCreator as DebugSettlementCreator, player, tgtSystem as SystemItem);
    //                usedSystems.Add(tgtSystem);

    //                debugCreatorsDeployed.Add(debugCreator);
    //                if (debugCreatorsDeployed.Count == qtyToDeploy) {
    //                    toDeployAutoCreators = false;
    //                    break;
    //                }
    //            }
    //        }

    //        if (toDeployAutoCreators) {
    //            D.Assert(debugCreatorsDeployed.Count < qtyToDeploy);

    //            for (int deployedCount = debugCreatorsDeployed.Count; deployedCount < qtyToDeploy; deployedCount++) {
    //                if (!gameKnowledge.TryGetRandomSystem(usedSystems, out tgtSystem)) {
    //                    // Note: This can occur when using SystemDensity.ExistingDebugCreators. It can be ignored
    //                    D.Warn("{0} cannot deploy sufficient {1}'s for {2} to meet requirement of {3}.", Name, typeof(SettlementCreator).Name, player, qtyToDeploy);
    //                    continue;
    //                }
    //                UnitConfigurator.GenerateRandomAutoSettlementCreator(player, tgtSystem as SystemItem);
    //                usedSystems.Add(tgtSystem);
    //            }
    //        }
    //    }

    //    //__ReportDeployedUnitCreators(qtyToDeploy, typeof(SettlementCreator), player, debugCreatorsDeployed);
    //    return debugCreatorsDeployed;
    //}

    private void __ReportDeployedUnitCreators(Type deployedType, Player player, IList<AUnitCreator> creatorsDeployed) {
        int qtyDeployed = creatorsDeployed.Count;
        int debugCreatorDeployedQty = creatorsDeployed.Select(c => c is ADebugUnitCreator).Count();
        int autoCreatorDeployedQty = qtyDeployed - debugCreatorDeployedQty;
        D.Log("{0} deployed {1} {2} for {3}. DebugCreators: {4}, AutoCreators: {5}.", Name, qtyDeployed, deployedType.Name, player, debugCreatorDeployedQty, autoCreatorDeployedQty);
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

    public void CommenceUnitOperationsOnDeployDate() {
        _unitCreators.ForAll(uc => uc.InitiateDeployment());
    }

    public void Reset() {
        SystemConfigurator.Reset();
        UnitConfigurator.Reset();
        UniverseCenter = null;
        _systemCreators = null;
        _unitCreators = null;
    }

    private int CalcUniverseSystemsQty(int nonPeripheralSectorQty) {
        int result;
        SystemDensity systemDensity = _gameMgr.GameSettings.SystemDensity;
        if (systemDensity == SystemDensity.Existing_Debug) {
            result = UniverseFolder.Instance.GetComponentsInChildren<DebugSystemCreator>().Count();
        }
        else {
            UniverseSize universeSize = _gameMgr.GameSettings.UniverseSize;
            result = Mathf.FloorToInt(nonPeripheralSectorQty * systemDensity.SystemsPerSector(universeSize));
            int minReqdSystemQty = universeSize.MinReqdSystemQty();
            if (result < minReqdSystemQty) {
                D.Warn("{0}: Calculated System Qty {1} < Min Reqd System Qty {2}. Correcting. SystemDensity = {3}, UniverseSize = {4}.",
                    Name, result, minReqdSystemQty, systemDensity.GetValueName(), universeSize.GetValueName());
                result = minReqdSystemQty;
            }
        }
        D.Log("{0} calculated a need for {1} Systems in a universe of {2} non-peripheral sectors.", Name, result, nonPeripheralSectorQty);
        return result;
    }

    //private int CalcUniverseSystemsQty(int nonPeripheralSectorQty) {
    //    SystemDensity systemDensity = _gameMgr.GameSettings.SystemDensity;
    //    if (systemDensity == SystemDensity.Existing_Debug) {
    //        return UniverseFolder.Instance.GetComponentsInChildren<DebugSystemCreator>().Count();
    //    }

    //    float densityMultiplierFromSize;
    //    UniverseSize universeSize = _gameMgr.GameSettings.UniverseSize;
    //    switch (universeSize) {
    //        case UniverseSize.Tiny:
    //            densityMultiplierFromSize = 6.0F;
    //            break;
    //        case UniverseSize.Small:
    //            densityMultiplierFromSize = 1.5F;
    //            break;
    //        case UniverseSize.Normal:
    //            densityMultiplierFromSize = 1.0F;
    //            break;
    //        case UniverseSize.Large:
    //            densityMultiplierFromSize = 0.9F;
    //            break;
    //        case UniverseSize.Enormous:
    //            densityMultiplierFromSize = 0.8F;
    //            break;
    //        case UniverseSize.Gigantic:
    //            densityMultiplierFromSize = 0.7F;
    //            break;
    //        case UniverseSize.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(universeSize));
    //    }

    //    int calculatedSystemQty = Mathf.CeilToInt(nonPeripheralSectorQty * systemDensity.SystemsPerSector() * densityMultiplierFromSize);
    //    int minReqdSystemQty = _gameMgr.GameSettings.UniverseSize.MinReqdSystemQty();
    //    if (calculatedSystemQty < minReqdSystemQty) {
    //        D.Warn("{0}: Calculated System Qty {1} < Min Reqd System Qty {2}. Correcting. SystemDensity = {3}, UniverseSize = {4}.",
    //            Name, calculatedSystemQty, minReqdSystemQty, systemDensity.GetValueName(), universeSize.GetValueName());
    //        return minReqdSystemQty;
    //    }
    //    return calculatedSystemQty;
    //}


    private FocusableItemCameraStat __MakeUCenterCameraStat(float radius, float closeOrbitInnerRadius) {
        float minViewDistance = radius + 1F;
        float closeOrbitOuterRadius = closeOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        float optViewDistance = closeOrbitOuterRadius + 1F;
        return new FocusableItemCameraStat(minViewDistance, optViewDistance, fov: 80F);
    }

    #region Debug

    /// <summary>
    /// Gets the AIPlayers that the User has not yet met, that have been assigned the initialUserRelationship to begin with when they do meet.
    /// </summary>
    /// <param name="initialUserRelationship">The initial user relationship.</param>
    /// <returns></returns>
    public IEnumerable<Player> __GetUnmetAiPlayersWithInitialUserRelationsOf(DiplomaticRelationship initialUserRelationship) {
        D.Assert(_gameMgr.IsRunning, "This method should only be called when the User manually changes a unit's user relationship in the editor.");
        Player userPlayer = _gameMgr.UserPlayer;
        IList<Player> aiPlayersWithSpecifiedInitialUserRelations;
        if (_aiPlayerInitialUserRelationsLookup.TryGetValue(initialUserRelationship, out aiPlayersWithSpecifiedInitialUserRelations)) {
            return aiPlayersWithSpecifiedInitialUserRelations.Except(userPlayer.OtherKnownPlayers);
        }
        return Enumerable.Empty<Player>();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

