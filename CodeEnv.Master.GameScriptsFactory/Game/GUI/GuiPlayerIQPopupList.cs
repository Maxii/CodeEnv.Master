// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerIQPopupList.cs
// Player IQ selection popup list in the NewGameMenu. 
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
/// Player IQ selection popup list in the NewGameMenu. 
/// </summary>
public class GuiPlayerIQPopupList : AGuiMenuPopupList<IQ> {

    //[FormerlySerializedAs("elementID")]
    [Tooltip("The unique ID of this PlayerIQPopupList GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    protected override string[] Choices { get { return Enums<IQ>.GetNames(excludeDefault: true); } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

