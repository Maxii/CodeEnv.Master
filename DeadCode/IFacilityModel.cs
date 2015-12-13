// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFacilityModel.cs
//  Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
    /// </summary>
    public interface IFacilityModel : IElementModel {

        //new FacilityData Data { get; set; }

        //FacilityState CurrentState { get; }


    }
}

