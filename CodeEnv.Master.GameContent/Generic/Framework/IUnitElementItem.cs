// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitElementItem.cs
// Interface for easy access to items that are unit elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to items that are unit elements.
    /// </summary>
    public interface IUnitElementItem : IMortalItem {

        event Action<IUnitElementItem> onIsHQChanged;

        bool IsHQ { get; }

        IUnitCmdItem Command { get; }

    }
}

