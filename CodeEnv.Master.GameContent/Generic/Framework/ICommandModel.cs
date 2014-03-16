// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICommandModel.cs
// Interface for a UnitCommand.Model.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for a UnitCommandModel.
    /// </summary>
    public interface ICommandModel : IUnitModel {

        IEnumerable<IElementModel> ElementTargets { get; }

    }
}

