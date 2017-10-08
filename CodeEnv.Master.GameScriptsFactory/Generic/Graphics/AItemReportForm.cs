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
            SetProperty<AItemReport>(ref _report, value, "Report");
        }
    }

    public sealed override void PopulateValues() {
        D.AssertNotNull(Report);
        AssignValuesToMembers();
    }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = Report.Name != null ? Report.Name : Unknown;
    }

    protected override void AssignValueToOwnerGuiElement() {
        base.AssignValueToOwnerGuiElement();
        _ownerGuiElement.Owner = Report.Owner;
    }

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        _report = null;
    }

}

