// --------------------------------------------------------------------------------------------------------------------
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

#define DEBUG_WARN
#define DEBUG_ERROR

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
    public NguiButtonPlayAnimation launchButtonAnimation;

    protected override void Awake() {
        base.Awake();
        SetupLinkedPrefab();
    }

    /// <summary>
    /// Instantiates a linked prefab, initializes and wires it for required animation behaviour.
    /// </summary>
    private void SetupLinkedPrefab() {
        if (linkedPrefab == null || launchButtonAnimation == null) {
            D.Error("One or more GuiPrefabLinker fields are not set on {0}. This is typically the lack of a Launch button instance.", gameObject.name);
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

        NguiButtonPlayAnimation[] allLaunchButtonAnimations = launchButtonAnimation.gameObject.GetSafeMonoBehaviourComponents<NguiButtonPlayAnimation>();
        NguiButtonPlayAnimation launchButtonAnimationWithNullTarget = allLaunchButtonAnimations.Single<NguiButtonPlayAnimation>(c => c.target == null);
        launchButtonAnimationWithNullTarget.target = prefabWindowBackAnimation;

        GuiVisibilityButton launchButton = launchButtonAnimationWithNullTarget.gameObject.GetSafeMonoBehaviourComponent<GuiVisibilityButton>();
        if (!Utility.CheckForContent<UIPanel>(launchButton.guiVisibilityExceptions)) {
            launchButton.guiVisibilityExceptions = new UIPanel[1];
        }
        else {
            D.Warn("GuiVisibilityExceptions already contains an exception! Now being replaced by {0}.".Inject(prefabUIPanel.name));
        }
        launchButton.guiVisibilityExceptions[0] = prefabUIPanel;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

