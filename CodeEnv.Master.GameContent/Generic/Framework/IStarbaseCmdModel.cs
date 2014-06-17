// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStarbaseCmdModel.cs
//  Interface for StarbaseCmdModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for StarbaseCmdModels.
    /// </summary>
    public interface IStarbaseCmdModel : ICmdModel {

        new StarbaseCmdData Data { get; set; }

    }
}

