// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedStarbaseForm.cs
// Form used by the SelectedItemHudWindow to display info from a StarbaseCmdReport when a starbase is selected.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info from a StarbaseCmdReport when a starbase is selected.  
/// </summary>
public class SelectedStarbaseForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedStarbase; } }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

