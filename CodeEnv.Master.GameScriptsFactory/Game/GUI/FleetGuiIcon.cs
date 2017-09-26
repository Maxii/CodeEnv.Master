// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetGuiIcon.cs
// AMultiSizeGuiIcon that holds a FleetCmdItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AMultiSizeGuiIcon that holds a FleetCmdItem.
/// <remarks>This is an icon used by the Gui, not the in game icon that tracks a unit in space.</remarks>
/// </summary>
public class FleetGuiIcon : AUnitCmdGuiIcon {

    private const string SpeedFormat = "{0:0.#}";

    public new FleetCmdItem Unit {
        get { return base.Unit as FleetCmdItem; }
        set { base.Unit = value; }
    }

    protected override int MaxElementsPerUnit { get { return TempGameValues.MaxShipsPerFleet; } }

    protected override string UnitImageFilename { get { return TempGameValues.FleetImageFilename; } }

    private UILabel _speedLabel;

    protected override void Subscribe() {
        base.Subscribe();
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<FleetCmdData, FleetComposition>(data => data.UnitComposition, UnitCompositionPropChangedHandler));
        _subscriptions.Add(Unit.Data.SubscribeToPropertyChanged<FleetCmdData, float>(data => data.UnitFullSpeedValue, UnitFullSpeedPropChangedHandler));
    }

    protected override void AcquireAdditionalIconWidgets(GameObject topLevelIconGo) {
        base.AcquireAdditionalIconWidgets(topLevelIconGo);
        _speedLabel = topLevelIconGo.GetComponentsInChildren<GuiElement>().Single(ge => ge.ElementID == GuiElementID.SpeedLabel).GetComponent<UILabel>();
    }

    protected override void Show(GameColor color = GameColor.White) {
        _speedLabel.text = SpeedFormat.Inject(Unit.Data.UnitFullSpeedValue);
        base.Show(color);
    }

    #region Event and Property Change Handlers

    private void UnitCompositionPropChangedHandler() {
        HandleCompositionChanged();
    }

    private void UnitFullSpeedPropChangedHandler() {
        D.Assert(IsShowing);
        _speedLabel.text = SpeedFormat.Inject(Unit.Data.UnitFullSpeedValue);
    }


    #endregion
}

