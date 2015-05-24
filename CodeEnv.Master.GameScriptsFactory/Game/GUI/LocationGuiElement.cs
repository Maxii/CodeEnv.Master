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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for the Location of a Command.  
/// </summary>
public class LocationGuiElement : GuiElement, IComparable<LocationGuiElement> {

    private static string _labelFormat = "{0} " + GameConstants.IconMarker_Distance + Constants.NewLine + Constants.NewLine + "{1}";
    private static string _tooltipFormat = "Distance in sectors to closest owned base {0} = {1}.";
    private static string _unknown = Constants.QuestionMark;

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private Index3D _sectorIndex;
    public Index3D SectorIndex {
        get { return _sectorIndex; }
        set { SetProperty<Index3D>(ref _sectorIndex, value, "SectorIndex", OnSectorIndexChanged); }
    }

    private bool _isPositionSet = false;
    private Vector3? _position;
    public Vector3? Position {
        get { return _position; }
        set {
            _position = value;
            OnPositionSet();
        }
    }

    protected virtual bool AreAllValuesSet { get { return _isPositionSet && SectorIndex != default(Index3D); } }

    private float? _closestBaseDistanceInSectors;
    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        Validate();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    private void OnSectorIndexChanged() {
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void OnPositionSet() {
        _isPositionSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    protected virtual void PopulateElementWidgets() {
        bool isPositionValid = false;
        Vector3 position;

        if (Position.HasValue) {
            position = Position.Value;
            isPositionValid = true;
        }
        else if (SectorGrid.Instance.TryGetSectorPosition(SectorIndex, out position)) {
            isPositionValid = true;
        }

        IBaseCmdItem myClosestBase = null;
        string distanceText = _unknown;
        if (isPositionValid) {
            myClosestBase = GameManager.Instance.GetUserPlayerKnowledge().GetMyClosestBase(position);
            if (myClosestBase != null) {
                // can be null if there are no bases currently owned by the user
                _closestBaseDistanceInSectors = SectorGrid.Instance.GetDistanceInSectors(SectorIndex, myClosestBase.SectorIndex);
                distanceText = Constants.FormatFloat_1DpMax.Inject(_closestBaseDistanceInSectors.Value);
            }
        }
        _label.text = _labelFormat.Inject(distanceText, SectorIndex);

        string baseText = myClosestBase != null ? myClosestBase.DisplayName : _unknown;
        _tooltipContent = _tooltipFormat.Inject(baseText, distanceText);
    }

    public override void Reset() {
        base.Reset();
        _isPositionSet = false;
        _sectorIndex = default(Index3D);
    }

    private void Validate() {
        if (elementID != GuiElementID.Location) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetName());
            elementID = GuiElementID.Location;
        }
    }

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

