// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITerminatableOrdnance.cs
// Interface for Ordnance that can be remotely terminated.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  Interface for Ordnance that can be remotely terminated.
    /// </summary>
    public interface ITerminatableOrdnance : IOrdnance {

        void Terminate();
    }
}

