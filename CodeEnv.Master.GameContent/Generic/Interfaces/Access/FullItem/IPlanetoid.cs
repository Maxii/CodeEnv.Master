// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlanetoid.cs
// Interface for easy access to MonoBehaviours that are APlanetoidItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are APlanetoidItems.
    /// </summary>
    public interface IPlanetoid : IMortalItem {

        IOrbitSimulator CelestialOrbitSimulator { get; }

        PlanetoidReport GetReport(Player player);

    }
}

