// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResearchWindow.cs
// A full screen window that shows the Research (aka Technology Tree) Form. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// A full screen window that shows the Research (aka Technology Tree) Form. 
/// </summary>
public class ResearchWindow : AFormWindow<ResearchWindow> {

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
    }

    protected override IEnumerable<AForm> GetChildFormsToInitialize() {
        return gameObject.GetSafeComponentsInChildren<AForm>(excludeSelf: true, includeInactive: true);
    }

    public void Show() {
        var form = PrepareForm(FormID.Research);
        ShowForm(form);
    }

    #region Event and Property Change Handlers

    #endregion

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        // TODO
    }

    #endregion

}

