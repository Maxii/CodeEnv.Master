// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APopupDialogForm.cs
// Abstract base class for Dialog Forms that popup.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Abstract base class for Dialog Forms that popup.
/// </summary>
public abstract class APopupDialogForm : AForm {

    private DialogWindow.DialogSettings _settings;
    public DialogWindow.DialogSettings Settings {
        get { return _settings; }
        set {
            D.AssertNull(_settings);    // occurs only once between Resets
            _settings = value;
        }
    }

    public sealed override void PopulateValues() {  // Called by AFormWindow just before window shows form
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        InitializeMenuControls();
    }

    protected abstract void InitializeMenuControls();

    protected abstract void UnsubscribeFromMenuControls();

    protected override void ResetForReuse_Internal() {
        if (Settings != null) {
            UnsubscribeFromMenuControls();
            _settings = null;
        }
    }

    protected override void Cleanup() {
        if (Settings != null) {
            UnsubscribeFromMenuControls();
        }
    }

}

