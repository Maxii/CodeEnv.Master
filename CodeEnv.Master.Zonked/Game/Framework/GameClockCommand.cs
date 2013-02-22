// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameClockCommand.cs
// Commands for the clock including Pause, Resume, Start and Stop.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Commands for the clock including Pause, Resume, Start and Stop.
    /// </summary>
    public enum GameClockCommand {

        None,
        UserPause,
        UserResume,
        Pause,
        Resume,
        Stop,
        Start
    }
}

