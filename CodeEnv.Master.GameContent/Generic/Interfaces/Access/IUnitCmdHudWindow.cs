// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitCmdHudWindow.cs
// Interface for easy access to the UnitCmdHudWindow MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to the UnitCmdHudWindow MonoBehaviour.
    /// </summary>
    [System.Obsolete]
    public interface IUnitCmdHudWindow : IHudWindow {

        void Show(FormID formID, IFleetCmd unit);

    }
}

