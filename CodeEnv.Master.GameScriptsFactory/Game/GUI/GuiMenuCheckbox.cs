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
using UnityEngine.Serialization;

/// <summary>
/// Standalone but extensible class for Checkboxes that are elements of a menu with an Accept button.
/// </summary>
public class GuiMenuCheckbox : AGuiMenuElement {

    //[FormerlySerializedAs("tooltip")]
    [Tooltip("Optional tooltip")]
    [SerializeField]
    private string _tooltip = string.Empty;

    //[FormerlySerializedAs("elementID")]
    [Tooltip("The unique ID of this Checkbox GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    /// <summary>
    /// The default value to use if there is no stored preference for this Checkbox.
    /// Default setting is <c>false</c>. 
    /// </summary>
    protected bool DefaultValue { get; set; }

    protected sealed override string TooltipContent { get { return _tooltip; } }

    /// <summary>
    /// Flag indicates whether this checkbox should initialize its selection itself.
    /// The default setting is <c>true</c>. If false, the Checkbox does nothing 
    /// and relies on a derived class to initialize its selection.
    /// </summary>
    protected virtual bool SelfInitializeSelection { get { return true; } }

    protected UIToggle _checkbox;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        if (SelfInitializeSelection) {
            InitializeSelection();
        }
    }

    protected virtual void InitializeValuesAndReferences() {
        _checkbox = gameObject.GetSafeComponent<UIToggle>();
        EventDelegate.Add(_checkbox.onChange, OnCheckboxStateSet);
    }

    private void InitializeSelection() {
        TryMakePreferenceSelection();
    }

    /// <summary>
    /// Tries to set the state of the checkbox based on a stored preference (held by PlayerPrefsManager).
    /// If there is a preference value stored, that value is used and the method returns <c>true</c>. 
    /// If there is no preference then the DefaultValue is used and the method returns <c>false</c>.
    /// </summary>
    /// <returns></returns>
    protected bool TryMakePreferenceSelection() {
        bool isPreferenceUsed = false;
        bool isChecked;
        string prefsPropertyName = ElementID.PreferencePropertyName();
        if (prefsPropertyName != null) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext(this, "No {0} property named {1} found!", typeof(PlayerPrefsManager).Name, prefsPropertyName);
            }
            Func<bool> propertyGet = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            isChecked = propertyGet();
            isPreferenceUsed = true;
        }
        else {
            // no pref stored for this ElementID
            D.WarnContext(this, "{0} does not have a preference. Using default {1}.", ElementID.GetValueName(), DefaultValue);
            isChecked = DefaultValue;
        }
        _checkbox.value = isChecked;
        return isPreferenceUsed;
    }

    /// <summary>
    /// Called when the state of the UIToggle is set. Default does nothing.
    /// </summary>
    protected virtual void OnCheckboxStateSet() { }

    protected override void Cleanup() {
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        EventDelegate.Remove(_checkbox.onChange, OnCheckboxStateSet);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region PlayerPrefs Reflection-based Property Acquisition Archive

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

