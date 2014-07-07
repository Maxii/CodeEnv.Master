// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFocusableItemPresenter.cs
// An abstract base MVPresenter associated with AFocusableViews and AFollowableViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An abstract base MVPresenter associated with AFocusableViews and AFollowableViews.
    /// </summary>
    public abstract class AFocusableItemPresenter : AItemPresenter {

        protected ICameraControl _cameraControl = References.CameraControl;

        public AFocusableItemPresenter(IViewable view)
            : base(view) {
        }

        public void OnIsFocus() {
            _cameraControl.CurrentFocus = View as ICameraFocusable;
        }
    }
}

