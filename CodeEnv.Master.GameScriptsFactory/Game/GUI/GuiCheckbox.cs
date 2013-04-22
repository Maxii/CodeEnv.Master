// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCheckbox.cs
// Standalone but extendable GuiCheckbox class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Standalone but extendable GuiCheckbox class that implements PlayerPrefsManager property initialization and Tooltip
/// functionality.
/// </summary>
public class GuiCheckbox : GuiTooltip {

    /// <summary>
    /// The name of the PlayerPrefsManager property for this checkbox. Warning: If clients inherit from this 
    /// class and wish to set this value programatically, it must be set in InitializeOnAwake()
    /// BEFORE base.InitializeOnAwake() is called.
    /// </summary>
    public string propertyName = string.Empty;
    protected UICheckbox checkbox;

    void Awake() {
        InitializeOnAwake();
    }

    /// <summary>
    /// Can override. Remember base.InitializeOnAwake();  The tPrefsValue for propertyName must be set before 
    /// base.InitializeOnAwake() is called.
    /// </summary>
    protected virtual void InitializeOnAwake() {
        checkbox = gameObject.GetSafeMonoBehaviourComponent<UICheckbox>();
        InitializeCheckbox();
        // don't receive events until initializing is complete
        checkbox.onStateChange += OnCheckboxStateChange;
    }

    /// <summary>
    /// Initializes the checkbox state with the tPrefsValue held in the PlayerPrefsManager property named in <c>propertyName</c>, 
    /// or if propertyName is empty, sets the state to false. Uses Reflection to find the PlayerPrefsManager
    /// property named, then creates a Property Delegate to acquire the initialization tPrefsValue.
    /// </summary>
    private void InitializeCheckbox() {
        if (!string.IsNullOrEmpty(propertyName)) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(propertyName);
            if (propertyInfo == null) {
                Debug.LogError("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
                return;
            }
            Func<bool> propertyGet = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            checkbox.startsChecked = propertyGet(); // startsChecked used as UICheckbox Start() uses it to initialize the checkbox
        }
        else {
            Debug.LogWarning("No PlayerPrefsManager Property named for {0} so setting false.".Inject(gameObject.name));
            checkbox.startsChecked = false;
        }
    }

    protected virtual void OnCheckboxStateChange(bool state) { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

