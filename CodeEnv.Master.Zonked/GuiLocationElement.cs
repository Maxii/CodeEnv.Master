// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiLocationElement.cs
// GuiElement handling the display and tooltip content for the Location of an item. 
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
/// GuiElement handling the display and tooltip content for the Location of an item. 
/// </summary>
[Obsolete]
public class GuiLocationElement : GuiElement, IComparable<GuiLocationElement> {

    private static string _labelFormat = "{0} " + GameConstants.IconMarker_Distance + Constants.NewLine + Constants.NewLine + "{1}";
    private static string _tooltipFormat = "Distance in sectors to closest owned base {0} = {1}.";
    private static string _unknown = Constants.QuestionMark;


    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    //private Index3D _sectorIndex;
    //private IBaseCmdItem _closestOwnedBase;
    private float? _distanceInSectors;
    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        Validate();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    public void SetValues(Index3D sectorIndex, Vector3? worldPosition) {
        //_sectorIndex = sectorIndex;

        bool isPositionValid = false;
        Vector3 position;

        if (worldPosition.HasValue) {
            position = worldPosition.Value;
            isPositionValid = true;
        }
        else if (SectorGrid.Instance.TryGetSectorPosition(sectorIndex, out position)) {
            isPositionValid = true;
        }

        IBaseCmdItem closestOwnedBase = null;
        string distanceText = _unknown;
        if (isPositionValid) {
            closestOwnedBase = GameManager.Instance.GetUserPlayerKnowledge().GetMyClosestBase(position);
            _distanceInSectors = SectorGrid.Instance.GetDistanceInSectors(sectorIndex, closestOwnedBase.SectorIndex);
            distanceText = Constants.FormatFloat_1DpMax.Inject(_distanceInSectors.Value);
        }

        _label.text = _labelFormat.Inject(distanceText, sectorIndex);

        string baseText = closestOwnedBase != null ? closestOwnedBase.DisplayName : _unknown;
        _tooltipContent = _tooltipFormat.Inject(baseText, distanceText);
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

    #region IComparable<GuiLocationElement> Members

    public int CompareTo(GuiLocationElement other) {
        int result;
        if (!_distanceInSectors.HasValue) {
            result = !other._distanceInSectors.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other._distanceInSectors.HasValue ? Constants.One : _distanceInSectors.Value.CompareTo(other._distanceInSectors.Value);
        }
        return result;
    }

    #endregion

}

