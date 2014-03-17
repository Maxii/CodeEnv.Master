// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdPresenter.cs
//  An MVPresenter associated with a Settlement View.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    ///  An MVPresenter associated with a Settlement View.
    /// </summary>
    public class SettlementCmdPresenter : AUnitCommandPresenter {

        public new ISettlementCmdModel Model {
            get { return base.Model as ISettlementCmdModel; }
            protected set { base.Model = value; }
        }

        public SettlementCmdPresenter(ICommandViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<ISettlementCmdModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<SettlementCmdData>(Model.Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        protected override IIcon MakeCmdIconInstance() {
            return SettlementIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

