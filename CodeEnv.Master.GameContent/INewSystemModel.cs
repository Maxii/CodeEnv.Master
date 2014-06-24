// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INewSystemModel.cs
// Interface for easy access to a SystemModel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to a SystemModel.
    /// </summary>
    public interface INewSystemModel : IModel {

        new NewSystemData Data { get; set; }
    }
}

