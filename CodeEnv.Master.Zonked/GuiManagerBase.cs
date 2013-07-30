// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiManagerBase.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
[Obsolete]
public abstract class GuiManagerBase<T> : AMonoBehaviourBase where T : AMonoBehaviourBase {

    /// <summary>
    /// true if a temporary GameObject has been created to host this Singleton.
    /// </summary>
    protected static bool isTempGO;

    protected static T instance;
    public static T Instance {
        get {
            if (instance == null) {
                // values is required for the first time, so look for it                        
                Type thisType = typeof(T);
                instance = FindObjectOfType(thisType) as T;
                if (instance == null) {
                    // an instance of this singleton doesn't yet exist so create a temporary one
                    Debug.LogWarning("No instance of {0} found, so a temporary one has been created.".Inject(thisType.ToString()));
                    GameObject tempGO = new GameObject("Temp values of {0}.".Inject(thisType.ToString()), thisType);
                    instance = tempGO.GetComponent<T>();
                    if (instance == null) {
                        Debug.LogError("Problem during the creation of {0}.".Inject(thisType.ToString()));
                    }
                    isTempGO = true;
                }
            }
            return instance;
        }
    }

    protected virtual void InitializeGui() {
        AcquireGuiReferences();
        SetupGuiEventHandlers();
        InitializeGuiWidgets();
    }

    protected virtual void WarnOnMissingGuiElementReference(Type scriptType) {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string callerIdMessage = ". Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
        Debug.LogWarning("Missing GUI Element Reference of Type: " + scriptType.Name + callerIdMessage);
    }

    protected virtual void WarnOnIncorrectName(string name) {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string callerIdMessage = ". Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);
        Debug.LogWarning("Name used on GuiElement not found: " + name + callerIdMessage);

    }

    protected abstract void AcquireGuiReferences();

    protected abstract void SetupGuiEventHandlers();

    protected abstract void InitializeGuiWidgets();

    /// <summary>
    /// TODO - Override this in derived class and set instance = null;
    /// </summary>
    protected virtual void OnApplicationQuit() {
        Debug.LogWarning("You should override this OnApplicationQuit() and set instance to null in derived class.");
        instance = null;
    }

}

