// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHudForm.cs
// Abstract base class for HudForms.
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
/// Abstract base class for HudForms. A HudForm is a collection of UIWidgets
///in an arrangement that can be displayed by a HudWindow. AHudForms are
///populated with content to display by feeding them AHudFormContents.
/// </summary>
[System.Obsolete]
public abstract class AHudForm : AMonoBase {

    public abstract HudFormID FormID { get; }

    private AHudFormContent _formContent;
    public AHudFormContent FormContent {
        get { return _formContent; }
        set { SetProperty<AHudFormContent>(ref _formContent, value, "FormContent", OnHudFormContentChanged); }
    }

    private void OnHudFormContentChanged() {
        D.Assert(FormContent.FormID == FormID);
        AssignValuesToWidgets();
    }

    protected abstract void AssignValuesToWidgets();

}

