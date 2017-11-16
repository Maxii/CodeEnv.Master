// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUserUnitInteractibleHudForm.cs
// Abstract class for User-owned UnitForms used by the InteractibleHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class for User-owned UnitForms used by the InteractibleHud.
/// </summary>
public abstract class AUserUnitInteractibleHudForm : AUserItemInteractibleHudForm {

    public new AUnitCmdData ItemData { get { return base.ItemData as AUnitCmdData; } }

    protected sealed override void AssignValueToNameChangeGuiElement() {
        base.AssignValueToNameChangeGuiElement();
        _nameChgGuiElement.NameReference = new Reference<string>(() => ItemData.UnitName, z => ItemData.UnitName = z);
    }

    protected sealed override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = ItemData.UnitOutputs;
    }

    protected override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        _offensiveStrengthGuiElement.Strength = ItemData.UnitOffensiveStrength;
    }

    protected override void AssignValueToDefensiveStrengthGuiElement() {
        base.AssignValueToDefensiveStrengthGuiElement();
        _defensiveStrengthGuiElement.Strength = ItemData.UnitDefensiveStrength;
    }

    protected sealed override void AssignValueToHeroChangeGuiElement() {
        base.AssignValueToHeroChangeGuiElement();
        _heroChgGuiElement.HeroDataReference = new Reference<Hero>(() => ItemData.Hero, z => ItemData.Hero = z);
    }

    protected sealed override void AssignValueToFormationChangeGuiElement() {
        base.AssignValueToFormationChangeGuiElement();
        _formationChgGuiElement.FormationReference = new Reference<Formation>(() => ItemData.Formation, z => ItemData.Formation = z);
        _formationChgGuiElement.AcceptableFormations = ItemData.AcceptableFormations;
    }


}

