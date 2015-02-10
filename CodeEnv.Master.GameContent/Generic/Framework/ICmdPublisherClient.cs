// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICmdPublisherClient.cs
// Interface that CmdPublishers use to communicate with their UnitCmdItem clients.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface that CmdPublishers use to communicate with their UnitCmdItem clients.
    /// </summary>
    public interface ICmdPublisherClient<ElementReportType> where ElementReportType : AElementItemReport {

        ElementReportType[] GetElementReports(Player player);

    }
}

