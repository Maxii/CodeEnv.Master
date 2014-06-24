// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlanetModel.cs
// Interface providing access to PlanetModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface providing access to PlanetModels.
    /// </summary>
    public interface IPlanetModel : IMortalModel {

        new PlanetData Data { get; set; }

    }
}

