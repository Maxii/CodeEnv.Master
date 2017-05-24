// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetTableRowForm.cs
// Form that displays info about a fleet in a table row.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form that displays info about a fleet in a table row.
/// </summary>
public class FleetTableRowForm : ACommandTableRowForm {

    public override FormID FormID { get { return FormID.FleetTableRow; } }

    private FleetCompositionGuiElement _compositionElement;

    protected override void InitializeCompositionGuiElement(AGuiElement e) {
        base.InitializeCompositionGuiElement(e);
        _compositionElement = e as FleetCompositionGuiElement;
    }

    protected override void AssignValueToCompositionGuiElement() {
        base.AssignValueToCompositionGuiElement();
        var report = Report as FleetCmdReport;
        _compositionElement.IconInfo = FleetIconInfoFactory.Instance.MakeInstance(report);
        _compositionElement.Category = report.Category;
    }

    protected override void AssignValueToSpeedGuiElement() {
        base.AssignValueToSpeedGuiElement();
        var report = Report as FleetCmdReport;
        _speedLabel.text = report.UnitFullSpeed.HasValue ? Constants.FormatFloat_1DpMax.Inject(report.UnitFullSpeed.Value) : _unknown;
    }

}

