// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISelectedItemHudWindow.cs
// Interface for easy access to the SelectedItemHudWindow. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Interface for easy access to the SelectedItemHudWindow. 
/// </summary>
public interface ISelectedItemHudWindow : IHudWindow {

    void Show(FormID formID, AItemReport report);

}

