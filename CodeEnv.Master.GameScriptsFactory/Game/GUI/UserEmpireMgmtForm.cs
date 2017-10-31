﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserEmpireMgmtForm.cs
// AInfoDisplayForm that is fixed on the screen displaying User's empire information.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AInfoDisplayForm that is fixed on the screen displaying User's empire information.
/// <remarks>Also contains the screen access buttons which operate independently from this script.</remarks>
/// </summary>
public class UserEmpireMgmtForm : AInfoDisplayForm {

    public override FormID FormID { get { return FormID.UserEmpireMgmt; } }

    private UserPlayerKnowledge _userKnowledge;
    private IList<IDisposable> _subscriptions;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        GameManager.Instance.isReadyForPlayOneShot += IsReadyForPlayEventHandler;
    }

    public override void PopulateValues() {
        AssignValuesToMembers();
    }

    private void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_userKnowledge.SubscribeToPropertyChanged<PlayerKnowledge, OutputsYield>(uk => uk.TotalOutputs, TotalOutputsPropChangedHandler));
        _subscriptions.Add(_userKnowledge.SubscribeToPropertyChanged<PlayerKnowledge, ResourcesYield>(uk => uk.TotalResources, TotalResourcesPropChangedHandler));
    }

    #region Event and Property Change Handlers

    private void IsReadyForPlayEventHandler(object sender, EventArgs e) {
        _userKnowledge = GameManager.Instance.UserAIManager.Knowledge;
        Subscribe();
        PopulateValues();
    }

    private void TotalOutputsPropChangedHandler() {
        RefreshValueOfOutputsGuiElement();
    }

    private void TotalResourcesPropChangedHandler() {
        RefreshValueOfResourcesGuiElement();
    }

    #endregion

    private void RefreshValueOfOutputsGuiElement() {
        _outputsGuiElement.ResetForReuse();
        AssignValueToOutputsGuiElement();
    }

    private void RefreshValueOfResourcesGuiElement() {
        _resourcesGuiElement.ResetForReuse();
        AssignValueToResourcesGuiElement();
    }

    protected override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = _userKnowledge.TotalOutputs;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        _resourcesGuiElement.Resources = _userKnowledge.TotalResources;
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(d => d.Dispose());
        _subscriptions.Clear();
    }

    protected override void Cleanup() {
        base.Cleanup();
        Unsubscribe();
    }

}

