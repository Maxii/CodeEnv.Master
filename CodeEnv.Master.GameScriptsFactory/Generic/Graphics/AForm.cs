﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AForm.cs
//  Abstract base class for Forms. A Form supervises a collection of UIWidgets
//in an arrangement that can be displayed by a HudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms. A Form supervises a collection of UIWidgets
///in an arrangement that can be displayed by a GuiWindow. AForms are
///populated with content to display by feeding them Text, Reports or individual
///values (e.g. a ResourceForm is fed a ResourceID, displaying values derived from
///the ResourceID in a TooltipHudWindow).
/// </summary>
public abstract class AForm : AMonoBase {

    public abstract FormID FormID { get; }

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    protected abstract void InitializeValuesAndReferences();

    protected abstract void AssignValuesToMembers();

    /// <summary>
    /// Resets this Form by nulling the existing content (Text, Reports, etc.).
    /// If this is not done, then incoming content that is the same as 
    /// existing content will not trigger OnChange initialization.
    /// </summary>
    public abstract void Reset();

    /// <summary>
    /// Returns the single UILabel sibling or child of the provided GuiElement.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns></returns>
    protected UILabel GetLabel(AGuiElement element) {
        return element.gameObject.GetSingleComponentInChildren<UILabel>();
    }

}

