// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiLabelReadoutBase.cs
// Base class for Dynamic Gui Labels (used as readouts) built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Base class for Dynamic Gui Labels (used as readouts) built with NGUI.
/// </summary>
public abstract class AGuiLabelReadoutBase : GuiTooltip {

    protected GameEventManager eventMgr;
    protected UILabel readoutLabel;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        eventMgr = GameEventManager.Instance;
        readoutLabel = gameObject.GetSafeMonoBehaviourComponent<UILabel>();
    }
}

