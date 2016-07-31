// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdPublisher.cs
// Abstract generic class for Publishers that support CmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic class for Publishers that support CmdItems.
    /// </summary>
    public abstract class ACmdPublisher<ReportType, DataType> : AIntelItemPublisher<ReportType, DataType>
        where ReportType : AUnitCmdReport
        where DataType : AUnitCmdData {

        public ACmdPublisher(DataType data)
            : base(data) {
        }

        [System.Obsolete]
        protected bool IsEqual(IEnumerable<AUnitElementReport> reportsA, IEnumerable<AUnitElementReport> reportsB) {
            var isEqual = reportsA.OrderBy(r => r.Name).SequenceEqual(reportsB.OrderBy(r => r.Name));
            string equalsPhrase = isEqual ? "equal" : "not equal";
            D.Log("{0} ElementReports are {1}.", GetType().Name, equalsPhrase);
            return isEqual;
        }

    }
}

