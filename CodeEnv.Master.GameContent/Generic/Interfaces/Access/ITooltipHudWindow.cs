// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITooltipHudWindow.cs
//  Interface for easy access to the TooltipHudWindow.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Interface for easy access to the TooltipHudWindow.  
/// </summary>
public interface ITooltipHudWindow : IHoveredHudWindow {

    void Show(ResourceID resourceID);

}

