﻿// --------------------------------------------------------------------------------------------------------------------
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

        public new IStarbaseCmdModel Model {
            get { return base.Model as IStarbaseCmdModel; }
            protected set { base.Model = value; }
        }

        public StarbaseCmdPresenter(ICommandViewable view)
            : base(view) {
            Subscribe();
        }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<IStarbaseCmdModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            var publisher = new GuiHudPublisher<StarbaseCmdData>(Model.Data);
            publisher.SetOptionalUpdateKeys(GuiHudLineKeys.Health);
            return publisher;
        }

        protected override IIcon MakeCmdIconInstance() {
            return StarbaseIconFactory.Instance.MakeInstance(Model.Data, View.PlayerIntel);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}
