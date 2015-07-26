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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages the instantiation and setup of a prefab Gui windowed menu system. This is primarily used when
/// the same windowed menu system is used by more than one scene's Gui system, allowing the dev
/// to maintain only the one prefab.
/// Usage: Place this script on the gameObject that you wish to be the parent of the newly instantiated prefab
/// clone.
/// </summary>
[System.Obsolete]   // no longer used as can't see windows in Editor, although still functional
public class GuiPrefabLinker : AMonoBase {

    /// <summary>
    /// A Window or WindowSystem prefab gameObject that has one or more embedded window
    /// menus.
    /// </summary>
    public GameObject linkedPrefab;

    /// <summary>
    /// The button (w/UIPlayAnimation script) that launches the topLevel WindowMenu embedded
    /// in the linkedPrefab.
    /// </summary>
    public UIPlayAnimation topLevelLaunchButton;

    /// <summary>
    /// Any UIPanels that should not be hidden when a subMenu of the linkedPrefab is shown. 
    /// Example: The DebugControls UIPanel should normally stay visible when the subMenus of the
    /// OptionsMenu are shown.
    /// </summary>
    public List<UIPanel> optionalHidePanelExceptions;

    protected override void Start() {
        base.Start();
        SetupLinkedPrefab();    // called from Start to allow disabling in scene without error, i.e. Awake() is called even when disabled
    }

    /// <summary>
    /// Instantiates a linked prefab, initializes and wires it for required animation behaviour.
    /// </summary>
    private void SetupLinkedPrefab() {
        if (linkedPrefab == null || topLevelLaunchButton == null) {
            D.Error("One or more GuiPrefabLinker fields are not set on {0}. \nThis is typically the lack of a Launch button instance.", gameObject.name);
            return;
        }
        GameObject prefabClone = NGUITools.AddChild(gameObject, linkedPrefab);
        prefabClone.name = linkedPrefab.name;

        // some linkedPrefabs are headed by an empty gameObject containing multiple UIPanel fwdBackWindow children (eg. main menu and submenus)
        var uiPanels = prefabClone.GetSafeMonoBehavioursInChildren<UIPanel>();
        var uiPanelCount = uiPanels.Length;
        UIPanel topLevelUIPanel = prefabClone.GetSafeFirstMonoBehaviourInChildren<UIPanel>();
        if (uiPanelCount > 1) {
            // D.Log("{0} contains {1} UIPanels: {2}. \nUsing {3}.", prefabClone.name, uiPanelCount, uiPanels.Concatenate(), topLevelUIPanel.name);
            // GetComponentInChildren() should return the first UIPanel which should be the same as GetComponentsInChildren()[0] when there are other submenus
            D.Assert(uiPanels[0] == topLevelUIPanel, "{0} and {1} are not the same.".Inject(uiPanels[0].name, topLevelUIPanel.name));
        }

        Animation topLevelWindowAnimation = topLevelUIPanel.gameObject.GetComponent<Animation>();

        if (prefabClone != topLevelUIPanel.gameObject) {
            // prefabClone is an empty GameObject holding multiple UIPanels so always make sure it is activated
            NGUITools.SetActive(prefabClone, true);
        }
        // start all offscreen fwdBackWindows deactivated. MyNguiButtonPlayAnimation will activate onPlay
        uiPanels.ForAll(p => NGUITools.SetActive(p.gameObject, false));

        // get all instances of NguiButtonPlayAnimation on this button in case the button has more than 1 NguiButtonPlayAnimation
        UIPlayAnimation[] animationsOnTopLevelLaunchButton = topLevelLaunchButton.gameObject.GetSafeMonoBehaviours<UIPlayAnimation>();
        UIPlayAnimation animationOnTopLevelLaunchButtonWithNoAssignedTarget = animationsOnTopLevelLaunchButton.Single<UIPlayAnimation>(c => c.target == null);
        animationOnTopLevelLaunchButtonWithNoAssignedTarget.target = topLevelWindowAnimation;
        // if there are any submenus in this linkedPrefab, they are already wired to the buttons in the topLevelMenu that launch them as prefabs can retain internal linkages

        var launchButton = animationOnTopLevelLaunchButtonWithNoAssignedTarget.gameObject.GetSafeMonoBehaviour<GuiVisibilityModeControlButton>();
        if (!Utility.CheckForContent<UIPanel>(launchButton.exceptions)) {
            launchButton.exceptions = new List<UIPanel>(1);
        }
        else {
            //D.Warn("GuiVisibilityExceptions already contains an exception! Now being replaced by {0}.".Inject(topLevelUIPanel.name));
        }
        launchButton.exceptions.Add(topLevelUIPanel);
        // Note: any exception arrays from GuiVisibilityButtons that launch subMenus should already have the exception for the subMenu itself set in the editor

        // Check for any exceptions that need to be added to subMenu launch buttons (if any)
        if (Utility.CheckForContent<UIPanel>(optionalHidePanelExceptions)) {
            // there are exceptions so check to see if there are also subMenu launch buttons that need them added to their exception list
            var subMenuLaunchButtons = prefabClone.GetComponentsInChildren<GuiVisibilityModeControlButton>(includeInactive: true).Where(lb => lb.visibilityModeOnClick == GuiVisibilityMode.Hidden);
            if (subMenuLaunchButtons.IsNullOrEmpty()) {
                D.WarnContext("{0}.{1} has panel exceptions listed, but no subMenu launchButtons to apply them too.".Inject(gameObject.name, GetType().Name), this);
            }
            else {
                subMenuLaunchButtons.ForAll(lb => lb.exceptions.AddRange(optionalHidePanelExceptions));
            }
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

