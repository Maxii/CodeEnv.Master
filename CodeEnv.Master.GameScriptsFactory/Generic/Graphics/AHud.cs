// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHud.cs
// Singleton. Abstract Gui Window with fading ability for showing Heads Up Displays.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. Abstract Gui Window with fading ability for showing Heads Up Displays.
/// Each derived type instance can handle multiple content sources (aka customized HUDs).
/// Current derived classes include Tooltip, the left side Item Hud and the bottom Selection Hud.
/// </summary>
public abstract class AHud<T> : AGuiWindow, IHud where T : AHud<T> {

    #region MonoBehaviour Singleton Pattern

    protected static T _instance;
    public static T Instance {
        get {
            if (_instance == null) {
                if (IsApplicationQuiting) {
                    //D.Warn("Application is quiting while trying to access {0}.Instance.".Inject(typeof(T).Name));
                    return null;
                }
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(T);
                _instance = GameObject.FindObjectOfType(thisType) as T;
                // value is required for the first time, so look for it                        
                if (_instance == null && !Application.isLoadingLevel) {
                    var stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetMethod().DeclaringType, stackFrame.GetMethod().Name);
                    D.Error("No instance of {0} found. Is it destroyed/deactivated? Called by {1}.".Inject(thisType.Name, callerIdMessage));
                }
                _instance.InitializeOnInstance();
            }
            return _instance;
        }
    }

    protected sealed override void Awake() {
        base.Awake();
        // If no other MonoBehaviour has requested Instance in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object
        if (_instance == null) {
            _instance = this as T;
            InitializeOnInstance();
        }
        InitializeOnAwake();
    }

    #endregion

    protected UIWidget _backgroundWidget;

    private IDictionary<HudElementID, AHudElement> _tooltipElementLookup;
    private MyEnvelopContent _backgroundEnvelopContent;

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// Note: This method is not called by instance copies, only by the original instance. If not persistent across scenes,
    /// then this method will be called each time the new instance in a scene is instantiated.
    /// </summary>
    protected virtual void InitializeOnInstance() { }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeElementLookup();
        ActivateElement(null);
    }

    protected override void AcquireReferences() {
        base.AcquireReferences();
        _backgroundEnvelopContent = gameObject.GetSafeMonoBehaviourInImmediateChildren<MyEnvelopContent>();
        // TODO set envelopContent padding programmatically once background is permanently picked?
        _backgroundWidget = _backgroundEnvelopContent.gameObject.GetSafeMonoBehaviour<UIWidget>();
    }

    private void InitializeElementLookup() {
        var tooltipElements = gameObject.GetSafeMonoBehavioursInImmediateChildren<AHudElement>();
        _tooltipElementLookup = tooltipElements.ToDictionary(e => e.ElementID);
    }

    /// <summary>
    /// Activates the specified hudElement's gameObject, deactivating all others.
    /// If element is null, all element gameObjects are deactivated.
    /// </summary>
    /// <param name="element">The hud element.</param>
    private void ActivateElement(AHudElement element) {
        _tooltipElementLookup.Values.ForAll(e => {
            if (e == element) {
                NGUITools.SetActive(e.gameObject, true);
            }
            else {
                NGUITools.SetActive(e.gameObject, false);
            }
        });
    }

    private void EncompassElementWithBackground(AHudElement element) {
        _backgroundEnvelopContent.targetRoot = element.transform;
        _backgroundEnvelopContent.Execute();
    }

    private void SetContent(AHudElementContent content) {
        var tooltipElement = _tooltipElementLookup[content.ElementID];
        ActivateElement(tooltipElement);
        currentContentHolder = tooltipElement.transform;
        tooltipElement.HudContent = content;
        EncompassElementWithBackground(tooltipElement);

        PositionPopup();
        ShowWindow();
    }

    protected abstract void PositionPopup();

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the provided content.
    /// </summary>
    /// <param name="content">The content.</param>
    public void Show(AHudElementContent content) {
        SetContent(content);
    }

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the provided text.
    /// </summary>
    /// <param name="text">The text.</param>
    public void Show(string text) {
        if (!text.IsNullOrEmpty()) {
            Show(new TextHudContent(text));
        }
    }

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the content of the StringBuilder.
    /// </summary>
    /// <param name="coloredStringBuilder">The StringBuilder containing text.</param>
    public void Show(StringBuilder stringBuilder) {
        Show(new TextHudContent(stringBuilder.ToString()));
    }

    /// <summary>
    /// Shows the tooltip at the mouse screen position using the content of the ColoredStringBuilder.
    /// </summary>
    /// <param name="coloredStringBuilder">The ColoredStringBuilder containing colorized text.</param>
    public void Show(ColoredStringBuilder coloredStringBuilder) {
        Show(new TextHudContent(coloredStringBuilder.ToString()));
    }

    /// <summary>
    /// Hide the tooltip.
    /// </summary>
    public void Hide() {
        HideWindow();
    }

    #region Cleanup

    protected sealed override void OnDestroy() {
        base.OnDestroy();
    }

    protected override void Cleanup() {
        base.Cleanup();
        _instance = null;
    }

    protected override void OnApplicationQuit() {
        base.OnApplicationQuit();
        _instance = null;
    }

    #endregion

}

