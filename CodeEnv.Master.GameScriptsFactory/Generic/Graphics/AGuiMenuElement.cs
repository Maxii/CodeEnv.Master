﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiMenuElement.cs
// Abstract base class for Gui elements that are part of a menu with an Accept button.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Gui elements that are part of a menu with an Accept button.
/// </summary>
public abstract class AGuiMenuElement : AGuiTooltip {

    /// <summary>
    /// Flag indicating whether PlayerPrefsManager holds a selection preference associated with this ElementID.
    /// </summary>
    public virtual bool HasPreference { get { return false; } }

    /// <summary>
    /// Unique ID for this Gui Menu Element.
    /// </summary>
    public abstract GuiMenuElementID ElementID { get; }

    protected override void Awake() {
        base.Awake();
        Validate();
    }

    private void Validate() {
        UnityUtility.ValidateMonoBehaviourPresence<UIWidget>(gameObject);
        D.Assert(ElementID != GuiMenuElementID.None, "ElementID not set.", gameObject);
        if (HasPreference) {
            Arguments.ValidateForContent(ElementID.PreferencePropertyName());
        }
    }

}
