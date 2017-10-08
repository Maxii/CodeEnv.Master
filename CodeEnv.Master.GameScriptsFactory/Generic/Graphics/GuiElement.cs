// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiElement.cs
// Instantiable AGuiElement that is uniquely identifiable by its GuiElementID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Instantiable AGuiElement that is uniquely identifiable by its GuiElementID.
/// <remarks>Primarily used to identify and find a specific GameObject in a complex Gui implementation.</remarks>
/// </summary>
public sealed class GuiElement : AGuiElement {

    [Tooltip("The unique ID of this GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    public override bool IsInitialized { get { return true; } }

    protected override void InitializeValuesAndReferences() { }

    public override void ResetForReuse() { }

    protected override void Cleanup() { }


}

