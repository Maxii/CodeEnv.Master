// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiMenuPopupList.cs
//  Abstract generic base class for popup lists that are elements of a menu with an Accept button.
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
/// Abstract generic base class for popup lists that are elements of a menu with an Accept button.
/// </summary>
/// <typeparam name="T">Limited to Types supported by PlayerPrefsManager (Enum, string, int, float?)</typeparam>
public abstract class AGuiMenuPopupList<T> : AGuiMenuElement {

    /// <summary>
    /// Flag indicating whether this popupList should initialize its selection itself.
    /// Default is <c>true</c>. If false, the popup does nothing and relies on a derived class to initialize its selection.
    /// </summary>
    protected virtual bool SelfInitializeSelection { get { return true; } }

    /// <summary>
    /// Flag indicating whether <c>Random</c> is included in the selection choices.
    /// Default is <c>false</c>.
    /// </summary>
    protected virtual bool IncludesRandom { get { return false; } }

    private string _defaultSelection;
    /// <summary>
    /// The name of the T value to use as the selection default in cases where there is
    /// no preference stored. If null, the first item in Choices will be used.
    /// </summary>
    protected string DefaultSelection {
        private get {
            if (_defaultSelection == null) {
                //D.Log("{0} default selection is null. Selecting Item 0: {1}.", ElementID.GetValueName(), _popupList.items[0]);
                return _popupList.items[0];
            }
            return _defaultSelection;
        }
        set { _defaultSelection = value; }
    }

    /// <summary>
    /// The names of the T values to use populating the choices in the popup list.
    /// </summary>
    protected abstract string[] Choices { get; }

    protected UIPopupList _popupList;
    protected UILabel _label;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        if (SelfInitializeSelection) {
            InitializeSelection();
        }
    }

    protected virtual void InitializeValuesAndReferences() {
        _popupList = UnityUtility.ValidateMonoBehaviourPresence<UIPopupList>(gameObject);
        _label = gameObject.GetSafeFirstMonoBehaviourInChildren<UILabel>();
        EventDelegate.Add(_popupList.onChange, _label.SetCurrentSelection);
        EventDelegate.Add(_popupList.onChange, OnPopupListSelection);
    }

    private void InitializeSelection() {
        AssignSelectionChoices();
        TryMakePreferenceSelection();
    }

    /// <summary>
    /// Assign the choices available for selection in the popupList from Choices.
    /// </summary>
    protected void AssignSelectionChoices() {
        _popupList.items.Clear();
        Choices.ForAll(n => _popupList.AddItem(n));
        Validate();
    }

    /// <summary>
    /// Tries to select the list's preference (held by PlayerPrefsManager) from the choices available to the popupList.  
    /// If there is a preference value stored and it is present in the list's choices, that value is selected and the method 
    /// returns <c>true</c>. If there is no preference stored or the preference is not one of the available choices, 
    /// then the DefaultSelection is selected and the method returns <c>false</c>. If DefaultSelection has not
    /// been set (== null), then the first choice in the list is selected returning <c>false</c>.
    /// Warning: The list's choices must be populated by calling AssignSelectionChoices() prior to attempting this selection.
    /// </summary>
    /// <returns></returns>
    protected virtual bool TryMakePreferenceSelection() {
        bool isPrefSelected = true;
        string valueName;
        string prefsPropertyName = ElementID.PreferencePropertyName();
        if (prefsPropertyName != null) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
                isPrefSelected = false;
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            valueName = propertyGet().ToString();    // gets the value of the PlayerPrefsManager Property named prefsPropertyName
            //D.Log("{0} is using preference value {1} as its selection.", ElementID.GetValueName(), valueName);
            if (!_popupList.items.Contains(valueName)) {
                // the prefs value is not one of the available choices
                //D.LogContext("{0} Prefs value {1} is not among choices available to select. Using default {2}."
                //    .Inject(ElementID.GetValueName(), valueName, DefaultSelection), gameObject);
                valueName = DefaultSelection;
                isPrefSelected = false;
            }
        }
        else {
            // no pref stored for this ElementID
            valueName = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : DefaultSelection;
            isPrefSelected = false;
        }

        //D.Log(_popupList.value == valueName, "{0} selection unchanged from {1}.", ElementID.GetValueName(), valueName);
        _popupList.value = valueName;
        return isPrefSelected;
    }

    /// <summary>
    /// Called when a selection has been made. Default does nothing.
    /// Note: Called when any selection is made, even a selection that is the same as the previous selection.
    /// </summary>
    protected virtual void OnPopupListSelection() { }

    protected virtual void Validate() {
        if (IncludesRandom) {
            D.Assert(_popupList.items.Contains("Random"));
        }
        else {
            D.Assert(!_popupList.items.Contains("Random"));
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        EventDelegate.Remove(_popupList.onChange, _label.SetCurrentSelection);
        EventDelegate.Remove(_popupList.onChange, OnPopupListSelection);
    }

}

