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

    protected new ISystemViewable View {
        get { return base.View as ISystemViewable; }
    }

    private IViewable[] _childViewsInSystem;

    public SystemPresenter(IViewable view)
        : base(view) {
        _childViewsInSystem = _viewGameObject.GetSafeInterfacesInChildren<IViewable>().Except(view).ToArray();
    }

    protected override AItem InitilizeItemLinkage() {
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

    public void OnPlayerIntelLevelChanged() {
        _childViewsInSystem.ForAll<IViewable>(cov => cov.PlayerIntelLevel = View.PlayerIntelLevel);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

