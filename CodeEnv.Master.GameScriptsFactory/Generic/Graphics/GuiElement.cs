// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiElement.cs
// Instantiable version of AGuiElement that is uniquely identifiable by its GuiElementID. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Instantiable version of AGuiElement that is uniquely identifiable by its GuiElementID. Also has
/// embedded text tooltip support. GuiElements typically have one or more UIWidget siblings
/// and/or children associated with them that they help identify and/or find.
/// </summary>
public sealed class GuiElement : AGuiElement {

    public GuiElementID elementID;

    public override GuiElementID ElementID { get { return elementID; } }

    public override void Reset() { }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

