// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitElement.cs
// Interface for easy access to MonoBehaviours that are AUnitElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are AUnitElementItems.
    /// </summary>
    public interface IUnitElement : IMortalItem {

        event EventHandler isHQChanged;

        IUnitCmd Command { get; }

        bool IsHQ { get; }

    }
}

