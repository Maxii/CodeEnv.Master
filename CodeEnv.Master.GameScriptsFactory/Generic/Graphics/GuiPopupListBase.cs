// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPopupListBase.cs
//  Generic GuiPopupListBase class that implements PlayerPrefsManager property initialization and Tooltip functionality.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using System.Reflection;

/// <summary>
/// Generic GuiPopupListBase class that implements PlayerPrefsManager property initialization and Tooltip
/// functionality. Also pre-registers with the NGUI PopupList delegate to receive OnPopupListSelectionChange events.
/// </summary>
public abstract class GuiPopupListBase<T> : GuiTooltip where T : struct {

    public string propertyName = string.Empty;
    protected UIPopupList popupList;

    void Start() {
        Initialize();
    }

    /// <summary>
    /// Can override. Remember base.Initialize(); The value for propertyName must be set before 
    /// base.Initialize() is called.
    /// </summary>
    protected virtual void Initialize() {
        popupList = gameObject.GetSafeMonoBehaviourComponent<UIPopupList>();
        popupList.onSelectionChange += OnPopupListSelectionChange;
        InitializePopupList();
    }

    protected virtual void OnPopupListSelectionChange(string item) { }

    /// <summary>
    /// Initializes the PopupList selection with the value held in PlayerPrefsManager. Uses Reflection to find the PlayerPrefsManager
    /// property named, then creates a Property Delegate to acquire the initialization value.
    /// </summary>
    private void InitializePopupList() {
        if (!string.IsNullOrEmpty(propertyName)) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(propertyName);
            if (propertyInfo == null) {
                Debug.LogError("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
                return;
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            popupList.selection = propertyGet().ToString();
        }
        else {
            Debug.LogWarning("The PlayerPrefsManager Property has not been named for {0}.".Inject(gameObject.name));
        }
    }

    protected void WarnOnUnrecognizedItem(string item) {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string callerIdMessage = ". Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
        Debug.LogWarning("Item used in PopupList not found: " + item + callerIdMessage);
    }

    // IDisposable Note: No reason to remove Ngui event listeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

