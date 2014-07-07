// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementModel.cs
//  Interface for a UnitElementModel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for a UnitElementModel.
    /// </summary>
    public interface IElementModel : IMortalModel {

        //new AElementData Data { get; set; }

        bool IsHQElement { get; set; }

        ICmdModel Command { get; set; }

    }
}

