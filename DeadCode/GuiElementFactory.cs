// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiElementFactory.cs
// Singleton. COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. COMMENT 
/// </summary>
[Obsolete]
public class GuiElementFactory : AMonoSingleton<GuiElementFactory> {

    public GuiStrengthElement strengthGuiElementPrefab;

    // not persistent across scenes

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        //TODO  
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        Validate();
    }

    #endregion

    public GuiStrengthElement MakeInstance(StrengthInstruction instruction, GameObject parent, CombatStrength? primaryStrength, CombatStrength? secondaryStrength = null) {
        GameObject strengthElementGoClone = NGUITools.AddChild(parent, strengthGuiElementPrefab.gameObject);
        // runs Awake() then disabled to avoid running Start
        GuiStrengthElement strengthElement = strengthElementGoClone.GetComponent<GuiStrengthElement>();

        switch (instruction) {
            case StrengthInstruction.Offensive:
                //strengthElement.SetOffensiveStrength(primaryStrength);
                break;
            case StrengthInstruction.Defensive:
                //strengthElement.SetDefensiveStrength(primaryStrength);
                break;
            case StrengthInstruction.Both:
                //strengthElement.SetOffensiveStrength(primaryStrength);
                //strengthElement.SetDefensiveStrength(secondaryStrength);
                break;
            case StrengthInstruction.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(instruction));
        }
        strengthElement.enabled = true;
        return strengthElement;
    }

    private void Validate() {
        D.Assert(strengthGuiElementPrefab != null);
    }

    #region Cleanup

    protected override void Cleanup() {
        //TODO
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    public enum StrengthInstruction {

        None,

        Offensive,

        Defensive,

        Both
    }
    #endregion

}

