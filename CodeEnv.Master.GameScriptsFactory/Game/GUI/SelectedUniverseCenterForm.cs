// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedUniverseCenterForm.cs
// Form used by the SelectedItemHudWindow to display info from a UniverseCenterReport when the UniverseCenter is selected.    
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the SelectedItemHudWindow to display info from a UniverseCenterReport when the UniverseCenter is selected.    
/// </summary>
public class SelectedUniverseCenterForm : ASelectedItemForm {

    public override FormID FormID { get { return FormID.SelectedUniverseCenter; } }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

