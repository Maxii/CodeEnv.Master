// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlanetoidModel.cs
//  Interface for PlanetoidModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for PlanetoidModels.
    /// </summary>
    public interface IPlanetoidModel : IMortalModel {

        new PlanetoidItemData Data { get; set; }

    }
}

