// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseTableRowForm.cs
//  Form that displays info about a starbase in a table row.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form that displays info about a starbase in a table row.
/// </summary>
public class StarbaseTableRowForm : ACommandTableRowForm {

    public override FormID FormID { get { return FormID.StarbaseTableRow; } }

    private StarbaseCompositionGuiElement _compositionElement;

    protected override void InitializeCompositionGuiElement(AGuiElement e) {
        base.InitializeCompositionGuiElement(e);
        _compositionElement = e as StarbaseCompositionGuiElement;
    }

    protected override void AssignValueToCompositionGuiElement() {
        base.AssignValueToCompositionGuiElement();
        var report = Report as StarbaseCmdReport;
        _compositionElement.IconInfo = StarbaseIconInfoFactory.Instance.MakeInstance(report);
        _compositionElement.Category = report.Category;
    }

    protected override void AssignValueToStrategicResourcesGuiElement() {
        base.AssignValueToStrategicResourcesGuiElement();
        var report = Report as StarbaseCmdReport;
        _resourcesElement.Resources = report.Resources;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

