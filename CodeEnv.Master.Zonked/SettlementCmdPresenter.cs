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

        protected new SettlementCmdItemData Data { get { return base.Data as SettlementCmdItemData; } }

        public SettlementCmdPresenter(ICommandViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<SettlementCmdItemData>(Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        protected override IIcon MakeCmdIconInstance() {
            return SettlementIconFactory.Instance.MakeInstance(Data, View.PlayerIntel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

