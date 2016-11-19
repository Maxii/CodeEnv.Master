// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugFleetCreator.cs
// Interface for access to DebugFleetCreators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for access to DebugFleetCreators.
    /// </summary>
    public interface IDebugFleetCreator {

        AUnitCreatorEditorSettings EditorSettings { get; }

    }
}

