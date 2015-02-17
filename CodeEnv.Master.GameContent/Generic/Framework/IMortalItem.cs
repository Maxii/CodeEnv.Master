// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalItem.cs
//  Interface for all items that can die.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    ///  Interface for all items that can die.
    /// </summary>
    public interface IMortalItem : IItem {

        event Action<IMortalItem> onDeathOneShot;

        bool IsOperational { get; }

    }
}

