// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFocusablePresenter.cs
// An abstract base MVPresenter associated with AFocusableViews and AFollowableViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// An abstract base MVPresenter associated with AFocusableViews and AFollowableViews.
/// </summary>
public abstract class AFocusablePresenter : APresenter {

    public AFocusablePresenter(IViewable view)
        : base(view) {
    }

    public void OnIsFocus() {
        CameraControl.Instance.CurrentFocus = View as ICameraFocusable;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

