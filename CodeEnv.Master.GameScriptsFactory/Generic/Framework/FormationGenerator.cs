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
/// Singleton Factory that generates Formations for Unit Commands.
/// <remarks>Must reside in GameScriptsFactory project to have access to ReqdPrefabs.</remarks>
/// </summary>
public class FormationGenerator : AGenericSingleton<FormationGenerator>, IFormationGenerator, IDisposable {

    private GameObject _globeFormation;
    private GameObject _wedgeFormation;
    private GameObject _planeFormation;
    private GameObject _diamondFormation;
    private GameObject _spreadFormation;

    #region Singleton Initialization

    private FormationGenerator() {
        Initialize();
    }

    protected override void Initialize() {
        // WARNING: Donot use Instance or _instance in here as this is still part of Constructor
        _globeFormation = RequiredPrefabs.Instance.globeFormation;
        _wedgeFormation = RequiredPrefabs.Instance.wedgeFormation;
        _planeFormation = RequiredPrefabs.Instance.planeFormation;
        _diamondFormation = RequiredPrefabs.Instance.diamondFormation;
        _spreadFormation = RequiredPrefabs.Instance.spreadFormation;
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
    public IList<FormationStationSlotInfo> GenerateBaseFormation(Formation formation, Transform cmdTransform, out float formationRadius) {
        D.Assert(formation != Formation.None && formation != Formation.Wedge);
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
    public IList<FormationStationSlotInfo> GenerateFleetFormation(Formation formation, Transform cmdTransform, out float formationRadius) {
        D.Assert(formation != Formation.None);
        return GenerateFormation(formation, cmdTransform, out formationRadius);
    }

    /// <summary>
    /// Generates a formation, returning a list of FormationStationSlotInfo instances containing the slotID and
    /// the local space relative position (offset relative to the position of the HQElement) of each station in the formation
    /// including the HQElement's station.
    /// </summary>
    /// <param name="formation">The formation.</param>
    /// <param name="cmdTransform">The command transform.</param>
    /// <param name="formationRadius">The resulting formation radius.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private IList<FormationStationSlotInfo> GenerateFormation(Formation formation, Transform cmdTransform, out float formationRadius) {
        System.DateTime startTime = System.DateTime.UtcNow;

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
            case Formation.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(formation));
        }
        formationRadius = Constants.ZeroF;
        IList<FormationStationPlaceholder> allStationPlaceholders = formationGrid.GetComponentsInChildren<FormationStationPlaceholder>();
        int placeholderCount = allStationPlaceholders.Count;

        IList<FormationStationSlotInfo> stationSlotInfos = new List<FormationStationSlotInfo>(placeholderCount);
        var hqStationPlaceholder = allStationPlaceholders.Single(sp => sp.IsHQ);    // throws error if more than one or not present
        Vector3 hqStationPosition = hqStationPlaceholder.transform.position;
        stationSlotInfos.Add(new FormationStationSlotInfo(hqStationPlaceholder.SlotID, hqStationPlaceholder.IsReserve, Vector3.zero));
        for (int i = 0; i < placeholderCount; i++) {
            var placeholder = allStationPlaceholders[i];
            if (placeholder.IsHQ) {
                continue;
            }
            var stationSlotID = placeholder.SlotID;
            bool isReserve = placeholder.IsReserve;
            Vector3 worldOffsetToHQ = placeholder.transform.position - hqStationPosition;
            Vector3 localOffset = cmdTransform.InverseTransformDirection(worldOffsetToHQ);
            stationSlotInfos.Add(new FormationStationSlotInfo(stationSlotID, isReserve, localOffset));

            float distanceToHQ = worldOffsetToHQ.magnitude;
            formationRadius = Mathf.Max(distanceToHQ, formationRadius);
        }
        // this value is from HQ to the outside element, so add that element's formation station radius
        formationRadius += TempGameValues.FleetFormationStationRadius;
        D.Log("{0} generated a {1} Formation accommodating up to {2} elements with radius {3:0.#}.", GetType().Name, formation.GetValueName(), placeholderCount, formationRadius);
        D.Log("{0}: Generating a {1} Formation took {2:0.####} secs.", GetType().Name, formation.GetValueName(), (System.DateTime.UtcNow - startTime).TotalSeconds);
        ValidateSlotIDs(stationSlotInfos);
        return stationSlotInfos;
    }

    private void ValidateSlotIDs(IList<FormationStationSlotInfo> slotInfos) {
        FormationStationSlotID duplicate;
        D.Assert(!slotInfos.Select(si => si.SlotID).ContainsDuplicates(out duplicate), "{0} found duplicate {1}: {2}.", GetType().Name, typeof(FormationStationSlotID).Name, duplicate.GetValueName());
    }

    private void Cleanup() {
        CallOnDispose();
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


