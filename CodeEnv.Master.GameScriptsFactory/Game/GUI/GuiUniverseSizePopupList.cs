// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiUniverseSizePopupList.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiUniverseSizePopupList : AGuiEnumPopupListBase<UniverseSize> {

    protected override void OnPopupListSelectionChange(string item) { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

