// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFacilityModel.cs
//  Interface for FacilityModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for FacilityModels.
    /// </summary>
    public interface IFacilityModel : IElementModel {

        new FacilityData Data { get; set; }

        FacilityState CurrentState { get; set; }

        ICommandModel Command { get; set; }

    }
}

