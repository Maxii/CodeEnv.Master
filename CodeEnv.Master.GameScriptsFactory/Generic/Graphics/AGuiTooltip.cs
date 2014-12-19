// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiTooltip.cs
// Abstract base class for all Gui scripts that want to specify a tooltip.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Abstract base class for all Gui scripts that want to specify a tooltip.
/// </summary>
public abstract class AGuiTooltip : AMonoBase {

    protected virtual string TooltipContent { get { return string.Empty; } }

    private bool _tooltipHasContent;

    protected override void Awake() {
        base.Awake();
        if (_tooltipHasContent = Utility.CheckForContent(TooltipContent)) {
            UnityUtility.ValidateComponentPresence<Collider>(gameObject);
            //UnityUtility.ValidateComponentPresence<Collider2D>(gameObject);   // OPTIMIZE use 2D Box Colliders for UI rather than 3D when Box2D Physics bug fixed
        }
    }

    void OnTooltip(bool toShow) {
        if (_tooltipHasContent) {
            if (toShow) {
                UITooltip.Show(TooltipContent);
            }
            else {
                UITooltip.Show(string.Empty);
            }
        }
    }

}

