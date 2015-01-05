// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiPopupList.cs
//  An abstract base class for popup lists used in the Gui, pre-wired with Tooltip functionality.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// An abstract base class for popup lists used in the Gui, pre-wired with Tooltip functionality.
/// </summary>
public abstract class AGuiPopupList : AGuiMenuElement {

    protected UIPopupList _popupList;

    protected override void Awake() {
        base.Awake();
        ConfigurePopupList();
        InitializeListValues();
        InitializeSelection();
        // don't receive events until initializing is complete
        EventDelegate.Add(_popupList.onChange, OnPopupListSelectionChanged);
    }

    /// <summary>
    /// Configures the popupList prior to initializing list values or the starting selection.
    /// </summary>
    private void ConfigurePopupList() {
        _popupList = gameObject.GetSafeMonoBehaviourComponent<UIPopupList>();
        UILabel label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        EventDelegate.Add(_popupList.onChange, label.SetCurrentSelection);
    }

    /// <summary>
    /// Assign all the values in the popupList.
    /// </summary>
    protected abstract void InitializeListValues();

    /// <summary>
    /// Select the PopupList item that is the starting selection.
    /// </summary>
    /// <remarks>Called in the Awake sequence as UIPopupList will make
    /// a selectionName change to item[0] in Start() if not already set.
    /// </remarks>
    protected abstract void InitializeSelection();

    /// <summary>
    /// Called when a selection change has been made. Default does nothing.
    /// </summary>
    protected virtual void OnPopupListSelectionChanged() { }

}

