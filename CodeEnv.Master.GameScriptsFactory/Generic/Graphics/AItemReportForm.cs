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

using System;
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
            _report = value;
            ReportPropSetHandler();
        }
    }

    public override void PopulateValues() {
        D.AssertNotNull(Report);
        AssignValuesToMembers();
    }

    #region Event and Property Change Handlers

    private void ReportPropSetHandler() {
        HandleReportPropSet();
    }

    #endregion

    /// <summary>
    /// Hook for derived classes after Report is set but before values are assigned to members.
    /// <remarks>Default does nothing.</remarks>
    /// </summary>
    protected virtual void HandleReportPropSet() { }

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        _report = null;
    }

}

