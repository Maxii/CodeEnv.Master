// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiPopupListBase.cs
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
public abstract class AGuiPopupListBase : AGuiTooltip {

    protected UIPopupList _popupList;

    protected override void Awake() {
        base.Awake();
        _popupList = gameObject.GetSafeMonoBehaviourComponent<UIPopupList>();
        ConfigurePopupList();
        InitializeListValues();
        InitializeSelection();
        // don't receive events until initializing is complete
        EventDelegate.Add(_popupList.onChange, OnPopupListSelectionChange);
    }

    /// <summary>
    /// Virtual method that does any required configuration of the popupList
    /// prior to initializing list values or the selection.
    /// </summary>
    protected virtual void ConfigurePopupList() {
        UILabel label = gameObject.GetSafeMonoBehaviourComponentInChildren<UILabel>();
        EventDelegate.Add(_popupList.onChange, label.SetCurrentSelection);
    }

    /// <summary>
    /// Abstract method to initialize the values in the popupList.
    /// </summary>
    protected abstract void InitializeListValues();

    /// <summary>
    /// Abstract method for initializing the PopupList selectionName.
    /// </summary>
    /// <remarks>Called in the Awake sequence as UIPopupList will make
    /// a selectionName change to item[0] in Start() if not already set.
    /// </remarks>
    protected abstract void InitializeSelection();

    protected virtual void OnPopupListSelectionChange() { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

