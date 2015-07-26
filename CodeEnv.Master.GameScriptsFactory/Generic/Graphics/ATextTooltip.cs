// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATextTooltip.cs
// Abstract base class for all Gui constructs that want to be able to display a simple text tooltip.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

/// <summary>
/// Abstract base class for all Gui constructs that want to be able to display a simple text tooltip.
/// </summary>
public abstract class ATextTooltip : AMonoBase {

    protected virtual string TooltipContent { get { return null; } }

    void OnTooltip(bool toShow) {
        if (toShow) {
            TooltipHudWindow.Instance.Show(TooltipContent);   // Tooltip tests for null or empty
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

}

