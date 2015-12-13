// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUniverseCenterModel.cs
// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
    /// </summary>
    public interface IUniverseCenterModel : IModel {

        //new UniverseCenterData Data { get; set; }

    }
}

