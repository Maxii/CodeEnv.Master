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

    /// <summary>
    /// An MVPresenter associated with a FleetView.
    /// </summary>
    public class FleetCmdPresenter : AUnitCommandPresenter {

        public new IFleetCmdModel Model {
            get { return base.Model as IFleetCmdModel; }
            protected set { base.Model = value; }
        }

        public FleetCmdPresenter(ICommandViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<IFleetCmdModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var hudPublisher = new GuiHudPublisher<FleetCmdData>(Model.Data);
            hudPublisher.SetOptionalUpdateKeys(GuiHudLineKeys.Speed, GuiHudLineKeys.Health);
            return hudPublisher;
        }

        public Reference<float> GetFleetSpeedReference() {
            return new Reference<float>(() => Model.Data.CurrentSpeed);
        }

        protected override IIcon MakeCmdIconInstance() {
            return FleetIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

