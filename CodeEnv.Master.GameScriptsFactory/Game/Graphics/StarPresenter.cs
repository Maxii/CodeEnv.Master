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

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// An MVPresenter associated with a StarView.
/// </summary>
public class StarPresenter : Presenter {

    private ISystemViewable _systemView;

    public StarPresenter(IViewable view)
        : base(view) {
        _systemView = _viewGameObject.GetSafeInterfaceInParents<ISystemViewable>();
    }

    public void OnHover(bool isOver) {
        _systemView.HighlightTrackingLabel(isOver);
    }

    public void OnLeftClick() {
        (_systemView as ISelectable).IsSelected = true;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

