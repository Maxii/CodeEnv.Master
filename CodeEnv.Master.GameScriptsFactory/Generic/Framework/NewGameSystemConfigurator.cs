﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameSystemConfigurator.cs
// Generates and/or configure SystemCreators. 
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
/// Generates and/or configure SystemCreators. Existing DebugSystemCreators that are already present in the editor
/// just need to be configured by accessing their EditorSettings. Auto SystemCreators are both generated and then randomly configured.
/// </summary>
public class NewGameSystemConfigurator {

    private const string DebugCreatorNameFormat = "{0}({1})";

    private const string DesignNameFormat = "{0}_{1}";

    /// <summary>
    /// Static counter used to provide a unique name for each design name.
    /// </summary>
    private static int _designNameCounter = Constants.One;

    /// <summary>
    /// Gets a unique design name.
    /// </summary>
    /// <param name="categoryName">The hull category name.</param>
    /// <returns></returns>
    private static string GetUniqueDesignName(string categoryName) {
        var designName = DesignNameFormat.Inject(categoryName, _designNameCounter);
        _designNameCounter++;
        return designName;
    }

    private static IEnumerable<PlanetoidCategory> _desirablePlanetCategories = new PlanetoidCategory[] {
        PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Terrestrial
    };

    private static IEnumerable<PlanetoidCategory> _acceptablePlanetCategories = new PlanetoidCategory[] {
        PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Terrestrial, PlanetoidCategory.Volcanic
    };

    private static IEnumerable<PlanetoidCategory> _undesirablePlanetCategories = new PlanetoidCategory[] {
        PlanetoidCategory.GasGiant, PlanetoidCategory.Ice, PlanetoidCategory.Volcanic
    };

    private static IEnumerable<PlanetoidCategory> _acceptableLargePlanetMoonCategories = new PlanetoidCategory[] {
        PlanetoidCategory.Moon_001, PlanetoidCategory.Moon_002, PlanetoidCategory.Moon_003, PlanetoidCategory.Moon_004, PlanetoidCategory.Moon_005
    };

    private static IEnumerable<PlanetoidCategory> _acceptableMediumPlanetMoonCategories = new PlanetoidCategory[] {
        PlanetoidCategory.Moon_001, PlanetoidCategory.Moon_002, PlanetoidCategory.Moon_003, PlanetoidCategory.Moon_004
    };

    private static IEnumerable<PlanetoidCategory> _acceptableSmallPlanetMoonCategories = new PlanetoidCategory[] {
        PlanetoidCategory.Moon_001, PlanetoidCategory.Moon_002, PlanetoidCategory.Moon_003
    };

    private string Name { get { return GetType().Name; } }

    private GameTimeDuration _minSystemOrbitPeriod = GameTimeDuration.OneYear;
    private GameTimeDuration _systemOrbitPeriodIncrement = new GameTimeDuration(hours: 0, days: GameTime.DaysPerYear / 2, years: 0);
    private GameTimeDuration _minMoonOrbitPeriod = new GameTimeDuration(hours: 0, days: 30, years: 0);
    private GameTimeDuration _moonOrbitPeriodIncrement = new GameTimeDuration(hours: 0, days: 10, years: 0);

    private IList<PassiveCountermeasureStat> _availablePassiveCountermeasureStats;
    private float __systemOrbitSlotDepth;

    private GameManager _gameMgr;
    private SystemNameFactory _nameFactory;
    private SystemFactory _systemFactory;

    public NewGameSystemConfigurator() {
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _nameFactory = SystemNameFactory.Instance;
        _systemFactory = SystemFactory.Instance;
        _availablePassiveCountermeasureStats = MakeAvailablePassiveCountermeasureStats(9);
    }

    #region Configure Existing Creators

    [Obsolete]
    public List<SystemCreator> DeployAndConfigurePlayersStartingSystemCreators(GameSettings gameSettings) {
        var sectorGrid = SectorGrid.Instance;

        var userStartLevel = gameSettings.UserStartLevel;
        var aiPlayerStartLevels = gameSettings.AIPlayersStartLevel;

        int systemQtyToDeploy = userStartLevel.SettlementStartQty() > 0 ? userStartLevel.SettlementStartQty() : 1;
        aiPlayerStartLevels.ForAll(sl => {
            systemQtyToDeploy += sl.SettlementStartQty() > 0 ? sl.SettlementStartQty() : 1;
        });
        List<SystemCreator> deployedCreators = new List<SystemCreator>(systemQtyToDeploy);

        // Handle User first
        Player userPlayer = gameSettings.UserPlayer;
        SystemDesirability homeSystemDesirability = gameSettings.UserHomeSystemDesirability;
        string homeSystemName = _nameFactory.GetHomeSystemNameFor(userPlayer);
        StarCategory homeStarCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability
        IList<PlanetoidCategory> homePlanetCatsByPlanetIndex = GetPlanetCategories(homeSystemDesirability, isHomeSystem: true);
        var userHomeSystemSectorIndex = sectorGrid.GetRandomSectorIndex();
        Vector3 homeLocation = sectorGrid.GetSectorPosition(userHomeSystemSectorIndex);

        var deployedHomeCreator = DeployAndConfigureCreatorTo(homeSystemName, homeLocation, homeSystemDesirability, homeStarCat, homePlanetCatsByPlanetIndex);
        deployedCreators.Add(deployedHomeCreator);

        var additionalCreatorsDeployed = DeployAndConfigureAdditionalCreatorsAround(homeLocation, userStartLevel);
        deployedCreators.AddRange(additionalCreatorsDeployed);

        PlayerSeparation aiPlayerSeparationFromUser;
        IEnumerable<IntVector3> otherOccupiedSectorIndices;
        int universeRadiusInSectors = gameSettings.UniverseSize.RadiusInSectors();
        Player[] aiPlayers = gameSettings.AIPlayers;
        for (int i = 0; i < aiPlayers.Length; i++) {
            var aiPlayer = aiPlayers[i];
            var aiPlayerStartLevel = aiPlayerStartLevels[i];
            homeSystemDesirability = gameSettings.AIPlayersHomeSystemDesirability[i];
            homeSystemName = _nameFactory.GetHomeSystemNameFor(aiPlayer);
            homeStarCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability
            homePlanetCatsByPlanetIndex = GetPlanetCategories(homeSystemDesirability, isHomeSystem: true);

            aiPlayerSeparationFromUser = gameSettings.AIPlayersSeparationFromUser[i];
            otherOccupiedSectorIndices = deployedCreators.Select(c => c.SectorIndex);
            homeLocation = CalcAiPlayerHomeLocation(userHomeSystemSectorIndex, otherOccupiedSectorIndices, universeRadiusInSectors, aiPlayerSeparationFromUser);

            deployedHomeCreator = DeployAndConfigureCreatorTo(homeSystemName, homeLocation, homeSystemDesirability, homeStarCat, homePlanetCatsByPlanetIndex);
            deployedCreators.Add(deployedHomeCreator);

            additionalCreatorsDeployed = DeployAndConfigureAdditionalCreatorsAround(homeLocation, aiPlayerStartLevel);
            deployedCreators.AddRange(additionalCreatorsDeployed);
        }
        return deployedCreators;
    }

    /// <summary>
    /// Deploys and configures each player's home system creator.
    /// <remarks>This does not mean the systems generated by these creators will become
    /// the player's home system as the player can start with only a fleet and pick another
    /// system as its home. If the player starts with one or more settlements, one of the settlements
    /// will be assigned the player's home system created by this method.</remarks>
    /// </summary>
    /// <param name="gameSettings">The game settings.</param>
    /// <param name="sectorIDsToAvoid">The sector IDs to avoid.</param>
    /// <returns></returns>
    public IDictionary<Player, SystemCreator> DeployAndConfigurePlayersHomeSystemCreators(GameSettings gameSettings, IEnumerable<IntVector3> sectorIDsToAvoid) {
        var sectorGrid = SectorGrid.Instance;

        int homeSystemQtyToDeploy = gameSettings.PlayerCount;
        IDictionary<Player, SystemCreator> deployedCreatorLookup = new Dictionary<Player, SystemCreator>(homeSystemQtyToDeploy);
        List<IntVector3> sectorIDsToAvoid_Internal = new List<IntVector3>(sectorIDsToAvoid);

        // Handle User first
        Player userPlayer = gameSettings.UserPlayer;
        SystemDesirability homeSystemDesirability = gameSettings.UserHomeSystemDesirability;
        string homeSystemName = _nameFactory.GetUnusedHomeSystemNameFor(userPlayer);
        StarCategory homeStarCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability
        IList<PlanetoidCategory> homePlanetCatsByPlanetIndex = GetPlanetCategories(homeSystemDesirability, isHomeSystem: true);
        var userHomeSystemSectorID = sectorGrid.GetRandomSectorIndex(includePeriphery: false, excludedIndices: sectorIDsToAvoid_Internal);
        sectorIDsToAvoid_Internal.Add(userHomeSystemSectorID);

        // IMPROVE This neighbor addition is superfluous as CalcAiPlayerHomeSector() requires sectorCandidates to be > 2 sectors away from userHomeSystemSector
        var homeNeighboringSectorIDs = sectorGrid.GetNeighboringIndices(userHomeSystemSectorID);
        sectorIDsToAvoid_Internal.AddRange(homeNeighboringSectorIDs);

        Vector3 homeLocation = sectorGrid.GetSectorPosition(userHomeSystemSectorID);
        var deployedHomeCreator = DeployAndConfigureCreatorTo(homeSystemName, homeLocation, homeSystemDesirability, homeStarCat, homePlanetCatsByPlanetIndex);
        deployedCreatorLookup.Add(userPlayer, deployedHomeCreator);

        IntVector3 homeSectorID;
        PlayerSeparation aiPlayerSeparationFromUser;
        int universeRadiusInSectors = gameSettings.UniverseSize.RadiusInSectors();
        Player[] aiPlayers = gameSettings.AIPlayers;
        for (int i = 0; i < aiPlayers.Length; i++) {
            var aiPlayer = aiPlayers[i];
            homeSystemDesirability = gameSettings.AIPlayersHomeSystemDesirability[i];
            homeSystemName = _nameFactory.GetUnusedHomeSystemNameFor(aiPlayer);
            homeStarCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability
            homePlanetCatsByPlanetIndex = GetPlanetCategories(homeSystemDesirability, isHomeSystem: true);

            aiPlayerSeparationFromUser = gameSettings.AIPlayersSeparationFromUser[i];
            homeSectorID = CalcAiPlayerHomeSectorID(userHomeSystemSectorID, sectorIDsToAvoid_Internal, universeRadiusInSectors, aiPlayerSeparationFromUser);
            sectorIDsToAvoid_Internal.Add(homeSectorID);

            homeLocation = sectorGrid.GetSectorPosition(homeSectorID);
            deployedHomeCreator = DeployAndConfigureCreatorTo(homeSystemName, homeLocation, homeSystemDesirability, homeStarCat, homePlanetCatsByPlanetIndex);
            deployedCreatorLookup.Add(aiPlayer, deployedHomeCreator);

            // This keeps other homeSystemSectors from being placed right next to this homeSystemSector where 
            // DeployAndConfigureAdditionalCreatorsAround() may choose to place additional systems
            homeNeighboringSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
            sectorIDsToAvoid_Internal.AddRange(homeNeighboringSectorIDs);
        }
        D.Log("{0} deployed {1} Home {2}s. Names: {3}.", Name, deployedCreatorLookup.Count, typeof(SystemCreator).Name,
            deployedCreatorLookup.Values.Select(c => c.SystemName).Concatenate());
        return deployedCreatorLookup;
    }

    private IntVector3 CalcAiPlayerHomeSectorID(IntVector3 userHomeSectorID, IEnumerable<IntVector3> sectorIDsToAvoid, int universeRadiusInSectors, PlayerSeparation separationFromUser) {
        SectorGrid sectorGrid = SectorGrid.Instance;

        float userHomeSectorDistanceFromOrigin = sectorGrid.GetDistanceInSectorsFromOrigin(userHomeSectorID);
        int maxSectorDistanceToUniverseEdge = universeRadiusInSectors + Mathf.FloorToInt(userHomeSectorDistanceFromOrigin);
        int closestAllowedSectorDistanceToUserHome = 2;  // in case userHome has surrounding systems
        int maxClosestAdder = maxSectorDistanceToUniverseEdge - closestAllowedSectorDistanceToUserHome;
        D.Assert(maxClosestAdder >= 1);

        int minSectorDistanceFromUserHome;
        int maxSectorDistanceFromUserHome;

        if (separationFromUser == PlayerSeparation.Close) {
            minSectorDistanceFromUserHome = closestAllowedSectorDistanceToUserHome;
            maxSectorDistanceFromUserHome = closestAllowedSectorDistanceToUserHome + Mathf.CeilToInt(maxClosestAdder * Constants.OneThird);
        }
        else if (separationFromUser == PlayerSeparation.Normal) {
            minSectorDistanceFromUserHome = closestAllowedSectorDistanceToUserHome + Mathf.FloorToInt(maxClosestAdder * Constants.OneThird);
            maxSectorDistanceFromUserHome = closestAllowedSectorDistanceToUserHome + Mathf.CeilToInt(maxClosestAdder * Constants.TwoThirds);
        }
        else {
            D.Assert(separationFromUser == PlayerSeparation.Distant);
            minSectorDistanceFromUserHome = closestAllowedSectorDistanceToUserHome + Mathf.FloorToInt(maxClosestAdder * Constants.TwoThirds);
            maxSectorDistanceFromUserHome = closestAllowedSectorDistanceToUserHome + maxClosestAdder;
        }

        IEnumerable<IntVector3> candidateSectorIDs = sectorGrid.GetSurroundingIndicesBetween(userHomeSectorID, minSectorDistanceFromUserHome, maxSectorDistanceFromUserHome);
        D.Assert(candidateSectorIDs.Any(), "{0} could get no surrounding sectors around {1} between distances {2} and {3}.", Name, userHomeSectorID, minSectorDistanceFromUserHome, maxSectorDistanceFromUserHome);
        var tempCandidateSectorIDs = candidateSectorIDs;
        candidateSectorIDs = candidateSectorIDs.Except(sectorIDsToAvoid);
        D.Assert(candidateSectorIDs.Any(), "{0} found no sectors to place AIPlayer's home sector. Candidates = {1}, ToAvoid = {2}.",
            Name, tempCandidateSectorIDs.Concatenate(), sectorIDsToAvoid.Concatenate());
        var homeSectorID = RandomExtended.Choice(candidateSectorIDs);
        return homeSectorID;
    }
    //private IntVector3 CalcAiPlayerHomeSectorID(IntVector3 userHomeSectorID, IEnumerable<IntVector3> sectorIDsToAvoid, int universeRadiusInSectors, PlayerSeparation separationFromUser) {
    //    SectorGrid sectorGrid = SectorGrid.Instance;

    //    float userHomeSectorDistanceFromOrigin = sectorGrid.GetDistanceInSectorsFromOrigin(userHomeSectorID);
    //    int maxSectorDistanceToUniverseEdge = universeRadiusInSectors + Mathf.FloorToInt(userHomeSectorDistanceFromOrigin);
    //    int closestAllowedSectorDistanceToUserHome = 2;  // in case userHome has surrounding systems
    //    int maxAdderChoice = maxSectorDistanceToUniverseEdge - closestAllowedSectorDistanceToUserHome;
    //    D.Assert(maxAdderChoice >= 1);

    //    int lowAdderChoice;
    //    int highAdderChoice;

    //    if (separationFromUser == PlayerSeparation.Close) {
    //        lowAdderChoice = 0;
    //        highAdderChoice = Mathf.CeilToInt(maxAdderChoice * Constants.OneThird);
    //    }
    //    else if (separationFromUser == PlayerSeparation.Normal) {
    //        lowAdderChoice = Mathf.FloorToInt(maxAdderChoice * Constants.OneThird);
    //        highAdderChoice = Mathf.CeilToInt(maxAdderChoice * Constants.TwoThirds);
    //    }
    //    else {
    //        D.Assert(separationFromUser == PlayerSeparation.Distant);
    //        lowAdderChoice = Mathf.FloorToInt(maxAdderChoice * Constants.TwoThirds);
    //        highAdderChoice = maxAdderChoice;
    //    }

    //    int adderChoice = RandomExtended.Range(lowAdderChoice, highAdderChoice);

    //    int sectorDistanceToUserHome = closestAllowedSectorDistanceToUserHome + adderChoice;

    //    IEnumerable<IntVector3> candidateSectorIDs = sectorGrid.GetNeighboringIndices(userHomeSectorID, sectorDistanceToUserHome);
    //    D.Assert(candidateSectorIDs.Any(), "{0} could get no neighboring sectors around {1} at distance {2}.", Name, userHomeSectorID, sectorDistanceToUserHome);
    //    var tempCandidateSectorIDs = candidateSectorIDs;
    //    candidateSectorIDs = candidateSectorIDs.Except(sectorIDsToAvoid);
    //    D.Assert(candidateSectorIDs.Any(), "{0} found no sectors to place AIPlayer's home sector. Candidates = {1}, ToAvoid = {2}.",
    //        Name, tempCandidateSectorIDs.Concatenate(), sectorIDsToAvoid.Concatenate());
    //    var homeSectorID = RandomExtended.Choice(candidateSectorIDs);
    //    return homeSectorID;
    //}

    [Obsolete]
    private Vector3 CalcAiPlayerHomeLocation(IntVector3 userHomeSectorIndex, IEnumerable<IntVector3> otherOccupiedSectorIndices, int universeRadiusInSectors, PlayerSeparation separationFromUser) {
        SectorGrid sectorGrid = SectorGrid.Instance;

        float userHomeSystemSectorsFromOrigin = sectorGrid.GetDistanceInSectorsFromOrigin(userHomeSectorIndex);
        int maxSectorsToUniverseEdge = universeRadiusInSectors + Mathf.FloorToInt(userHomeSystemSectorsFromOrigin);
        int closestAllowedDistanceInSectorsToUserHome = 2;  // in case userHome has surrounding systems
        int maxAdderChoice = maxSectorsToUniverseEdge - closestAllowedDistanceInSectorsToUserHome;
        D.Assert(maxAdderChoice >= 1);

        int lowAdderChoice;
        int highAdderChoice;

        if (separationFromUser == PlayerSeparation.Close) {
            lowAdderChoice = 0;
            highAdderChoice = Mathf.CeilToInt(maxAdderChoice * Constants.OneThird);
        }
        else if (separationFromUser == PlayerSeparation.Normal) {
            lowAdderChoice = Mathf.FloorToInt(maxAdderChoice * Constants.OneThird);
            highAdderChoice = Mathf.CeilToInt(maxAdderChoice * Constants.TwoThirds);
        }
        else {
            D.Assert(separationFromUser == PlayerSeparation.Distant);
            lowAdderChoice = Mathf.FloorToInt(maxAdderChoice * Constants.TwoThirds);
            highAdderChoice = maxAdderChoice;
        }

        int adderChoice = RandomExtended.Range(lowAdderChoice, highAdderChoice);

        int distanceInSectorsFromUserHome = closestAllowedDistanceInSectorsToUserHome + adderChoice;

        IEnumerable<IntVector3> candidateIndices = null; // = sectorGrid.GetNeighboringIndices(userHomeSectorIndex, distanceInSectorsFromUserHome);
        candidateIndices = candidateIndices.Except(otherOccupiedSectorIndices);
        var index = RandomExtended.Choice(candidateIndices);
        return sectorGrid.GetSectorPosition(index);
    }

    /// <summary>
    /// Deploys and configures any additional creators around the player's homeLocation
    /// if startLevel indicates there will be more player owned settlements.
    /// </summary>
    /// <param name="homeLocation">The home location.</param>
    /// <param name="startLevel">The start level.</param>
    /// <returns></returns>
    [Obsolete]
    private IEnumerable<SystemCreator> DeployAndConfigureAdditionalCreatorsAround(Vector3 homeLocation, EmpireStartLevel startLevel) {
        int additionalCreatorQtyToDeploy = startLevel.SettlementStartQty() - 1;
        if (additionalCreatorQtyToDeploy < 1) {
            return Enumerable.Empty<SystemCreator>();
        }

        var deployedCreators = new List<SystemCreator>(additionalCreatorQtyToDeploy);
        SectorGrid sectorGrid = SectorGrid.Instance;
        var homeSectorIndex = sectorGrid.GetSectorIndexThatContains(homeLocation);
        var sectorIndicesSurroundingHome = sectorGrid.GetNeighboringIndices(homeSectorIndex);
        var sectorIndicesToDeployTo = sectorIndicesSurroundingHome.Shuffle().Take(additionalCreatorQtyToDeploy);

        int[] systemDesirabilityWeighting = new int[] { 1, 3, 1 };
        SystemDesirability[] systemDesirabilityChoices = Enums<SystemDesirability>.GetValues(excludeDefault: true).ToArray();
        foreach (var index in sectorIndicesToDeployTo) {
            SystemDesirability systemDesirability = RandomExtended.WeightedChoice(systemDesirabilityChoices, systemDesirabilityWeighting);
            string systemName = _nameFactory.GetUnusedName();
            StarCategory starCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability
            IList<PlanetoidCategory> planetCatsByPlanetIndex = GetPlanetCategories(systemDesirability, isHomeSystem: false);
            Vector3 location = sectorGrid.GetSectorPosition(index);
            SystemCreator creator = DeployAndConfigureCreatorTo(systemName, location, systemDesirability, starCat, planetCatsByPlanetIndex);
            deployedCreators.Add(creator);
        }
        return deployedCreators;
    }

    public IEnumerable<SystemCreator> DeployAndConfigureAdditionalCreatorsAround(IntVector3 homeSectorID, EmpireStartLevel startLevel) {
        int additionalCreatorQtyToDeploy = startLevel.SettlementStartQty() - 1;
        if (additionalCreatorQtyToDeploy < 1) {
            return Enumerable.Empty<SystemCreator>();
        }

        var deployedCreators = new List<SystemCreator>(additionalCreatorQtyToDeploy);
        SectorGrid sectorGrid = SectorGrid.Instance;
        var homeNeighboringSectorIDs = sectorGrid.GetNeighboringIndices(homeSectorID);
        var sectorIDsToDeployTo = homeNeighboringSectorIDs.Shuffle().Take(additionalCreatorQtyToDeploy);
        D.Assert(additionalCreatorQtyToDeploy == sectorIDsToDeployTo.Count());  // was there a shortage of sectorIndicesSurroundingHome?

        int[] systemDesirabilityWeighting = new int[] { 1, 3, 1 };
        SystemDesirability[] systemDesirabilityChoices = Enums<SystemDesirability>.GetValues(excludeDefault: true).ToArray();
        foreach (var deployedSectorID in sectorIDsToDeployTo) {
            SystemDesirability systemDesirability = RandomExtended.WeightedChoice(systemDesirabilityChoices, systemDesirabilityWeighting);
            string systemName = _nameFactory.GetUnusedName();
            StarCategory starCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability
            IList<PlanetoidCategory> planetCatsByPlanetIndex = GetPlanetCategories(systemDesirability, isHomeSystem: false);
            Vector3 deployedLocation = sectorGrid.GetSectorPosition(deployedSectorID);
            SystemCreator creator = DeployAndConfigureCreatorTo(systemName, deployedLocation, systemDesirability, starCat, planetCatsByPlanetIndex);
            deployedCreators.Add(creator);
        }
        return deployedCreators;
    }


    /// <summary>
    /// Configures the existing debug creators, destroying any present that exceed the allowedSystemQty.
    /// </summary>
    /// <param name="allowedQty">The allowed system qty.</param>
    /// <returns></returns>
    public IList<DebugSystemCreator> ConfigureExistingDebugCreators(int allowedQty, IEnumerable<IntVector3> sectorIDsToAvoid) {
        var existingDebugCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugSystemCreator>();
        D.Log("{0} found {1} existing {2}s to contribute to populating {3} remaining Systems.", Name, existingDebugCreators.Count(), typeof(DebugSystemCreator).Name, allowedQty);

        List<DebugSystemCreator> deployedCreators;
        List<DebugSystemCreator> creatorsToDestroy;
        if (allowedQty == 0) {
            deployedCreators = new List<DebugSystemCreator>(0);
            creatorsToDestroy = new List<DebugSystemCreator>(existingDebugCreators);
        }
        else {
            int existingCreatorQty = existingDebugCreators.Count();
            deployedCreators = new List<DebugSystemCreator>(existingCreatorQty);
            creatorsToDestroy = new List<DebugSystemCreator>(existingCreatorQty);

            existingDebugCreators.ForAll(ec => {
                if (sectorIDsToAvoid.Contains(ec.SectorIndex)) {
                    creatorsToDestroy.Add(ec);
                }
            });

            deployedCreators.AddRange(existingDebugCreators.Except(creatorsToDestroy).Take(allowedQty));

            var unhandledDebugCreators = existingDebugCreators.Except(deployedCreators).Except(creatorsToDestroy);
            if (unhandledDebugCreators.Any()) {
                creatorsToDestroy.AddRange(unhandledDebugCreators);
            }
        }

        deployedCreators.ForAll(c => {
            string existingSystemName = c.SystemName;
            string newSystemName = DebugCreatorNameFormat.Inject(_nameFactory.GetUnusedName(), existingSystemName);
            AssignConfigurationToExistingDebugCreator(c, newSystemName);
        });

        creatorsToDestroy.ForAll(c => {
            D.Log("{0} is about to destroy excess Creator {1}.", Name, c.Name);
            GameUtility.Destroy(c.gameObject);
        });

        return deployedCreators;
    }
    //public IEnumerable<DebugSystemCreator> ConfigureExistingDebugCreators(int allowedSystemQty) {
    //    var existingCreators = UniverseFolder.Instance.GetComponentsInChildren<DebugSystemCreator>();
    //    D.Log("{0} found {1} existing {2}s to contribute to populating {3} Systems.", Name, existingCreators.Count(), typeof(DebugSystemCreator).Name, allowedSystemQty);
    //    // AHA! OrderBy(bool) actually alphabetically orders rather than true before false...
    //    var orderedCreators = existingCreators.OrderByDescending(sc => _nameFactory.IsSystemProperlyNamed(sc.SystemName));
    //    //D.Log("{0} orderedCreators = {1}.", Name, orderedCreators.Select(c => c.Name).Concatenate());

    //    var allowedCreators = orderedCreators.Take(allowedSystemQty);
    //    //D.Log("{0} allowedCreators = {1}.", Name, allowedCreators.Select(c => c.Name).Concatenate());
    //    var properlyNamedAllowedCreators = allowedCreators.Where(sc => _nameFactory.IsSystemProperlyNamed(sc.SystemName));
    //    //D.Log("{0} properlyNamedAllowedCreators = {1}.", Name, properlyNamedAllowedCreators.Select(c => c.Name).Concatenate());
    //    properlyNamedAllowedCreators.ForAll(sc => {
    //        _nameFactory.MarkNameAsUsed(sc.SystemName);
    //        AssignConfigurationToExistingDebugCreator(sc, sc.SystemName);
    //    });

    //    var unNamedAllowedCreators = allowedCreators.Except(properlyNamedAllowedCreators);
    //    //D.Log("{0} unNamedAllowedCreators = {1}.", Name, unNamedAllowedCreators.Select(c => c.Name).Concatenate());
    //    unNamedAllowedCreators.ForAll(sc => AssignConfigurationToExistingDebugCreator(sc, _nameFactory.GetUnusedName()));

    //    var unallowedCreators = existingCreators.Except(allowedCreators);
    //    unallowedCreators.ForAll(c => {
    //        //D.Log("{0} is about to destroy excess unAllowedCreator {1}.", Name, c.Name);
    //        GameUtility.Destroy(c.gameObject);
    //    });

    //    return allowedCreators;
    //}

    /// <summary>
    /// Assigns a configuration to an existing DebugSystemCreator. The configuration is generated from
    /// the existing creator's EditorSettings.
    /// </summary>
    /// <param name="creator">The creator.</param>
    /// <returns></returns>
    private void AssignConfigurationToExistingDebugCreator(DebugSystemCreator creator, string systemName) {
        StarCategory starCat;
        IList<PlanetoidCategory[]> moonCatsByPlanetIndex;
        IList<PlanetoidCategory> planetCatsByPlanetIndex;

        var editorSettings = creator.EditorSettings;
        if (editorSettings.IsCompositionPreset) {
            starCat = editorSettings.PresetStarCategory;
            planetCatsByPlanetIndex = editorSettings.PresetPlanetCategories;
            moonCatsByPlanetIndex = editorSettings.PresetMoonCategories;
        }
        else {
            starCat = Enums<StarCategory>.GetRandom(excludeDefault: true);
            planetCatsByPlanetIndex = RandomExtended.Choices(_acceptablePlanetCategories, editorSettings.NonPresetPlanetQty);
            moonCatsByPlanetIndex = GetRandomMoonCats(planetCatsByPlanetIndex);
        }

        SystemDesirability systemDesirability = editorSettings.Desirability;

        float systemOrbitSlotsStartRadius;
        string starDesignName = MakeAndRecordStarDesign(starCat, systemDesirability, out systemOrbitSlotsStartRadius);

        Stack<OrbitData> innerOrbitSlots;
        Stack<OrbitData> goldilocksOrbitSlots;
        Stack<OrbitData> outerOrbitSlots;
        GenerateSystemOrbitSlots(systemOrbitSlotsStartRadius, out innerOrbitSlots, out goldilocksOrbitSlots, out outerOrbitSlots);
        OrbitData settlementOrbitSlot = goldilocksOrbitSlots.Pop();
        D.Assert(settlementOrbitSlot != null);

        IList<int> unassignedPlanetIndices;
        IList<OrbitData> planetOrbitSlots;
        IList<string> planetDesignNames = MakePlanetDesignsAndOrbitSlots(planetCatsByPlanetIndex, systemDesirability, innerOrbitSlots, goldilocksOrbitSlots, outerOrbitSlots, out planetOrbitSlots, out unassignedPlanetIndices);

        if (unassignedPlanetIndices.Count > Constants.Zero) {
            // one or more planets could not be assigned an orbit slot and design so eliminate any moons for that planet
            foreach (var pIndex in unassignedPlanetIndices) {
                moonCatsByPlanetIndex[pIndex] = Enumerable.Empty<PlanetoidCategory>().ToArray();
                planetCatsByPlanetIndex[pIndex] = default(PlanetoidCategory);
            }
        }
        IList<OrbitData[]> moonOrbitSlots;
        IList<string[]> moonDesignNames = MakeMoonDesignsAndOrbitSlots(planetCatsByPlanetIndex, moonCatsByPlanetIndex, systemDesirability, out moonOrbitSlots);

        bool enableTrackingLabel = editorSettings.IsTrackingLabelEnabled;
        SystemCreatorConfiguration config = new SystemCreatorConfiguration(systemName, starDesignName, settlementOrbitSlot, planetDesignNames, planetOrbitSlots, moonDesignNames, moonOrbitSlots, enableTrackingLabel);
        creator.Configuration = config;
        //D.Log("{0} assigned a configuration to {1}.", Name, creator.Name);
    }

    #endregion

    /// <summary>
    /// Gets the planet categories based on SystemDesirability.
    /// IMPROVE categories should vary by species.
    /// </summary>
    /// <param name="systemDesirability">The system desirability.</param>
    /// <param name="isHomeSystem">if set to <c>true</c> [is home system].</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private IList<PlanetoidCategory> GetPlanetCategories(SystemDesirability systemDesirability, bool isHomeSystem = false) {
        int minPlanetQty;
        IEnumerable<PlanetoidCategory> planetCatsToChooseFrom;

        switch (systemDesirability) {
            case SystemDesirability.Desirable:
                minPlanetQty = isHomeSystem ? 4 : 3;
                planetCatsToChooseFrom = _desirablePlanetCategories;
                break;
            case SystemDesirability.Normal:
                minPlanetQty = isHomeSystem ? 3 : 1;
                planetCatsToChooseFrom = _acceptablePlanetCategories;
                break;
            case SystemDesirability.Challenged:
                minPlanetQty = isHomeSystem ? 2 : 0;
                planetCatsToChooseFrom = _undesirablePlanetCategories;
                break;
            case SystemDesirability.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(systemDesirability));
        }
        int planetQty = RandomExtended.Range(minPlanetQty, TempGameValues.TotalOrbitSlotsPerSystem - 1);
        IList<PlanetoidCategory> planetCatsByPlanetIndex = RandomExtended.Choices<PlanetoidCategory>(planetCatsToChooseFrom, planetQty);
        return planetCatsByPlanetIndex;
    }

    #region Generate Random AutoCreators

    public SystemCreator DeployAndConfigureRandomSystemCreatorTo(Vector3 location) {
        string systemName = _nameFactory.GetUnusedName();

        SystemDesirability[] desirabilityChoices = Enums<SystemDesirability>.GetValues(excludeDefault: true).ToArray();
        int[] desirabilityWeight = new int[] { 1, 3, 1 };
        SystemDesirability systemDesirability = RandomExtended.WeightedChoice(desirabilityChoices, desirabilityWeight);

        StarCategory starCat = Enums<StarCategory>.GetRandom(excludeDefault: true); // IMPROVE vary by SystemDesirability

        IList<PlanetoidCategory> planetCatsByPlanetIndex = GetPlanetCategories(systemDesirability, isHomeSystem: false);

        return DeployAndConfigureCreatorTo(systemName, location, systemDesirability, starCat, planetCatsByPlanetIndex);
    }
    //public SystemCreator DeployAndConfigureRandomSystemCreatorTo(Vector3 location) {
    //    string systemName = _nameFactory.GetUnusedName();

    //    SystemDesirability[] desirabilityChoices = Enums<SystemDesirability>.GetValues(excludeDefault: true).ToArray();
    //    int[] desirabilityWeight = new int[] { 1, 3, 1 };
    //    SystemDesirability systemDesirability = RandomExtended.WeightedChoice(desirabilityChoices, desirabilityWeight);

    //    float systemOrbitSlotsStartRadius;
    //    StarCategory starCat = Enums<StarCategory>.GetRandom(excludeDefault: true);
    //    string starDesignName = MakeAndRecordStarDesign(starCat, systemDesirability, out systemOrbitSlotsStartRadius);

    //    Stack<OrbitData> innerOrbitSlots;
    //    Stack<OrbitData> goldilocksOrbitSlots;
    //    Stack<OrbitData> outerOrbitSlots;

    //    GenerateSystemOrbitSlots(systemOrbitSlotsStartRadius, out innerOrbitSlots, out goldilocksOrbitSlots, out outerOrbitSlots);
    //    OrbitData settlementOrbitSlot = goldilocksOrbitSlots.Pop();
    //    D.Assert(settlementOrbitSlot != null);

    //    int planetQty = RandomExtended.Range(0, TempGameValues.TotalOrbitSlotsPerSystem - 1);
    //    IList<PlanetoidCategory> planetCatsByPlanetIndex = RandomExtended.Choices<PlanetoidCategory>(_acceptablePlanetCategories, planetQty);

    //    IList<int> unassignedPlanetIndices;
    //    IList<OrbitData> planetOrbitSlots;
    //    IList<string> planetDesignNames = MakePlanetDesignsAndOrbitSlots(planetCatsByPlanetIndex, systemDesirability, innerOrbitSlots, goldilocksOrbitSlots, outerOrbitSlots, out planetOrbitSlots, out unassignedPlanetIndices);

    //    IList<PlanetoidCategory[]> moonCatsByPlanetIndex = GetRandomMoonCats(planetCatsByPlanetIndex);

    //    if (unassignedPlanetIndices.Count > Constants.Zero) {
    //        // one or more planets could not be assigned an orbit slot and design so eliminate any moons for that planet
    //        foreach (var pIndex in unassignedPlanetIndices) {
    //            moonCatsByPlanetIndex[pIndex] = Enumerable.Empty<PlanetoidCategory>().ToArray();
    //            planetCatsByPlanetIndex[pIndex] = default(PlanetoidCategory);
    //        }
    //    }

    //    IList<OrbitData[]> moonOrbitSlots;
    //    IList<string[]> moonDesignNames = MakeMoonDesignsAndOrbitSlots(planetCatsByPlanetIndex, moonCatsByPlanetIndex, systemDesirability, out moonOrbitSlots);

    //    bool isTrackingLabelEnabled = DebugControls.Instance.AreAutoSystemCreatorTrackingLabelsEnabled;

    //    SystemCreatorConfiguration config = new SystemCreatorConfiguration(systemName, starDesignName, settlementOrbitSlot, planetDesignNames, planetOrbitSlots, moonDesignNames, moonOrbitSlots, isTrackingLabelEnabled);
    //    SystemCreator creator = _systemFactory.MakeCreatorInstance(location);
    //    creator.Configuration = config;
    //    return creator;
    //}

    private SystemCreator DeployAndConfigureCreatorTo(string systemName, Vector3 location, SystemDesirability systemDesirability, StarCategory starCat, IList<PlanetoidCategory> planetCatsByPlanetIndex) {
        float systemOrbitSlotsStartRadius;
        string starDesignName = MakeAndRecordStarDesign(starCat, systemDesirability, out systemOrbitSlotsStartRadius);

        Stack<OrbitData> innerOrbitSlots;
        Stack<OrbitData> goldilocksOrbitSlots;
        Stack<OrbitData> outerOrbitSlots;

        GenerateSystemOrbitSlots(systemOrbitSlotsStartRadius, out innerOrbitSlots, out goldilocksOrbitSlots, out outerOrbitSlots);
        OrbitData settlementOrbitSlot = goldilocksOrbitSlots.Pop();
        D.Assert(settlementOrbitSlot != null);

        IList<int> unassignedPlanetIndices;
        IList<OrbitData> planetOrbitSlots;
        IList<string> planetDesignNames = MakePlanetDesignsAndOrbitSlots(planetCatsByPlanetIndex, systemDesirability, innerOrbitSlots, goldilocksOrbitSlots, outerOrbitSlots, out planetOrbitSlots, out unassignedPlanetIndices);

        IList<PlanetoidCategory[]> moonCatsByPlanetIndex = GetRandomMoonCats(planetCatsByPlanetIndex);

        if (unassignedPlanetIndices.Count > Constants.Zero) {
            // one or more planets could not be assigned an orbit slot and design so eliminate any moons for that planet
            foreach (var pIndex in unassignedPlanetIndices) {
                moonCatsByPlanetIndex[pIndex] = Enumerable.Empty<PlanetoidCategory>().ToArray();
                planetCatsByPlanetIndex[pIndex] = default(PlanetoidCategory);
            }
        }

        IList<OrbitData[]> moonOrbitSlots;
        IList<string[]> moonDesignNames = MakeMoonDesignsAndOrbitSlots(planetCatsByPlanetIndex, moonCatsByPlanetIndex, systemDesirability, out moonOrbitSlots);

        bool isTrackingLabelEnabled = DebugControls.Instance.AreAutoSystemCreatorTrackingLabelsEnabled;

        SystemCreatorConfiguration config = new SystemCreatorConfiguration(systemName, starDesignName, settlementOrbitSlot, planetDesignNames, planetOrbitSlots, moonDesignNames, moonOrbitSlots, isTrackingLabelEnabled);
        SystemCreator creator = _systemFactory.MakeCreatorInstance(location);
        creator.Configuration = config;
        return creator;
    }

    #endregion

    public void Reset() {
        _designNameCounter = Constants.One;
        __systemOrbitSlotDepth = Constants.ZeroF;
    }

    private IList<PlanetoidCategory[]> GetRandomMoonCats(IList<PlanetoidCategory> planetCatsByPlanetIndex) {
        int planetCatQty = planetCatsByPlanetIndex.Count;
        IList<PlanetoidCategory[]> moonCatsByPlanetIndex = new List<PlanetoidCategory[]>(planetCatQty);

        for (int pIndex = 0; pIndex < planetCatQty; pIndex++) {
            int maxMoonQty;
            IEnumerable<PlanetoidCategory> acceptableMoonCats;
            PlanetoidCategory pCat = planetCatsByPlanetIndex[pIndex];
            switch (pCat) {
                case PlanetoidCategory.Volcanic:
                    maxMoonQty = 3; // 10.3.16 can't fit 4 even if all are smallest moons
                    acceptableMoonCats = _acceptableSmallPlanetMoonCategories;
                    break;
                case PlanetoidCategory.Terrestrial:
                case PlanetoidCategory.Ice:
                    maxMoonQty = 2; // 10.3.16 can't fit 3 even if all are smallest moons
                    acceptableMoonCats = _acceptableMediumPlanetMoonCategories;
                    break;
                case PlanetoidCategory.GasGiant:
                    maxMoonQty = 2; // 10.3.16 can't fit 3 even if all are smallest moons
                    acceptableMoonCats = _acceptableLargePlanetMoonCategories;
                    break;
                case PlanetoidCategory.Moon_001:
                case PlanetoidCategory.Moon_002:
                case PlanetoidCategory.Moon_003:
                case PlanetoidCategory.Moon_004:
                case PlanetoidCategory.Moon_005:
                case PlanetoidCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pCat));
            }
            int moonQty = RandomExtended.Range(0, maxMoonQty);
            PlanetoidCategory[] mCats = RandomExtended.Choices<PlanetoidCategory>(acceptableMoonCats, moonQty).ToArray();
            //D.Log("{0}: Index = {1}, PlanetCategoryQty = {2}.", Name, pIndex, planetCatQty);
            moonCatsByPlanetIndex.Add(mCats);   //moonCatsByPlanetIndex[pIndex] = mCats;
        }
        return moonCatsByPlanetIndex;
    }

    private IList<string> MakePlanetDesignsAndOrbitSlots(IList<PlanetoidCategory> pCats, SystemDesirability desirability, Stack<OrbitData> innerOrbitSlots, Stack<OrbitData> goldilocksOrbitSlots, Stack<OrbitData> outerOrbitSlots, out IList<OrbitData> assignedOrbitSlots, out IList<int> unassignedPlanetIndices) {
        IList<string> designNames = new List<string>(pCats.Count);
        assignedOrbitSlots = new List<OrbitData>(pCats.Count);
        unassignedPlanetIndices = new List<int>(pCats.Count);
        for (int pIndex = 0; pIndex < pCats.Count; pIndex++) {
            PlanetoidCategory pCat = pCats[pIndex];
            OrbitData assignedSlot;
            if (TryAssignPlanetOrbitSlot(pCat, innerOrbitSlots, goldilocksOrbitSlots, outerOrbitSlots, out assignedSlot)) {
                assignedOrbitSlots.Add(assignedSlot);   //assignedOrbitSlots[pIndex] = assignedSlot;
                //D.Log("{0} fit planetCat {1} in system orbit slot {2}.", Name, pCat.GetValueName(), pIndex);

                string designName = MakeAndRecordPlanetDesign(pCat, /*passiveCmQty*/desirability);
                designNames.Add(designName);    //designNames[pIndex] = designName;
            }
            else {
                unassignedPlanetIndices.Add(pIndex);
                D.Log("{0} could not fit planetCat {1} in system orbit slot {2}.", GetType().Name, pCat.GetValueName(), pIndex);
            }
        }
        return designNames;
    }

    private IList<string[]> MakeMoonDesignsAndOrbitSlots(IList<PlanetoidCategory> pCatsByPlanetIndex, IList<PlanetoidCategory[]> mCatsByPlanetIndex, SystemDesirability desirability, out IList<OrbitData[]> assignedOrbitSlotsByPlanetIndex) {
        int planetIndexQty = pCatsByPlanetIndex.Count;

        assignedOrbitSlotsByPlanetIndex = new List<OrbitData[]>(planetIndexQty);
        IList<string[]> moonDesignNamesByPlanetIndex = new List<string[]>(planetIndexQty);

        for (int pIndex = 0; pIndex < planetIndexQty; pIndex++) {
            PlanetoidCategory[] moonCats = mCatsByPlanetIndex[pIndex];
            IList<string> moonDesignNames = new List<string>(moonCats.Length);
            IList<OrbitData> moonOrbitSlots = new List<OrbitData>(moonCats.Length);
            if (moonCats.Any()) {

                PlanetoidCategory pCat = pCatsByPlanetIndex[pIndex];
                D.Assert(pCat != default(PlanetoidCategory));   // there are moons so this shouldn't be None
                float planetCloseOrbitInnerRadius = pCat.Radius() + TempGameValues.PlanetCloseOrbitInnerRadiusAdder;
                float moonOrbitSlotStartDepth = planetCloseOrbitInnerRadius;

                for (int mSlotIndex = 0; mSlotIndex < moonCats.Length; mSlotIndex++) {
                    PlanetoidCategory mCat = moonCats[mSlotIndex];
                    OrbitData assignedMoonOrbitSlot;
                    if (TryAssignMoonOrbitSlot(mCat, mSlotIndex, moonOrbitSlotStartDepth, out assignedMoonOrbitSlot)) {
                        moonOrbitSlots.Add(assignedMoonOrbitSlot);  //moonOrbitSlots[mSlotIndex] = assignedMoonOrbitSlot;
                        //D.Log("{0} fit moonCat {1} in orbit slot {2} around planetCat {3}.",
                        //    Name, mCat.GetValueName(), mSlotIndex, pCat.GetValueName());

                        string moonDesignName = MakeAndRecordMoonDesign(mCat, desirability);
                        moonDesignNames.Add(moonDesignName);    //moonDesignNames[mSlotIndex] = moonDesignName;

                        moonOrbitSlotStartDepth += assignedMoonOrbitSlot.OuterRadius;
                    }
                    else {
                        //D.Log("{0} could not fit moonCat {1} in orbit slot {2} around planetCat {3}.",
                        //    Name, mCat.GetValueName(), mSlotIndex, pCat.GetValueName());
                    }
                }
            }
            // else no moons for this planetIndex either because 1) none were desired, 
            // or 2) the planet was removed due to lack of an available orbit slot

            assignedOrbitSlotsByPlanetIndex.Add(moonOrbitSlots.ToArray());  //assignedOrbitSlotsByPlanetIndex[pIndex] = moonOrbitSlots.ToArray();
            moonDesignNamesByPlanetIndex.Add(moonDesignNames.ToArray());    //moonDesignNamesByPlanetIndex[pIndex] = moonDesignNames.ToArray();
        }
        return moonDesignNamesByPlanetIndex;
    }

    private bool TryAssignMoonOrbitSlot(PlanetoidCategory mCat, int slotIndex, float startDepthForOrbitSlot, out OrbitData assignedSlot) {
        float depthAvailableForMoonOrbitsAroundPlanet = __systemOrbitSlotDepth;
        float moonObstacleZoneRadius = mCat.Radius() + TempGameValues.MoonObstacleZoneRadiusAdder;
        float depthReqdForOrbitSlot = moonObstacleZoneRadius * 2F;
        float endDepthForOrbitSlot = startDepthForOrbitSlot + depthReqdForOrbitSlot;
        if (endDepthForOrbitSlot > depthAvailableForMoonOrbitsAroundPlanet) {
            assignedSlot = null;
            return false;
        }
        GameTimeDuration orbitPeriod = _minMoonOrbitPeriod + (slotIndex * _moonOrbitPeriodIncrement);
        assignedSlot = new OrbitData(slotIndex, startDepthForOrbitSlot, endDepthForOrbitSlot, orbitPeriod);
        return true;
    }

    #region Make and Record Designs

    private string MakeAndRecordStarDesign(StarCategory cat, SystemDesirability desirability, out float systemOrbitSlotsStartRadius) {
        string designName = GetUniqueDesignName(cat.GetValueName());
        StarStat stat = MakeRandomStarStat(cat, desirability, out systemOrbitSlotsStartRadius);
        StarDesign design = new StarDesign(designName, stat);
        _gameMgr.CelestialDesigns.Add(design);
        return designName;
    }

    private string MakeAndRecordPlanetDesign(PlanetoidCategory cat, SystemDesirability desirability) {
        string designName = GetUniqueDesignName(cat.GetValueName());
        PlanetStat stat = MakeRandomPlanetStat(cat, desirability);
        int passiveCmQty = GetPassiveCountermeasureQty(desirability, max: 3);
        var passiveCMs = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCmQty);
        PlanetDesign design = new PlanetDesign(designName, stat, passiveCMs);
        _gameMgr.CelestialDesigns.Add(design);
        return designName;
    }

    private string MakeAndRecordMoonDesign(PlanetoidCategory cat, SystemDesirability desirability) {
        string designName = GetUniqueDesignName(cat.GetValueName());
        PlanetoidStat stat = MakeRandomMoonStat(cat, desirability);
        int passiveCmQty = GetPassiveCountermeasureQty(desirability, max: 1);
        var passiveCMs = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCmQty);
        MoonDesign design = new MoonDesign(designName, stat, passiveCMs);
        _gameMgr.CelestialDesigns.Add(design);
        return designName;
    }

    #endregion

    private bool TryAssignPlanetOrbitSlot(PlanetoidCategory pCat, Stack<OrbitData> innerSlots, Stack<OrbitData> goldilocksSlots, Stack<OrbitData> outerSlots, out OrbitData assignedSlot) {
        Stack<OrbitData>[] slots;
        switch (pCat) {
            case PlanetoidCategory.Volcanic:
                slots = new Stack<OrbitData>[] { innerSlots, goldilocksSlots };
                break;
            case PlanetoidCategory.Terrestrial:
                slots = new Stack<OrbitData>[] { goldilocksSlots, innerSlots, outerSlots };
                break;
            case PlanetoidCategory.Ice:
                slots = new Stack<OrbitData>[] { outerSlots, goldilocksSlots };
                break;
            case PlanetoidCategory.GasGiant:
                slots = new Stack<OrbitData>[] { outerSlots, goldilocksSlots };
                break;
            case PlanetoidCategory.Moon_001:
            case PlanetoidCategory.Moon_002:
            case PlanetoidCategory.Moon_003:
            case PlanetoidCategory.Moon_004:
            case PlanetoidCategory.Moon_005:
            case PlanetoidCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pCat));
        }
        return TryFindOrbitSlot(slots, out assignedSlot);
    }

    private bool TryFindOrbitSlot(Stack<OrbitData>[] slotStacks, out OrbitData slot) {
        foreach (var slotStack in slotStacks) {
            if (slotStack.Count > 0) {
                slot = slotStack.Pop();
                return true;
            }
        }
        slot = null;
        return false;
    }

    /// <summary>
    /// Generates an ordered array of all CelestialOrbitSlots in the system. The first slot (index = 0) is the closest to the star, just outside
    /// the star's CloseOrbitOuterRadius.
    /// Note: These are system orbit slots that can be occupied by planets and settlements.
    /// </summary>
    /// <returns></returns>
    private void GenerateSystemOrbitSlots(float sysOrbitSlotsStartRadius, out Stack<OrbitData> innerSlots, out Stack<OrbitData> goldilocksSlots, out Stack<OrbitData> outerSlots) {
        float systemRadiusAvailableForAllOrbits = TempGameValues.SystemRadius - sysOrbitSlotsStartRadius;
        __systemOrbitSlotDepth = systemRadiusAvailableForAllOrbits / (float)TempGameValues.TotalOrbitSlotsPerSystem;
        //D.Log("{0}: SystemOrbitSlotDepth = {1:0.#}.", Name, _systemOrbitSlotDepth);

        innerSlots = new Stack<OrbitData>(1);
        goldilocksSlots = new Stack<OrbitData>(3);
        outerSlots = new Stack<OrbitData>(2);

        for (int slotIndex = 0; slotIndex < TempGameValues.TotalOrbitSlotsPerSystem; slotIndex++) {
            float insideRadius = sysOrbitSlotsStartRadius + __systemOrbitSlotDepth * slotIndex;
            float outsideRadius = insideRadius + __systemOrbitSlotDepth;
            var orbitPeriod = _minSystemOrbitPeriod + (slotIndex * _systemOrbitPeriodIncrement);
            //D.Log("{0}: Orbit slot index {1} OrbitPeriod = {2}.", Name, slotIndex, orbitPeriod);
            OrbitData slot = new OrbitData(slotIndex, insideRadius, outsideRadius, orbitPeriod);

            switch (slotIndex) {
                case 0:
                    innerSlots.Push(slot);
                    break;
                case 1:
                case 2:
                case 3:
                    goldilocksSlots.Push(slot);
                    break;
                case 4:
                case 5:
                    outerSlots.Push(slot);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(slotIndex));
            }
        }
        innerSlots.Shuffle();
        goldilocksSlots.Shuffle();
        outerSlots.Shuffle();
    }

    private int GetPassiveCountermeasureQty(SystemDesirability desirability, int max) {
        Utility.ValidateForRange(max, 1, 3);    // 10.8.16 HACK if this fails, the values are lower/higher than I currently intend
        switch (desirability) {
            case SystemDesirability.Desirable:
                return RandomExtended.Range(1, max);
            case SystemDesirability.Normal:
                return RandomExtended.Range(0, max);
            case SystemDesirability.Challenged:
                return RandomExtended.Range(0, max - 1);
            case SystemDesirability.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(desirability));
        }
    }

    #region Make Stats

    private StarStat MakeRandomStarStat(StarCategory category, SystemDesirability desirability, out float systemOrbitSlotsStartRadius) {
        float radius = TempGameValues.StarRadius;
        float closeOrbitInnerRadius = radius + 2F;
        systemOrbitSlotsStartRadius = closeOrbitInnerRadius + TempGameValues.ShipCloseOrbitSlotDepth;
        int capacity = 100; // TODO vary by desirability
        return new StarStat(category, radius, closeOrbitInnerRadius, capacity, CreateRandomResourceYield(desirability, ResourceCategory.Common, ResourceCategory.Strategic));
    }

    private IList<PassiveCountermeasureStat> MakeAvailablePassiveCountermeasureStats(int quantity) {
        IList<PassiveCountermeasureStat> statsList = new List<PassiveCountermeasureStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            DamageStrength damageMitigation;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: false);
            float damageMitigationValue;
            switch (damageMitigationCategory) {
                case DamageCategory.Thermal:
                    name = "HighVaporAtmosphere";
                    damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Atomic:
                    name = "HighAcidAtmosphere";
                    damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Kinetic:
                    name = "HighParticulateAtmosphere";
                    damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.None:
                    name = "NoAtmosphere";
                    damageMitigation = new DamageStrength(1F, 1F, 1F);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageMitigationCategory));
            }
            var countermeasureStat = new PassiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, damageMitigation);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    private ResourceYield CreateRandomResourceYield(SystemDesirability desirability, params ResourceCategory[] resCategories) {
        ResourceYield sum = default(ResourceYield);
        resCategories.ForAll(resCat => sum += CreateRandomResourceYield(resCat, desirability));
        return sum;
    }

    private ResourceYield CreateRandomResourceYield(ResourceCategory resCategory, SystemDesirability desirability) {
        float minYield;
        float maxYield;
        int minNumberOfResources;
        switch (resCategory) {
            case ResourceCategory.Common:
                switch (desirability) {
                    case SystemDesirability.Desirable:
                        minNumberOfResources = 2;
                        minYield = 1F;
                        maxYield = 5F;
                        break;
                    case SystemDesirability.Normal:
                        minNumberOfResources = 1;
                        minYield = 1F;
                        maxYield = 4F;
                        break;
                    case SystemDesirability.Challenged:
                        minNumberOfResources = 1;
                        minYield = 0F;
                        maxYield = 3F;
                        break;
                    case SystemDesirability.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(desirability));
                }
                break;
            case ResourceCategory.Strategic:
                switch (desirability) {
                    case SystemDesirability.Desirable:
                        minNumberOfResources = 1;
                        minYield = 1F;
                        maxYield = 3F;
                        break;
                    case SystemDesirability.Normal:
                        minNumberOfResources = 0;
                        minYield = 1F;
                        maxYield = 2F;
                        break;
                    case SystemDesirability.Challenged:
                        minNumberOfResources = 0;
                        minYield = 0F;
                        maxYield = 2F;
                        break;
                    case SystemDesirability.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(desirability));
                }
                break;
            case ResourceCategory.Luxury:   // No Luxury Resources yet
            case ResourceCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resCategory));
        }

        var categoryResources = Enums<ResourceID>.GetValues(excludeDefault: true).Where(res => res.GetResourceCategory() == resCategory);
        int categoryResourceCount = categoryResources.Count();
        D.Assert(categoryResourceCount > minNumberOfResources);
        int numberOfResourcesToCreate = RandomExtended.Range(minNumberOfResources, categoryResourceCount);

        IList<ResourceYield.ResourceValuePair> resValuePairs = new List<ResourceYield.ResourceValuePair>(numberOfResourcesToCreate);
        var resourcesChosen = categoryResources.Shuffle().Take(numberOfResourcesToCreate);
        resourcesChosen.ForAll(resID => {
            var rvp = new ResourceYield.ResourceValuePair(resID, UnityEngine.Random.Range(minYield, maxYield));
            resValuePairs.Add(rvp);
        });
        return new ResourceYield(resValuePairs.ToArray());
    }

    private PlanetStat MakeRandomPlanetStat(PlanetoidCategory pCategory, SystemDesirability desirability) {
        float radius = pCategory.Radius();
        float closeOrbitInnerRadius = radius + TempGameValues.PlanetCloseOrbitInnerRadiusAdder;
        float maxHitPts = 100F; // TODO vary by desirability
        int capacity = 25;  // TODO vary by desirability
        var resourceYield = CreateRandomResourceYield(desirability, ResourceCategory.Common, ResourceCategory.Strategic);
        return new PlanetStat(radius, 1000000F, maxHitPts, pCategory, capacity, resourceYield, closeOrbitInnerRadius);
    }

    private PlanetoidStat MakeRandomMoonStat(PlanetoidCategory mCategory, SystemDesirability desirability) {
        float radius = mCategory.Radius();
        float maxHitPts = 10F;  // TODO vary by desirability
        int capacity = 5;   // TODO vary by desirability
        var resourceYield = CreateRandomResourceYield(desirability, ResourceCategory.Common);
        return new PlanetoidStat(radius, 10000F, maxHitPts, mCategory, capacity, resourceYield);
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

