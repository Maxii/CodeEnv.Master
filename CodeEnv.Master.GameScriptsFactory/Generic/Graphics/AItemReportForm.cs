// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemReportForm.cs
// Abstract base class for Forms that are fed content from an Item Report.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Forms that are fed content from an Item Report.
/// </summary>
public abstract class AItemReportForm : AInfoDisplayForm {

    protected const string Unknown = Constants.QuestionMark;

    private AItemReport _report;
    public AItemReport Report {
        get { return _report; }
        set {
            D.AssertNull(_report);  // occurs only once between Resets
            SetProperty<AItemReport>(ref _report, value, "Report", ReportPropSetHandler);
        }
    }

    #region Event and Property Change Handlers

    private void ReportPropSetHandler() {
        AssignValuesToMembers();
    }

    #endregion

    public override void Reset() {
        _report = null;
    }

}

