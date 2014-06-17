// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISettlementCmdModel.cs
//  Interface for SettlementCmdModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for SettlementCmdModels.
    /// </summary>
    public interface ISettlementCmdModel : ICmdModel {

        new SettlementCmdData Data { get; set; }

    }
}

