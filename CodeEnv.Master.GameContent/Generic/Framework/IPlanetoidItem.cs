// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlanetoidItem.cs
// Interface for easy access to APlanetoidItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to APlanetoidItems.
    /// </summary>
    public interface IPlanetoidItem : IMortalItem {

        ISystemItem ParentSystem { get; }

    }
}

