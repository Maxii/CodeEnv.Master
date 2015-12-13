// --------------------------------------------------------------------------------------------------------------------
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

        protected new StarData Data { get { return base.Data as StarData; } }

        private IViewable _systemView;

        public StarPresenter(IViewable view)
            : base(view) {
            _systemView = _viewGameObject.GetSafeFirstInterfaceInParents<IViewable>(excludeSelf: true);
        }

        protected override IGuiHudPublisher InitializeHudPublisher() {
            return new GuiHudPublisher<StarData>(Data);
        }

        public void OnLeftClick() {
            if (_systemView.IsDiscernible) {
                (_systemView as ISelectable).IsSelected = true;
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

