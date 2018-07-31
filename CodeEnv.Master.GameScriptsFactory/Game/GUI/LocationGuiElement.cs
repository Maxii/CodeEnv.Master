// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LocationGuiElement.cs
// AGuiElement that represents the location of an item.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AGuiElement that represents the location of an item.
/// </summary>
public class LocationGuiElement : AGuiElement, IComparable<LocationGuiElement> {

    private const string TooltipFormat = "Distance in sectors to closest owned base {0} = {1}.";
    private static readonly string LabelFormat = "{0} " + GameConstants.IconMarker_Distance + Constants.NewLine + Constants.NewLine + "{1}";

    public override GuiElementID ElementID { get { return GuiElementID.Location; } }

    private IntVector3 _sectorID;
    public IntVector3 SectorID {
        get { return _sectorID; }
        set {
            D.AssertDefault(_sectorID); // only occurs once between Resets
            SetProperty<IntVector3>(ref _sectorID, value, "SectorID", SectorIdPropSetHandler);
        }
    }

    private bool _isLocationSet;
    private Vector3? _location;
    public Vector3? Location {
        get { return _location; }
        set {
            D.Assert(!_isLocationSet);  // only occurs once between Resets
            _location = value;
            LocationPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    public override bool IsInitialized { get { return _isLocationSet && SectorID != default(IntVector3); } }

    private float? _closestBaseDistanceInSectors;
    private UILabel _label;

    protected override void InitializeValuesAndReferences() {
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void SectorIdPropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void LocationPropSetHandler() {
        _isLocationSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        Vector3 location;
        if (Location.HasValue) {
            location = Location.Value;
        }
        else {
            location = SectorGrid.Instance.GetSectorCenterLocation(SectorID);
        }

        IUnitBaseCmd myClosestBase = null;
        string distanceText = Unknown;
        // can return false if there are no bases currently owned by the user
        if (GameManager.Instance.UserAIManager.TryFindMyClosestItem<IUnitBaseCmd>(location, out myClosestBase)) {
            _closestBaseDistanceInSectors = SectorGrid.Instance.GetDistanceInSectors(SectorID, myClosestBase.SectorID);
            distanceText = Constants.FormatFloat_1DpMax.Inject(_closestBaseDistanceInSectors.Value);
        }
        _label.text = LabelFormat.Inject(distanceText, SectorID);

        string baseText = myClosestBase != null ? myClosestBase.DebugName : Unknown;
        _tooltipContent = TooltipFormat.Inject(baseText, distanceText);
    }

    public override void ResetForReuse() {
        _isLocationSet = false;
        _sectorID = default(IntVector3);
    }

    protected override void Cleanup() { }

    #region IComparable<LocationGuiElement> Members

    public int CompareTo(LocationGuiElement other) {
        int result;
        if (!_closestBaseDistanceInSectors.HasValue) {
            result = !other._closestBaseDistanceInSectors.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other._closestBaseDistanceInSectors.HasValue ? Constants.One : _closestBaseDistanceInSectors.Value.CompareTo(other._closestBaseDistanceInSectors.Value);
        }
        return result;
    }

    #endregion

}

