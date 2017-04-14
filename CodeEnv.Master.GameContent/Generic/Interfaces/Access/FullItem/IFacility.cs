// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFacility.cs
// Interface for easy access to MonoBehaviours that are FacilityItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are FacilityItems.
    /// </summary>
    public interface IFacility : IUnitElement {

        FacilityOrder CurrentOrder { get; }
    }
}

