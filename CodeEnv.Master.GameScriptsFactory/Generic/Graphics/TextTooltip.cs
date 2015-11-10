// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TextTooltip.cs
// Standalone and extensible class for all Gui constructs that specify display of a simple text tooltip.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
///Standalone and extensible class for all Gui constructs that specify display of a simple text tooltip.
/// Can be instantiated for just text tooltip functionality but requires a Collider.
/// </summary>
public class TextTooltip : ATextTooltip {

    //[FormerlySerializedAs("tooltip")]
    [Tooltip("The content to use in the tooltip")]
    [SerializeField]
    private string _tooltip = string.Empty;

    protected override string TooltipContent { get { return _tooltip; } }

    protected override void Start() {
        base.Start();
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

