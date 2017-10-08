// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseTableRowForm.cs
// Form that displays info about a starbase in a table row.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form that displays info about a starbase in a table row.
/// </summary>
public class StarbaseTableRowForm : ACommandTableRowForm {

    public override FormID FormID { get { return FormID.StarbaseTableRow; } }

    protected override void AssignValueToCompositionGuiElement() {
        base.AssignValueToCompositionGuiElement();
        var report = Report as StarbaseCmdReport;
        _compositionGuiElement.IconInfo = StarbaseIconInfoFactory.Instance.MakeInstance(report);
        (_compositionGuiElement as StarbaseCompositionGuiElement).Category = report.Category;
    }

    protected override void AssignValueToStrategicResourcesGuiElement() {
        base.AssignValueToStrategicResourcesGuiElement();
        var report = Report as StarbaseCmdReport;
        _resourcesGuiElement.Resources = report.Resources;
    }

    protected override void AssignValueToConstructionGuiElement() {
        base.AssignValueToConstructionGuiElement();
        var report = Report as StarbaseCmdReport;
        _constructionGuiElement.Construction = report.CurrentConstruction;
    }

}

