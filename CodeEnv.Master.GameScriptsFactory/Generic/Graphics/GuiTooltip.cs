// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiTooltip.cs
// Standalone and extensible class for all Gui scripts containing Tooltip infrastructure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Standalone and extensible class for all Gui scripts containing Tooltip infrastructure. 
/// Can be instantiated for just Tooltip functionality but requires a Collider.
/// </summary>
public class GuiTooltip : AGuiTooltip {

    public string tooltip = string.Empty;

    protected override string TooltipContent { get { return tooltip; } }

    protected override void Start() {
        base.Start();
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

