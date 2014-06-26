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

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An MVPresenter associated with a SystemView.
    /// </summary>
    public class SystemPresenter : AFocusableItemPresenter {

        public new ISystemModel Model {
            get { return base.Model as ISystemModel; }
            protected set { base.Model = value; }
        }

        public SystemPresenter(IViewable view) : base(view) { }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<ISystemModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<SystemData>(Model.Data);
        }

        public void RequestContextMenu(bool isDown) {
            SettlementCmdData settlementData = Model.Data.SettlementData;
            //D.Log("Settlement null = {0}, isHumanOwner = {1}.", settlement == null, settlement.Owner.IsHuman);
            if (settlementData != null && (DebugSettings.Instance.AllowEnemyOrders || settlementData.Owner.IsHuman)) {
                _cameraControl.ShowContextMenuOnPress(isDown);
            }
        }

        public void OnIsSelected() {
            SelectionManager.Instance.CurrentSelection = View as ISelectable;
        }

        // UNCLEAR what should the relationship be between System.IntelCoverage and Settlement/Planet?, implemented Settlement for now
        public void OnPlayerIntelCoverageChanged() {
            // construct list each time as Settlement presence can change with time
            var settlementView = _viewGameObject.GetInterfaceInChildren<ICommandViewable>();
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
}

