// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGeneralFactory.cs
// Interface for easy access to GeneralFactory.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for easy access to GeneralFactory.
    /// </summary>
    public interface IGeneralFactory {

        GameObject MakeOrbiterInstance(GameObject parent, bool isMobile, bool isForShips, string name = "");

    }
}

