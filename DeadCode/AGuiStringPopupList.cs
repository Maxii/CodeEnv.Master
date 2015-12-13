// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiStringPopupList.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract generic base class for string PopupLists.
/// Automatically acquires the value held in PlayerPrefsManager to initialize the popup list's selection. 
/// </summary>
public abstract class AGuiStringPopupList : AGuiPopupList {

    /// <summary>
    /// Flag indicating whether <c>Random</c> is included in the selection choices.
    /// </summary>
    protected virtual bool IncludesRandom { get { return false; } }

    // must be called in Awake() as UIPopupList makes a selectionName change to the item[0] in Start()
    protected override void InitializeListValues() {
        _popupList.items.Clear();
        GetValues().ForAll(v => _popupList.items.Add(v));
        Validate();
    }

    protected abstract string[] GetValues();

    protected override void InitializeSelection() {
        if (HasPreference) {
            string prefsPropertyName = ElementID.PreferencePropertyName();
            PropertyInfo propertyInfo = typeof(PlayerPrefsManager).GetProperty(prefsPropertyName);
            if (propertyInfo == null) {
                D.ErrorContext("No {0} property named {1} found!".Inject(typeof(PlayerPrefsManager).Name, prefsPropertyName), gameObject);
            }
            Func<string> propertyGet = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            _popupList.value = propertyGet();
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

}

