// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiEnumPopupListBase.cs
// Abstract generic class that uses Enums to populate popup lists in the Gui.
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
/// Abstract generic class that uses Enums to populate popup lists in the Gui. Automatically acquires the value held in PlayerPrefsManager 
/// to initialize the popup list's selection. Also pre-registers with the NGUI PopupList delegate to receive OnPopupMenuSelectionChange events.
/// </summary>
/// <typeparam name="T">The enum Type used in the list.</typeparam>
public abstract class AGuiEnumPopupListBase<T> : AGuiPopupListBase where T : struct {

    [Obsolete]
    public string propertyName = string.Empty;

    // must be called in Awake() as UIPopupList makes a selectionName change to the item[0] in Start()
    protected override void InitializeListValues() {
        popupList.items.Clear();
        var tValues = Enums<T>.GetValues().Except<T>(default(T));
        foreach (var tValue in tValues) {
            popupList.items.Add(Enums<T>.GetName(tValue));
        }
        //popupList.items = Enums<T>.GetNames().AddDelimiter(delimiter: Constants.NewLine).ToList<string>();
    }

    /// <summary>
    /// Initializes the PopupList selectionName with the tPrefsValue held in PlayerPrefsManager or, if no PlayerPrefs property is found,
    /// defaults to the first tPrefsValue held in the items list. Uses Reflection to find the PlayerPrefsManager property of Type T, 
    /// then creates a Property Delegate to acquire the initialization tPrefsValue.
    /// </summary>
    protected override void InitializeSelection() {
        PropertyInfo[] propertyInfos = typeof(PlayerPrefsManager).GetProperties();
        PropertyInfo propertyInfo = propertyInfos.SingleOrDefault<PropertyInfo>(p => p.PropertyType == typeof(T));
        if (propertyInfo != null) {
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            popupList.selection = propertyGet().ToString();
        }
        else {
            popupList.selection = popupList.items[0];
            D.Log("No PlayerPrefsManager property found for {0}, so initializing selectionName to first item in list: {1}.".Inject(typeof(T), popupList.selection));
        }
    }

    /// <summary>
    /// Initializes the PopupList selectionName with the tPrefsValue held in PlayerPrefsManager. Uses Reflection to find the PlayerPrefsManager
    /// property named, then creates a Property Delegate to acquire the initialization tPrefsValue.
    /// </summary>
    /// <remarks>Must be called during Start() as propertyName must be set in Awake().</remarks>
    [Obsolete]
    private void SetSelectionFromPlayerPrefsUsingPropertyName() {
        if (!string.IsNullOrEmpty(propertyName)) {
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(propertyName);
            if (propertyInfo == null) {
                D.Error("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
                return;
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            popupList.selection = propertyGet().ToString();
        }
        else {
            D.Warn("The PlayerPrefsManager Property has not been named for {0}.".Inject(gameObject.name));
        }
    }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

