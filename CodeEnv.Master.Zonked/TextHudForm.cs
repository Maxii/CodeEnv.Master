// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextHudForm.cs
// HudForm supporting text, equivalent to UITooltip functionality.
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
/// HudForm supporting text, equivalent to UITooltip functionality.
/// </summary>
[System.Obsolete]
public class TextHudForm : AHudForm {

    public override HudFormID FormID { get { return HudFormID.Text; } }

    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        _label = gameObject.GetSafeMonoBehaviour<UILabel>();
    }

    protected override void AssignValuesToWidgets() {
        var content = FormContent as TextHudFormContent;
        _label.text = content.Text;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

