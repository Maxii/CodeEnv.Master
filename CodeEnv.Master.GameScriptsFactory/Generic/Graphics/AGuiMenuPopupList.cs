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
        Subscribe();
        if (SelfInitializeSelection) {
            InitializeSelection();
        }
    }

    protected virtual void InitializeValuesAndReferences() {
        _popupList = InitializePopupList();
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    /// <summary>
    /// Initializes the popup list prior to any content being added by this client. This includes validating that the popupList
    /// has no initial content in the inspector text box. With no text box content, UIPopupList.Start() won't raise an onChange 
    /// event before this client can populate the list with its own content.
    /// </summary>
    private UIPopupList InitializePopupList() {
        UIPopupList popupList = UnityUtility.ValidateComponentPresence<UIPopupList>(gameObject);
        // popupList.Clear();  // This just clears an initially empty items list and does nothing about what is showing in the editor.
        // Unfortunately, the UIPopupListInspector reads what is in the editor's text box and adds it to the items list every time OnInspectorGui()
        // is called. Accordingly, I have to manually clear the editor text box of content if I don't want the onChange event raised on Start().
        D.Assert(!Utility.CheckForContent<string>(popupList.items), "{0}: UIPopupList Inspector content must be empty.".Inject(GetType().Name), this);
        return popupList;
    }

    private void Subscribe() {
        EventDelegate.Add(_popupList.onChange, PopupListSelectionChangedEventHandler);
    }

    private void InitializeSelection() {
        AssignSelectionChoices();
        TryMakePreferenceSelection();
    }

    /// <summary>
    /// Assign the choices available for selection in the popupList from Choices.
    /// </summary>
    protected void AssignSelectionChoices() {
        _popupList.Clear();
        Choices.ForAll(choiceName => _popupList.AddItem(choiceName));
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
                D.ErrorContext(this, "No {0} property named {1} found!", typeof(PlayerPrefsManager).Name, prefsPropertyName);
                isPrefSelected = false;
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            valueName = propertyGet().ToString();    // gets the value of the PlayerPrefsManager Property named prefsPropertyName
            //D.Log("{0} is using preference value {1} as its selection.", ElementID.GetValueName(), valueName);
            if (!_popupList.items.Contains(valueName)) {
                // the prefs value is not one of the available choices
                //D.LogContext(this, "{0} Prefs value {1} is not among choices available to select. Using default {2}.", ElementID.GetValueName(), valueName, DefaultSelection);
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

    #region Event and Property Change Handlers


    /// <summary>
    /// Called when a selection has been made.
    /// Note: Called when any selection is made, even a selection that is the same as the previous selection.
    /// </summary>
    protected virtual void PopupListSelectionChangedEventHandler() {
        _label.SetCurrentSelection();
    }

    #endregion

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
        EventDelegate.Remove(_popupList.onChange, PopupListSelectionChangedEventHandler);
    }

}

