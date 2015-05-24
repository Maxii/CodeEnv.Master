// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiLabelReadout.cs
// Abstract base class for Gui Labels used as readouts. Supports Tooltips.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Gui Labels used as readouts. Supports Tooltips.
/// </summary>
public abstract class AGuiLabelReadout : AGuiTooltip {

    protected UILabel _readoutLabel;

    protected override void Awake() {
        base.Awake();
        _readoutLabel = gameObject.GetSafeMonoBehaviourInImmediateChildren<UILabel>();
    }

    protected virtual void RefreshReadout(string text, GameColor color = GameColor.White) {
        _readoutLabel.text = text;
        _readoutLabel.color = color.ToUnityColor();
    }
}

