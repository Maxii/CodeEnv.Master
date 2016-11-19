// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LocationGuiElement.cs
// GuiElement handling the display and tooltip content for the Location of a Command.  
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
/// GuiElement handling the display and tooltip content for the Location of a Command.  
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

    private bool _isPositionSet;
    private Vector3? _position;
    public Vector3? Position {
        get { return _position; }
        set {
            D.Assert(!_isPositionSet);  // only occurs once between Resets
            _position = value;
            PositionPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    protected virtual bool AreAllValuesSet { get { return _isPositionSet && SectorID != default(IntVector3); } }

    private float? _closestBaseDistanceInSectors;
    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void SectorIdPropSetHandler() {
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void PositionPropSetHandler() {
        _isPositionSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected virtual void PopulateElementWidgets() {
        bool isPositionValid = false;
        Vector3 position;

        if (Position.HasValue) {
            position = Position.Value;
            isPositionValid = true;
        }
        else if (SectorGrid.Instance.__TryGetSectorPosition(SectorID, out position)) {
            isPositionValid = true;
        }

        IUnitBaseCmd myClosestBase = null;
        string distanceText = _unknown;
        if (isPositionValid) {
            // can return false if there are no bases currently owned by the user
            if (GameManager.Instance.UserAIManager.TryFindMyClosestItem<IUnitBaseCmd>(position, out myClosestBase)) {
                _closestBaseDistanceInSectors = SectorGrid.Instance.GetDistanceInSectors(SectorID, myClosestBase.SectorID);
                distanceText = Constants.FormatFloat_1DpMax.Inject(_closestBaseDistanceInSectors.Value);
            }
        }
        _label.text = LabelFormat.Inject(distanceText, SectorID);

        string baseText = myClosestBase != null ? myClosestBase.DisplayName : _unknown;
        _tooltipContent = TooltipFormat.Inject(baseText, distanceText);
    }

    public override void Reset() {
        _isPositionSet = false;
        _sectorID = default(IntVector3);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

