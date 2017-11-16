// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationGenerator.cs
// Singleton Factory that generates Formations for Unit Commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton Factory that generates Formations for Unit Commands.
/// <remarks>Must reside in GameScriptsFactory project to have access to ReqdPrefabs.</remarks>
/// </summary>
public class FormationGenerator : AGenericSingleton<FormationGenerator>, IFormationGenerator, IDisposable {

    private GameObject _globeFormation;
    private GameObject _wedgeFormation;
    private GameObject _planeFormation;
    private GameObject _diamondFormation;
    private GameObject _spreadFormation;
    private GameObject _hangerFormation;

    #region Singleton Initialization

    private FormationGenerator() {
        Initialize();
    }

    protected override void Initialize() {
        // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
        _globeFormation = RequiredPrefabs.Instance.globeFormation;
        _wedgeFormation = RequiredPrefabs.Instance.wedgeFormation;
        _planeFormation = RequiredPrefabs.Instance.planeFormation;
        _diamondFormation = RequiredPrefabs.Instance.diamondFormation;
        _spreadFormation = RequiredPrefabs.Instance.spreadFormation;
        _hangerFormation = RequiredPrefabs.Instance.hangerFormation;
    }

    #endregion

    /// <summary>
    /// Generates the specified formation for a Base.
    /// Returns a list of FormationStationSlotInfo instances containing the slotID and the local space relative
    /// position (offset relative to the position of the HQElement) of each station slot in the formation including the HQElement's station.
    /// </summary>
    /// <param name="formation">The formation.</param>
    /// <param name="cmdTransform">The command transform.</param>
    /// <param name="formationRadius">The resulting unit formation radius.</param>
    /// <returns></returns>
    public List<FormationStationSlotInfo> GenerateBaseFormation(Formation formation, Transform cmdTransform, out float formationRadius) {
        D.AssertNotDefault((int)formation);
        D.AssertNotEqual(Formation.Wedge, formation);
        D.AssertNotEqual(Formation.Hanger, formation);
        return GenerateFormation(formation, cmdTransform, out formationRadius);
    }

    /// <summary>
    /// Generates the specified formation for a Fleet.
    /// Returns a list of FormationStationSlotInfo instances containing the slotID and the local space relative
    /// position (offset relative to the position of the HQElement) of each station slot in the formation including the HQElement's station.
    /// </summary>
    /// <param name="formation">The formation.</param>
    /// <param name="cmdTransform">The command transform.</param>
    /// <param name="formationRadius">The resulting unit formation radius.</param>
    /// <returns></returns>
    public List<FormationStationSlotInfo> GenerateFleetFormation(Formation formation, Transform cmdTransform, out float formationRadius) {
        D.AssertNotDefault((int)formation);
        D.AssertNotEqual(Formation.Hanger, formation);
        return GenerateFormation(formation, cmdTransform, out formationRadius);
    }

    /// <summary>
    /// Generates a formation to hold ships that are resident in a Base's Hanger.
    /// Returns a list of FormationStationSlotInfo instances containing the slotID and the local space relative
    /// position (offset relative to the position of the followTransform) of each station slot in the formation which can,
    /// but does not need to include a slot at the location of the followTransform.
    /// <remarks>Use of formation allows future use of other hanger formations.</remarks>
    /// </summary>
    /// <param name="formation">The hanger formation to generate.</param>
    /// <param name="followTransform">The transform this formation is to follow.</param>
    /// <param name="formationRadius">The resulting unit formation radius.</param>
    /// <returns></returns>
    public List<FormationStationSlotInfo> GenerateHangerFormation(Formation formation, Transform followTransform, out float formationRadius) {
        D.AssertEqual(Formation.Hanger, formation);
        return GenerateFormation(_hangerFormation, followTransform, FormationStationSlotID.Slot_1_2_2, out formationRadius);
    }

    /// <summary>
    /// Generates a formation, returning a list of FormationStationSlotInfo instances containing the slotID and
    /// the local space relative position (offset relative to the position of the HQElement) of each station in the formation
    /// including the HQElement's station.
    /// </summary>
    /// <param name="formation">The formation.</param>
    /// <param name="followTransform">The command transform.</param>
    /// <param name="formationRadius">The resulting formation radius.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private List<FormationStationSlotInfo> GenerateFormation(Formation formation, Transform cmdTransform, out float formationRadius) {

        GameObject formationGrid;
        switch (formation) {
            case Formation.Globe:
                formationGrid = _globeFormation;
                break;
            case Formation.Plane:
                formationGrid = _planeFormation;
                break;
            case Formation.Diamond:
                formationGrid = _diamondFormation;
                break;
            case Formation.Wedge:
                formationGrid = _wedgeFormation;
                break;
            case Formation.Spread:
                formationGrid = _spreadFormation;
                break;
            case Formation.Hanger:
            case Formation.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(formation));
        }
        IList<FormationStationPlaceholder> allActiveStationPlaceholders =
            formationGrid.GetComponentsInChildren<FormationStationPlaceholder>(includeInactive: false);
        var hqStationPlaceholder = allActiveStationPlaceholders.Single(sp => sp.IsHQ);    // throws error if more than one or not present
        var hqStationSlotID = hqStationPlaceholder.SlotID;
        return GenerateFormation(formationGrid, cmdTransform, hqStationSlotID, out formationRadius);
    }

    private List<FormationStationSlotInfo> GenerateFormation(GameObject formationGrid, Transform followTransform,
        FormationStationSlotID followSlotID, out float formationRadius) {
        //System.DateTime startTime = System.DateTime.UtcNow;

        formationRadius = Constants.ZeroF;
        IList<FormationStationPlaceholder> allStationPlaceholders =
            formationGrid.GetComponentsInChildren<FormationStationPlaceholder>(includeInactive: true);
        int placeholderCount = allStationPlaceholders.Count;

        List<FormationStationSlotInfo> stationSlotInfos = new List<FormationStationSlotInfo>(placeholderCount);
        // followStation does not need to be active to be followed as all we need is its position to calc relative positions of rest
        FormationStationPlaceholder followStation = allStationPlaceholders.Single(sp => sp.SlotID == followSlotID);
        Vector3 followStationPosition = followStation.transform.position;
        for (int i = 0; i < placeholderCount; i++) {
            var placeholder = allStationPlaceholders[i];
            if (!placeholder.gameObject.activeSelf) {
                continue;
            }
            var stationSlotID = placeholder.SlotID;
            bool isReserve = placeholder.IsReserve;
            Vector3 worldOffsetToFollowStation = placeholder.transform.position - followStationPosition;
            Vector3 localOffsetToFollowTransform = followTransform.InverseTransformDirection(worldOffsetToFollowStation);
            stationSlotInfos.Add(new FormationStationSlotInfo(stationSlotID, isReserve, localOffsetToFollowTransform));

            float stationDistanceToFollowStation = worldOffsetToFollowStation.magnitude;
            formationRadius = Mathf.Max(stationDistanceToFollowStation, formationRadius);
        }
        // this value is from the followStation to the outside element, so add that element's formation station radius
        formationRadius += TempGameValues.FleetFormationStationRadius;  // IMPROVE when Bases use movable FormationStations
        //D.Log("{0} generated a Formation using {1}, accommodating up to {2} elements with radius {3:0.#}.", DebugName, formationGrid.name, placeholderCount, formationRadius);
        //D.Log("{0}: Generating a Formation took {1:0.####} secs.", DebugName, (System.DateTime.UtcNow - startTime).TotalSeconds);
        __ValidateSlotIDs(stationSlotInfos);
        return stationSlotInfos;
    }

    private void Cleanup() { }

    #region Debug

    [Conditional("DEBUG")]
    private void __ValidateSlotIDs(IList<FormationStationSlotInfo> slotInfos) {
        FormationStationSlotID duplicate;
        D.Assert(!slotInfos.Select(si => si.SlotID).ContainsDuplicates(out duplicate, FormationStationSlotIDEqualityComparer.Default), duplicate.GetValueName());
    }

    #endregion

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
            CallOnDispose();
        }

        // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
        // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
        // called Dispose(false) to cleanup unmanaged resources

        _alreadyDisposed = true;
    }

    #endregion


}


