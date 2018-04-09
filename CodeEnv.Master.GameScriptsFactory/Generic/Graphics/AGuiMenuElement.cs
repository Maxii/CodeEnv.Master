// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Gui elements that are part of a menu with an Accept button.
/// <remarks>Menu Accept Buttons gather all state changes made in the menu (checkboxes, sliders, popupLists, etc)
/// and then update the appropriate place where those state changes are recorded.</remarks>
/// </summary>
public abstract class AGuiMenuElement : ATextTooltip {

    /// <summary>
    /// Unique ID for this Gui Element.
    /// </summary>
    public abstract GuiElementID ElementID { get; }

    protected override void Awake() {
        base.Awake();
        __ValidateOnAwake();
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateOnAwake() {
        UnityUtility.ValidateComponentPresence<UIWidget>(gameObject);
        D.Assert(ElementID != default(GuiElementID), gameObject, "ElementID not set.");
    }

}

