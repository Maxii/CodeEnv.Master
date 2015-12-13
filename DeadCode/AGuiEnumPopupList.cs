// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiEnumPopupList.cs
// Abstract generic base class that uses Enums to populate popup lists in the Gui.
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
/// Abstract generic base class that uses Enums to populate popup lists in the Gui. 
/// Automatically acquires the value held in PlayerPrefsManager to initialize the popup list's selection. 
/// </summary>
/// <typeparam name="E">The enum Type used in the list.</typeparam>
public abstract class AGuiEnumPopupList<E> : AGuiPopupList where E : struct {

    /// <summary>
    /// Flag indicating whether <c>Random</c> is included in the selection choices.
    /// </summary>
    protected virtual bool IncludesRandom { get { return false; } }

    // must be called in Awake() as UIPopupList makes a selectionName change to the item[0] in Start()
    protected override void InitializeListValues() {
        _popupList.items.Clear();
        var enumValues = Enums<E>.GetValues().Except<E>(default(E));
        enumValues.ForAll(e => _popupList.items.Add(Enums<E>.GetName(e)));
        Validate();
    }

    protected override void InitializeSelection() {
        if (HasPreference) {
            string prefsPropertyName = ElementID.PreferencePropertyName();
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
            }
            Func<E> propertyGet = (Func<E>)Delegate.CreateDelegate(typeof(Func<E>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            _popupList.value = propertyGet().ToString();
        }
        else {
            _popupList.value = IncludesRandom ? _popupList.items.Single(item => item.Equals("Random")) : _popupList.items[Constants.Zero];
        }
        D.Log("GuiElement [{0}] selection initialized to {1}.", ElementID.GetValueName(), _popupList.value);
    }

    private void Validate() {
        if (IncludesRandom) {
            D.Assert(_popupList.items.Contains("Random"));
        }
        else {
            D.Assert(!_popupList.items.Contains("Random"));
        }
    }

    #region Archive

    /// <summary>
    /// Initializes the PopupList selectionName with the value held in PlayerPrefsManager or, if no PlayerPrefs property is found,
    /// warns and selects the default. Uses Reflection to find the PlayerPrefsManager property of Type E, 
    /// then creates a Property Delegate to acquire the initialization value.
    /// </summary>
    //private void InitializeSelectionFromPlayerPrefs() {
    //    PropertyInfo[] propertyInfos = typeof(PlayerPrefsManager).GetProperties();
    //    PropertyInfo propertyInfo = propertyInfos.SingleOrDefault<PropertyInfo>(p => p.PropertyType == typeof(E));
    //    if (propertyInfo != null) {
    //        Func<E> propertyGet = (Func<E>)Delegate.CreateDelegate(typeof(Func<E>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
    //        _popupList.value = propertyGet().ToString();
    //    }
    //    else {
    //        InitializeDefaultSelection();
    //        D.Warn("No {0} property found for {1}. Initializing default selection {2}.", typeof(PlayerPrefsManager).Name, typeof(E).Name, _popupList.value);
    //    }
    //}

    //[Obsolete]
    //public string propertyName = string.Empty;

    ///// <summary>
    ///// Initializes the PopupList selectionName with the tPrefsValue held in PlayerPrefsManager. Uses Reflection to find the PlayerPrefsManager
    ///// property named, then creates a Property Delegate to acquire the initialization tPrefsValue.
    ///// </summary>
    ///// <remarks>Must be called during Start() as propertyName must be set in Awake().</remarks>
    //[Obsolete]
    //private void SetSelectionFromPlayerPrefsUsingPropertyName() {
    //    if (!string.IsNullOrEmpty(propertyName)) {
    //        PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(propertyName);
    //        if (propertyInfo == null) {
    //            D.Error("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
    //            return;
    //        }
    //        Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
    //        _popupList.value = propertyGet().ToString();
    //        //popupList.selection = propertyGet().ToString();
    //    }
    //    else {
    //        D.Warn("The PlayerPrefsManager Property has not been named for {0}.".Inject(gameObject.name));
    //    }
    //}

    #endregion

}

