// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiMenuCheckbox.cs
// Standalone but extensible class for Gui Checkboxes that are elements of a menu with an Accept button.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Standalone but extensible class for Gui Checkboxes that are elements of a menu with an Accept button.
/// </summary>
public class GuiMenuCheckbox : AGuiMenuElement {

    public bool hasPreference;

    public string tooltip = string.Empty;

    public GuiMenuElementID elementID;

    public override bool HasPreference { get { return hasPreference; } }
    protected override string TooltipContent { get { return tooltip; } }
    public override GuiMenuElementID ElementID { get { return elementID; } }

    protected UIToggle _checkbox;

    protected override void Awake() {
        base.Awake();
        InitializeCheckbox();
    }

    /// <summary>
    /// Initializes the checkbox. If the checkbox has a preference saved in PlayerPrefsManager that value is used. If not
    /// the checkbox value is set to false.
    /// </summary>
    private void InitializeCheckbox() {
        _checkbox = gameObject.GetSafeMonoBehaviourComponent<UIToggle>();

        if (HasPreference) {
            string prefsPropertyName = ElementID.PreferencePropertyName();
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
            }
            Func<bool> propertyGet = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            _checkbox.startsActive = propertyGet(); // startsActive (aka checked) used as UIToggle Start() uses it to initialize the checkbox
        }
        else {
            D.WarnContext("Checkbox {0} does not use a {1} preference. Initializing to false.".Inject(gameObject.name, typeof(PlayerPrefsManager).Name), gameObject);
            _checkbox.startsActive = false;
        }

        // don't receive events until initializing is complete
        EventDelegate.Add(_checkbox.onChange, OnCheckboxStateChange);
    }

    protected virtual void OnCheckboxStateChange() { }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.


    #region Archive

    /// <summary>
    /// The name of the PlayerPrefsManager property for this checkbox. 
    /// WARNING: If clients inherit from this class and wish to set this value programatically, 
    /// it must be set in Awake() BEFORE base.Awake() is called.
    /// </summary>
    //public string propertyName = string.Empty;

    ///// <summary>
    ///// Initializes the checkbox state with the value held in the PlayerPrefsManager property named in <c>propertyName</c>, 
    ///// or if propertyName is empty, unchecks the checkbox. Uses Reflection to find the PlayerPrefsManager
    ///// property named, then creates a Property Delegate to acquire the initialization value.
    ///// </summary>
    //private void InitializeCheckbox() {
    //    if (!string.IsNullOrEmpty(propertyName)) {
    //        PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(propertyName);
    //        if (propertyInfo == null) {
    //            D.Error("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
    //            return;
    //        }
    //        Func<bool> propertyGet = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
    //        _checkbox.startsActive = propertyGet(); // startsActive (aka checked) used as UIToggle Start() uses it to initialize the checkbox
    //    }
    //    else {
    //        D.Warn("No PlayerPrefsManager Property named for {0} so setting false.".Inject(gameObject.name));
    //        _checkbox.startsActive = false;
    //    }
    //}

    #endregion

}

