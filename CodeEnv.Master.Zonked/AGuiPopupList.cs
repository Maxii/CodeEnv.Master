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

    private string _defaultSelectionName;
    public string DefaultSelectionName {
        get {
            if (_defaultSelectionName == null) { return _popupList.items[Constants.Zero]; }
            return _defaultSelectionName;
        }
        set { SetProperty<string>(ref _defaultSelectionName, value, "DefaultSelectionName", OnDefaultSelectionNameChanged); }
    }

    /// <summary>
    /// The names of the values to initially use populating the popup list.
    /// Names can be subsequently removed from use using RemoveValueName(valueName).
    /// </summary>
    protected abstract string[] ValueNames { get; }

    protected UIPopupList _popupList;

    protected override void Awake() {
        base.Awake();
        ConfigurePopupList();
        InitializeListValues(ValueNames);
        RefreshSelection();
        // don't receive events until initializing is complete
        EventDelegate.Add(_popupList.onChange, OnPopupListSelectionChanged);
    }

    /// <summary>
    /// Configures the popupList prior to initializing list values or the starting selection.
    /// </summary>
    private void ConfigurePopupList() {
        _popupList = gameObject.GetSafeMonoBehaviour<UIPopupList>();
        UILabel label = gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
        EventDelegate.Add(_popupList.onChange, label.SetCurrentSelection);
    }

    /// <summary>
    /// Assign all the values in the popupList.
    /// </summary>
    /// <remarks>Must be called in Awake() as UIPopupList makes a selectionName change to the item[0] in Start()</remarks>
    private void InitializeListValues(string[] valueNames) {
        _popupList.items.Clear();
        valueNames.ForAll(n => _popupList.AddItem(n));
        Validate();
    }

    /// <summary>
    /// Removes the valueName from the PopupList's available choices.
    /// Allows dynamic adjustment in the available choices to be made by derived classes.
    /// </summary>
    /// <param name="valueName">The name of the value of Type T.</param>
    //protected void RemoveValueName(string valueName) {
    //    D.Assert(ValueNames.Contains(valueName));   // name might not be present in the list
    //    InitializeListValues(ValueNames.Except(valueName).ToArray());
    //    InitializeSelection();
    //}

    /// <summary>
    /// Select the PopupList item that is the starting selection.
    /// </summary>
    /// <remarks>Called in the Awake sequence as UIPopupList will make
    /// a selectionName change to item[0] in Start() if not already set.
    /// </remarks>
    public void RefreshSelection() {
        //D.Log("{0}.RefreshSelection() called.", ElementID.GetValueName());
        string valueName;
        string prefsPropertyName = ElementID.PreferencePropertyName();
        if (prefsPropertyName != null) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            valueName = propertyGet().ToString();    // gets the value of the PlayerPrefsManager Property named prefsPropertyName
            D.Log("{0} Prefs Property value = {1}.", ElementID.GetValueName(), valueName);
            if (!_popupList.items.Contains(valueName)) {
                // the prefs value has been removed from the available choices
                D.LogContext("{0} Prefs value {1} is no longer available to select. Selecting default {2}."
                    .Inject(ElementID.GetValueName(), valueName, DefaultSelectionName), gameObject);
                valueName = DefaultSelectionName;
            }
            //_popupList.value = valueName;
        }
        else {
            // no pref stored for this ElementID
            valueName = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : DefaultSelectionName;
            //_popupList.value = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : DefaultSelectionName;
        }

        if (_popupList.value != valueName) {    // selectionChanged delegate fires every time value is set, even if with the same value
            D.Log("{0} selection refreshing from {1} to {2}.", ElementID.GetValueName(), _popupList.value, valueName);
            _popupList.value = valueName;
        }
    }

    private void OnDefaultSelectionNameChanged() {
        D.Log("{0}.DefaultSelectionName changed to {1}.", ElementID.GetValueName(), DefaultSelectionName);
        if (!DefaultSelectionName.IsNullOrEmpty() && !_popupList.items.Contains(DefaultSelectionName)) {
            // the value is not among the available choices, probably because it was removed.
            D.WarnContext("{0} DefaultSelection {1} not among available choices. Adding back.".Inject(ElementID.GetValueName(), DefaultSelectionName), gameObject);
            _popupList.AddItem(DefaultSelectionName);
        }
        //RefreshSelection();
    }
    //private void OnDefaultSelectionNameChanged() {
    //    if (!_popupList.items.Contains(DefaultSelectionName)) {
    //        // the value is not among the available choices, probably because it was removed.
    //        D.WarnContext("DefaultSelectionValue {0} not among available choices. Reverting to {1}.".Inject(DefaultSelectionName, _popupList.items[0]), gameObject);
    //        _defaultSelectionName = null;  // forces use of _popupList.items[0]
    //    }
    //    InitializeSelection();
    //}

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

