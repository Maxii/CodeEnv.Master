﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiLabelReadoutBase.cs
// Base class for Dynamic Gui Labels (used as readouts) built with NGUI. Supports Tooltips.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Base class for Dynamic Gui Labels (used as readouts) built with NGUI. Supports Tooltips.
/// </summary>
public abstract class AGuiLabelReadoutBase : GuiTooltip {

    protected GameEventManager _eventMgr;
    protected UILabel _readoutLabel;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _eventMgr = GameEventManager.Instance;
        _readoutLabel = gameObject.GetSafeMonoBehaviourComponent<UILabel>();
    }
}

