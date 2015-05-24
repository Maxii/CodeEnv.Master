// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ItemHudEditor.cs
// Custom editor for ItemHuds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEditor;

/// <summary>
/// Custom editor for ItemHuds.
/// </summary>
[CustomEditor(typeof(ItemHud))]
public class ItemHudEditor : AGuiWindowEditor<ItemHud> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

