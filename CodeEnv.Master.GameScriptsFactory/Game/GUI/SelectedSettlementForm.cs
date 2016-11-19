// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedSettlementForm.cs
// Form used by the SelectedItemHudWindow to display info from a SettlementCmdReport when a settlement is selected. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info from a SettlementCmdReport when a settlement is selected. 
/// </summary>
public class SelectedSettlementForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedSettlement; } }

    protected override void AssignValueToStrategicResourcesGuiElement() {
        base.AssignValueToStrategicResourcesGuiElement();
        _resourcesElement.Reset();  // IMPORTANT Always Reset GuiElements used by AItemSelectedForms as the same instance is being reused
        var report = Report as SettlementCmdReport;
        _resourcesElement.Resources = report.Resources;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

