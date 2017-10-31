// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInteractibleHudWindow.cs
// Interface for easy access to the InteractibleHudWindow. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Interface for easy access to the InteractibleHudWindow. 
/// </summary>
public interface IInteractibleHudWindow : IHudWindow {

    void Show(FormID formID, AItemData data);

}

