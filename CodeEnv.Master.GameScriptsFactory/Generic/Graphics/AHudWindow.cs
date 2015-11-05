// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AHudWindow.cs
// Singleton. Abstract Gui Window for showing customized Forms that 'popup' at various locations on the screen. 
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. Abstract Gui Window for showing customized Forms that 'popup' at various locations on the screen. 
/// HudWindows have the ability to envelop their background around the Form they are displaying.
/// Each derived type instance can handle multiple Forms.
/// Current derived classes include TooltipHudWindow, the left side HoveredItemHudWindow and the bottom SelectedItemHudWindow.
/// </summary>
public abstract class AHudWindow<T> : AGuiWindow where T : AHudWindow<T> {

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
                if (_instance == null) { //if (_instance == null && !Application.isLoadingLevel) { Application.isLoadingLevel deprecated in Unity5
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

    private Transform _contentHolder;
    protected override Transform ContentHolder { get { return _contentHolder; } }

    protected UIWidget _backgroundWidget;

    private IDictionary<FormID, AForm> _formLookup;
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
        InitializeFormLookup();
        InitializeContentHolder();
        WireDelegates();
        DeactivateAllForms();
    }

    protected override void AcquireReferences() {
        base.AcquireReferences();
        _backgroundEnvelopContent = gameObject.GetSingleComponentInChildren<MyEnvelopContent>();
        // TODO set envelopContent padding programmatically once background is permanently picked?
        _backgroundWidget = _backgroundEnvelopContent.gameObject.GetSafeComponent<UIWidget>();
    }

    private void InitializeFormLookup() {
        var hudForms = gameObject.GetSafeComponentsInChildren<AForm>();
        _formLookup = hudForms.ToDictionary(form => form.FormID);
    }

    private void InitializeContentHolder() {
        _contentHolder = _formLookup.Values.First().transform;
    }

    private void WireDelegates() {
        onHideComplete.Add(new EventDelegate(DeactivateAllForms));
    }

    /// <summary>
    /// Activates the specified form's gameObject, deactivating all others.
    /// </summary>
    /// <param name="form">The form to activate.</param>
    private void ActivateForm(AForm form) {
        Arguments.ValidateNotNull(form);
        DeactivateAllForms();
        NGUITools.SetActive(form.gameObject, true);
    }

    private void DeactivateAllForms() {
        _formLookup.Values.ForAll(f => NGUITools.SetActive(f.gameObject, false));
    }

    private void EncompassFormWithBackground(AForm form) {
        _backgroundEnvelopContent.targetRoot = form.transform;
        _backgroundEnvelopContent.Execute();
    }

    /// <summary>
    /// Prepares the form ID'd by formID for showing and returns it.
    /// </summary>
    /// <param name="formID">The form identifier.</param>
    /// <returns></returns>
    protected AForm PrepareForm(FormID formID) {
        var form = _formLookup[formID];
        form.Reset();
        ActivateForm(form);
        _contentHolder = form.transform;
        return form;
    }

    /// <summary>
    /// Shows the form in the window after it has been prepared.
    /// </summary>
    /// <param name="form">The form.</param>
    protected void ShowForm(AForm form) {
        EncompassFormWithBackground(form);
        PositionWindow();
        ShowWindow();
    }

    protected abstract void PositionWindow();

    /// <summary>
    /// Hide the window.
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

