// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiEnumPopupListBase.cs
//  Generic GuiEnumPopupListBase class that implements PlayerPrefsManager property initialization and Tooltip functionality.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


// default namespace

using System;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Generic GuiEnumPopupListBase class that implements PlayerPrefsManager property initialization and Tooltip
/// functionality. Also pre-registers with the NGUI PopupList delegate to receive OnPopupMenuSelectionChange events.
/// </summary>
/// <typeparam name="T">The enum Type used in the list.</typeparam>
public abstract class GuiEnumPopupListBase<T> : GuiPopupListBase where T : struct {

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
            Debug.Log("No PlayerPrefsManager property found for {0}, so initializing selectionName to first item in list: {1}.".Inject(typeof(T), popupList.selection));
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
                Debug.LogError("No PlayerPrefsManager property named {0} found!".Inject(propertyName));
                return;
            }
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            popupList.selection = propertyGet().ToString();
        }
        else {
            Debug.LogWarning("The PlayerPrefsManager Property has not been named for {0}.".Inject(gameObject.name));
        }
    }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

