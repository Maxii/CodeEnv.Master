// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextHudElement.cs
// Hud Element supporting text, equivalent to UITooltip functionality.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Hud Element supporting text, equivalent to UITooltip functionality.
/// </summary>
public class TextHudElement : AHudElement {

    public override HudElementID ElementID { get { return HudElementID.Text; } }

    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        _label = gameObject.GetSafeMonoBehaviour<UILabel>();
    }

    protected override void AssignValuesToMembers() {
        var content = HudContent as TextHudContent;
        _label.text = content.Text;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

