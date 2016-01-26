// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedPlanetoidForm.cs
// Form used by the SelectedItemHudWindow to display info from a PlanetoidReport when a planetoid is selected.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info from a PlanetoidReport when a planetoid is selected.   
/// </summary>
public class SelectedPlanetoidForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedPlanetoid; } }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

