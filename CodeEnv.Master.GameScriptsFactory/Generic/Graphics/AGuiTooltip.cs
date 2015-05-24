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

/// <summary>
/// Abstract base class for all Gui scripts that want to specify a tooltip.
/// </summary>
public abstract class AGuiTooltip : AMonoBase {

    protected virtual string TooltipContent { get { return null; } }

    void OnTooltip(bool toShow) {
        if (toShow) {
            Tooltip.Instance.Show(TooltipContent);   // Tooltip tests for null or empty
        }
        else {
            Tooltip.Instance.Hide();
        }
    }

}

