﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Loader.cs
// Manages the initial sequencing of scene startups.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Manages the initial sequencing of scene startups.
/// </summary>
public class Loader : AMonoSingleton<Loader> {

    public int TargetFPS = 25;

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    private IList<IDisposable> _subscribers;
    private PlayerPrefsManager _playerPrefsMgr;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        _playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeQualitySettings();
        InitializeVectrosity();
        Subscribe();
    }

    private void InitializeQualitySettings() {
        // the initial QualitySettingChanged event occurs earlier than we can subscribe so do it manually
        OnQualitySettingChanged();
    }

    private void InitializeVectrosity() {
        // Note: not necessary to use VectorLine.SetCamera3D(mainCamera) as the default camera for 3D lines Vectrosity finds is mainCamera

        //VectorLine.useMeshLines = true; // removed in Vectrosity 4.0
        //VectorLine.useMeshPoints = true;    // removed in Vectrosity 4.0
        //VectorLine.useMeshQuads = true;       // removed in Vectrosity 3.0 as no advantages to using it
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_playerPrefsMgr.SubscribeToPropertyChanged<PlayerPrefsManager, string>(ppm => ppm.QualitySetting, OnQualitySettingChanged));
    }

    private void CheckDebugSettings() {
        if (DebugSettings.Instance.ForceFpsToTarget) {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFPS;
        }
    }

    private void OnQualitySettingChanged() {
        string newQualitySetting = _playerPrefsMgr.QualitySetting;
        if (newQualitySetting != QualitySettings.names[QualitySettings.GetQualityLevel()]) {
            // EDITOR Quality Level Changes will not be saved while in Editor play mode
            int newQualitySettingIndex = QualitySettings.names.IndexOf(newQualitySetting);
            QualitySettings.SetQualityLevel(newQualitySettingIndex, applyExpensiveChanges: true);
        }
        CheckDebugSettings();
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

