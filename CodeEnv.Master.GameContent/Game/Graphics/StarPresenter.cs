﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarPresenter.cs
// An MVPresenter associated with a StarView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// An MVPresenter associated with a StarView.
    /// </summary>
    public class StarPresenter : AFocusableItemPresenter {

        public new IStarModel Model {
            get { return base.Model as IStarModel; }
            protected set { base.Model = value; }
        }

        private IViewable _systemView;

        public StarPresenter(IViewable view)
            : base(view) {
            _systemView = _viewGameObject.GetSafeInterfaceInParents<IViewable>(excludeSelf: true);
        }

        protected override IModel AcquireModelReference() {
            return _viewGameObject.GetSafeInterface<IStarModel>();
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<StarData>(Model.Data);
        }

        public void OnHover(bool isOver) {
            (_systemView as IHighlightTrackingLabel).HighlightTrackingLabel(isOver);
        }

        public void OnLeftClick() {
            (_systemView as ISelectable).IsSelected = true;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

