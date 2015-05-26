// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiPopupList.cs
// Abstract generic base class for popup lists used in the Gui, pre-wired with Tooltip functionality.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract generic base class for popup lists used in the Gui, pre-wired with Tooltip functionality.
/// </summary>
/// <typeparam name="T">Limited to Types supported by PlayerPrefsManager (Enum, string, int, float?)</typeparam>
public abstract class AGuiPopupList<T> : AGuiMenuElement {

    /// <summary>
    /// Flag indicating whether <c>Random</c> is included in the selection choices.
    /// </summary>
    protected virtual bool IncludesRandom { get { return false; } }

    private string _defaultSelectionValue;
    protected string DefaultSelectionValue {
        get {
            if (_defaultSelectionValue == null) { return _popupList.items[Constants.Zero]; }
            return _defaultSelectionValue;
        }
        set { SetProperty<string>(ref _defaultSelectionValue, value, "DefaultSelectionValue", OnDefaultSelectionValueChanged); }
    }

    /// <summary>
    /// The name values to initially use populating the popup list.
    /// Names can be subsequently removed from use using RemoveNameValue(nameValue).
    /// </summary>
    protected abstract string[] NameValues { get; }

    protected UIPopupList _popupList;

    protected override void Awake() {
        base.Awake();
        ConfigurePopupList();
        InitializeListValues(NameValues);
        InitializeSelection();
        // don't receive events until initializing is complete
        EventDelegate.Add(_popupList.onChange, OnPopupListSelectionChanged);
    }

    /// <summary>
    /// Configures the popupList prior to initializing list values or the starting selection.
    /// </summary>
    private void ConfigurePopupList() {
        _popupList = gameObject.GetSafeMonoBehaviour<UIPopupList>();
        UILabel label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        EventDelegate.Add(_popupList.onChange, label.SetCurrentSelection);
    }

    /// <summary>
    /// Assign all the values in the popupList.
    /// </summary>
    /// <remarks>Must be called in Awake() as UIPopupList makes a selectionName change to the item[0] in Start()</remarks>
    private void InitializeListValues(string[] nameValues) {
        _popupList.items.Clear();
        nameValues.ForAll(nv => _popupList.items.Add(nv));
        Validate();
    }

    /// <summary>
    /// Removes the name value from the PopupList's available choices.
    /// Allows dynamic adjustment in the available choices to be made by derived classes.
    /// </summary>
    /// <param name="nameValue">The name value.</param>
    protected void RemoveNameValue(string nameValue) {
        D.Assert(NameValues.Contains(nameValue));   // name might not be present in the list
        InitializeListValues(NameValues.Except(nameValue).ToArray());
        InitializeSelection();
    }

    /// <summary>
    /// Select the PopupList item that is the starting selection.
    /// </summary>
    /// <remarks>Called in the Awake sequence as UIPopupList will make
    /// a selectionName change to item[0] in Start() if not already set.
    /// </remarks>
    private void InitializeSelection() {
        string prefsPropertyName = ElementID.PreferencePropertyName();
        if (prefsPropertyName != null) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            string nameValue = propertyGet().ToString();    // gets the value of the PlayerPrefsManager Property named prefsPropertyName
            if (!_popupList.items.Contains(nameValue)) {
                // the prefs name value has been removed from the available choices
                D.WarnContext("Prefs NameValue {0} is no longer available to choose. Choosing default {1}.".Inject(nameValue, DefaultSelectionValue), gameObject);
                nameValue = DefaultSelectionValue;
            }
            _popupList.value = nameValue;
        }
        else {
            // no pref stored for this ElementID
            _popupList.value = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : DefaultSelectionValue;
        }
        //D.Log("GuiElement [{0}] selection initialized to {1}.", ElementID.GetName(), _popupList.value);
    }

    private void OnDefaultSelectionValueChanged() {
        if (!_popupList.items.Contains(DefaultSelectionValue)) {
            // the value is not among the available choices, probably because it was removed
            D.WarnContext("DefaultSelectionValue {0} not among available value choices. Reverting to {1}.".Inject(DefaultSelectionValue, _popupList.items[0]), gameObject);
            _defaultSelectionValue = null;  // forces use of _popupList.items[0]
        }
        InitializeSelection();
    }

    /// <summary>
    /// Called when a selection change has been made. Default does nothing.
    /// </summary>
    protected virtual void OnPopupListSelectionChanged() { }

    private void Validate() {
        if (IncludesRandom) {
            D.Assert(_popupList.items.Contains("Random"));
        }
        else {
            D.Assert(!_popupList.items.Contains("Random"));
        }
    }

    #region PlayerPrefs Reflection-based Property Acquisition Archive

    //private void InitializeSelection() {
    //    if (HasPreference) {
    //        string prefsPropertyName = ElementID.PreferencePropertyName();
    //        PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
    //        if (propertyInfo == null) {
    //            D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
    //        }
    //        Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
    //        string nameValue = propertyGet().ToString();    // gets the value of the PlayerPrefsManager Property named prefsPropertyName
    //        if (!_popupList.items.Contains(nameValue)) {
    //            // the prefs name value has been removed from the available choices
    //            D.WarnContext("Prefs NameValue {0} is no longer available to choose. Choosing default {1}.".Inject(nameValue, DefaultSelectionValue), gameObject);
    //            nameValue = DefaultSelectionValue;
    //        }
    //        _popupList.value = nameValue;
    //    }
    //    else {
    //        _popupList.value = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : DefaultSelectionValue;
    //    }
    //}

    #endregion

}

