// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdPresenter.cs
// An MVPresenter associated with a FleetView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// An MVPresenter associated with a FleetView.
    /// </summary>
    public class FleetCmdPresenter : AUnitCommandPresenter {

        public new IFleetCmdModel Model { get { return base.Model as IFleetCmdModel; } }

        protected new FleetCmdItemData Data { get { return base.Data as FleetCmdItemData; } }

        protected new IFleetCmdViewable View { get { return base.View as IFleetCmdViewable; } }

        public FleetCmdPresenter(IFleetCmdViewable view)
            : base(view) {
            Subscribe();
        }

        protected override void Subscribe() {
            base.Subscribe();
            Model.onCoursePlotChanged += OnCoursePlotChanged;
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var hudPublisher = new GuiHudPublisher<FleetCmdItemData>(Data);
            hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health, GuiHudLineKeys.TargetDistance);
            return hudPublisher;
        }

        public Reference<float> GetFleetSpeedReference() {
            return new Reference<float>(() => Data.CurrentSpeed);
        }

        protected override IIcon MakeCmdIconInstance() {
            return FleetIconFactory.Instance.MakeInstance(Data, View.PlayerIntel);
        }

        public override void OnIsSelectedChanged() {
            base.OnIsSelectedChanged();
            View.AssessShowPlottedPath(Model.Course);
        }

        private void OnCoursePlotChanged() {
            View.AssessShowPlottedPath(Model.Course);
        }

        /// <summary>
        /// Gets the reference to the potentially changing location of the fleet's destination.
        /// </summary>
        /// <returns></returns>
        public Reference<Vector3> GetDestinationReference() {
            return Model.Destination;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

