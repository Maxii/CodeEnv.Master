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

    protected UIPopupList _popupList;

    protected override void Awake() {
        base.Awake();
        ConfigurePopupList();
        InitializeListValues();
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
    private void InitializeListValues() {
        _popupList.items.Clear();
        GetNames().ForAll(v => _popupList.items.Add(v));
        Validate();
    }

    protected abstract string[] GetNames();

    /// <summary>
    /// Select the PopupList item that is the starting selection.
    /// </summary>
    /// <remarks>Called in the Awake sequence as UIPopupList will make
    /// a selectionName change to item[0] in Start() if not already set.
    /// </remarks>
    private void InitializeSelection() {
        if (HasPreference) {
            string prefsPropertyName = ElementID.PreferencePropertyName();
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            _popupList.value = propertyGet().ToString();
        }
        else {
            _popupList.value = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : _popupList.items[Constants.Zero];
        }
        //D.Log("GuiElement [{0}] selection initialized to {1}.", ElementID.GetName(), _popupList.value);
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

}

