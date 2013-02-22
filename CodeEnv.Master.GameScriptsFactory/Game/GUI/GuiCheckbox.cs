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

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using System.Reflection;

/// <summary>
/// Standalone but extendable GuiCheckbox class that implements PlayerPrefsManager property initialization and Tooltip
/// functionality. Also pre-registers with the NGUI Checkbox delegate to receive OnCheckboxStateChange events.
/// </summary>
public class GuiCheckbox : GuiTooltip {

    public string propertyName = string.Empty;

    protected UICheckbox checkbox;

    void Start() {
        Initialize();
    }

    /// <summary>
    /// Can override. Remember base.Initialize();  The value for propertyName must be set before 
    /// base.Initialize() is called.
    /// </summary>
    protected virtual void Initialize() {
        checkbox = gameObject.GetSafeMonoBehaviourComponent<UICheckbox>();
        checkbox.onStateChange += OnCheckboxStateChange;
        InitializeCheckbox();
    }

    /// <summary>
    /// Initializes the checkbox state with the value held in PlayerPrefsManager. Uses Reflection to find the PlayerPrefsManager
    /// property named, then creates a Property Delegate to acquire the initialization value.
    /// </summary>
    private void InitializeCheckbox() {
        if (!string.IsNullOrEmpty(propertyName)) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(propertyName);
            if (propertyInfo == null) {
                Debug.LogError("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
                return;
            }
            Func<bool> propertyGet = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            checkbox.isChecked = propertyGet();
        }
        else {
            Debug.LogWarning("The PlayerPrefsManager Property has not been named for {0}.".Inject(gameObject.name));
        }
    }

    protected virtual void OnCheckboxStateChange(bool state) {
        //System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        //Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));
    }

    // IDisposable Note: No reason to remove Ngui event listeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

