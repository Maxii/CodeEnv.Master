// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFormWindow.cs
// Singleton. Abstract Gui Window for showing customized Forms.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. Abstract Gui Window for showing customized Forms. Each derived type instance can handle multiple Forms.
/// <remarks>10.5.17 Current derived classes include dynamically positioned TooltipHudWindow, HoveredHudWindow 
/// the fixed InteractableHudWindow, UnitHudWindow and the full screen TableWindow.</remarks>
/// </summary>
public abstract class AFormWindow<T> : AGuiWindow where T : AFormWindow<T> {

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

    private IDictionary<FormID, AForm> _formLookup;

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
        __ValidateOnAwake();
        InitializeFormLookup();
        InitializeContentHolder();
        DeactivateAllForms();
    }

    private void InitializeFormLookup() {
        var forms = GetChildFormsToInitialize();
        D.Log("{0} found {1} Form children: {2}.", DebugName, forms.Count(), forms.Select(form => form.DebugName).Concatenate());
        _formLookup = forms.ToDictionary(form => form.FormID, FormIDEqualityComparer.Default);
    }

    /// <summary>
    /// Returns the forms that are children of this Window that should be initialized.
    /// <remarks>Handled this way as ATableRowForms can be present as children of TableWindow for debug purposes.</remarks>
    /// </summary>
    /// <returns></returns>
    protected virtual IEnumerable<AForm> GetChildFormsToInitialize() {
        return gameObject.GetSafeComponentsInChildren<AForm>(excludeSelf: true, includeInactive: true);
    }

    private void InitializeContentHolder() {
        _contentHolder = _formLookup.Values.First().transform;
    }

    #region Event and Property Change Handlers

    #endregion

    /// <summary>
    /// Activates the specified form's gameObject, deactivating all others.
    /// </summary>
    /// <param name="form">The form to activate.</param>
    private void ActivateForm(AForm form) {
        DeactivateAllForms();
        NGUITools.SetActive(form.gameObject, true);
    }

    /// <summary>
    /// Prepares the form ID'd by formID for showing and returns it.
    /// </summary>
    /// <param name="formID">The form identifier.</param>
    /// <returns></returns>
    protected AForm PrepareForm(FormID formID) {
        var form = _formLookup[formID];
        ActivateForm(form);
        form.ResetForReuse();
        _contentHolder = form.transform;
        return form;
    }

    /// <summary>
    /// Shows the form in the window after it has been prepared.
    /// </summary>
    /// <param name="form">The form.</param>
    protected virtual void ShowForm(AForm form) {
        form.PopulateValues();
        ShowWindow();
    }

    /// <summary>
    /// Hide the window.
    /// </summary>
    public void Hide() {
        //D.Log("{0}.Hide() called at {1}.", DebugName, Utility.TimeStamp);
        HideWindow();
    }

    protected override void ResetForReuse() {
        _formLookup.Values.ForAll(f => f.ResetForReuse());  // IMPROVE Brute force approach
        DeactivateAllForms();
    }

    private void DeactivateAllForms() {
        _formLookup.Values.ForAll(f => NGUITools.SetActive(f.gameObject, false));
    }

    #region Cleanup

    protected sealed override void OnDestroy() {
        base.OnDestroy();
        _instance = null;
    }

    #endregion

    #region Debug

    protected virtual void __ValidateOnAwake() { }

    #endregion

}

