// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Star.cs
// Manages a stationary Star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages a stationary Star.
/// </summary>
[System.Obsolete]
public class Star : StationaryItem {

    private SystemGraphics _systemGraphics;
    private SystemCreator _systemManager;

    protected override void Awake() {
        base.Awake();
        _systemManager = gameObject.GetSafeFirstMonoBehaviourInParents<SystemCreator>();
        _systemGraphics = gameObject.GetSafeFirstMonoBehaviourInParents<SystemGraphics>();
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        _systemGraphics.HighlightTrackingLabel(isOver);
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            _systemManager.OnLeftClick();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

