﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPrefabLinker.cs
// Manages the instantiation and setup of a prefab menu system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the instantiation and setup of a prefab Gui menu system.
/// </summary>
public class GuiPrefabLinker : AMonoBehaviourBase {

    public GameObject linkedPrefab;
    public UIButtonPlayAnimation launchButtonAnimation;

    void Awake() {
        SetupLinkedPrefab();
    }

    /// <summary>
    /// Instantiates a linked prefab, initializes and wires it for required animation behaviour.
    /// </summary>
    private void SetupLinkedPrefab() {
        if (linkedPrefab == null || launchButtonAnimation == null) {
            Debug.LogError("One or more GuiPrefabLinker fields are not set. This is typically the lack of a Launch button instance.");
            return;
        }
        GameObject prefabClone = NGUITools.AddChild(gameObject, linkedPrefab);
        // NGUITools.AddChild handles all of the following
        //GameObject prefabClone = Instantiate<GameObject>(linkedPrefab);
        //prefabClone.transform.parent = transform;
        //prefabClone.transform.localScale = Vector3.one;
        //prefabClone.transform.localPosition = Vector3.zero;
        //prefabClone.transform.localRotation = Quaternion.identity;
        //prefabClone.layer = gameObject.layer;

        UIPanel prefabUIPanel = prefabClone.GetComponentInChildren<UIPanel>();
        Animation prefabWindowBackAnimation = prefabClone.GetComponentInChildren<Animation>();
        NGUITools.SetActive(prefabClone, true);

        UIButtonPlayAnimation[] allLaunchButtonAnimations = launchButtonAnimation.gameObject.GetSafeMonoBehaviourComponents<UIButtonPlayAnimation>();
        UIButtonPlayAnimation launchButtonAnimationWithNullTarget = allLaunchButtonAnimations.Single<UIButtonPlayAnimation>(c => c.target == null);
        launchButtonAnimationWithNullTarget.target = prefabWindowBackAnimation;

        GuiVisibilityButton launchButton = launchButtonAnimationWithNullTarget.gameObject.GetSafeMonoBehaviourComponent<GuiVisibilityButton>();
        if (launchButton.guiVisibilityExceptions.Length == 0) {
            launchButton.guiVisibilityExceptions = new UIPanel[1];
        }
        else {
            Debug.LogWarning("GuiVisibilityExceptions already contains an exception! Now being replaced by {0}.".Inject(prefabUIPanel.name));
        }
        launchButton.guiVisibilityExceptions[0] = prefabUIPanel;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
