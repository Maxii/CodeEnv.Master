// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHudElement.cs
// Abstract base class for customized Elements of HUDs.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for customized Elements of HUDs.
/// </summary>
public abstract class AHudElement : AMonoBase {

    public abstract HudElementID ElementID { get; }

    private AHudElementContent _hudContent;
    public AHudElementContent HudContent {
        get { return _hudContent; }
        set { SetProperty<AHudElementContent>(ref _hudContent, value, "HudContent", OnHudContentChanged); }
    }

    private void OnHudContentChanged() {
        D.Assert(HudContent.ElementID == ElementID);
        AssignValuesToMembers();
    }

    protected abstract void AssignValuesToMembers();

}

