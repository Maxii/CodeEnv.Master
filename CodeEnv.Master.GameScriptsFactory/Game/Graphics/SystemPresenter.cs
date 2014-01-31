// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemPresenter.cs
// An MVPresenter associated with a SystemView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a SystemView.
/// </summary>
public class SystemPresenter : AFocusablePresenter {

    public new SystemItem Item {
        get { return base.Item as SystemItem; }
        protected set { base.Item = value; }
    }

    public SystemPresenter(IViewable view) : base(view) { }

    protected override AItem AcquireItemReference() {
        return UnityUtility.ValidateMonoBehaviourPresence<SystemItem>(_viewGameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SystemData>(Item.Data);
    }

    public void RequestContextMenu(bool isDown) {
        SettlementData settlement = Item.Data.Settlement;
        //D.Log("Settlement null = {0}, isHumanOwner = {1}.", settlement == null, settlement.Owner.IsHuman);
        if (settlement != null && (DebugSettings.Instance.AllowEnemyOrders || settlement.Owner.IsHuman)) {
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    public void OnIsSelected() {
        SelectionManager.Instance.CurrentSelection = View as ISelectable;
    }

    // UNCLEAR what should the relationship be between System.IntelCoverage and Settlement/Planet?, implemented Settlement for now
    public void OnPlayerIntelCoverageChanged() {
        // construct list each time as Settlement presence can change with time
        SettlementView settlementView = _viewGameObject.GetComponentInChildren<SettlementView>();
        if (settlementView != null) {
            settlementView.PlayerIntel.CurrentCoverage = View.PlayerIntel.CurrentCoverage;
        }
        // The approach below acquired all views in the system and gave them the same IntelCoverage as the system
        //IEnumerable<IViewable> childViewsInSystem = _viewGameObject.GetSafeInterfacesInChildren<IViewable>().Except(View);
        //childViewsInSystem.ForAll<IViewable>(v => v.PlayerIntel.CurrentCoverage = View.PlayerIntel.CurrentCoverage);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

