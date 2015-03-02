// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdPresenter.cs
// An MVPresenter associated with a StarbaseView.
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
    /// An MVPresenter associated with a StarbaseView.
    /// </summary>
    public class StarbaseCmdPresenter : AUnitCommandPresenter {

        protected new StarbaseCmdData Data { get { return base.Data as StarbaseCmdData; } }

        public StarbaseCmdPresenter(ICommandViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<StarbaseCmdData>(Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        protected override IIcon MakeCmdIconInstance() {
            return StarbaseIconIDFactory.Instance.MakeInstance(Data, View.PlayerIntel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

