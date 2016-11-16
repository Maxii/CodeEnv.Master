// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DefinesWindow.cs
// Editor window that holds the Conditional Compilation DEFINE Symbols I wish to include in this UnityProject.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
// WARNING: This script causes an immediate recompile whenever the toggle value is changed.

/// <summary>
/// Editor window that holds the Conditional Compilation DEFINE Symbols I wish to include in this UnityProject.
/// <remarks>11.16.16 Currently no way to remove a #define symbol that Unity builds into the editor, aka ENABLE_PROFILER</remarks>
/// </summary>
public class DefinesWindow : EditorWindow {

    private const string Define_DebugLog = "DEBUG_LOG";
    private const string Define_DebugWarn = "DEBUG_WARN";
    private const string Define_DebugError = "DEBUG_ERROR";

    /// <summary>
    /// The DEFINE that enables Vectrosity functionality in GridFramework.
    /// </summary>
    private const string Define_GridFramework = "GRID_FRAMEWORK_VECTROSITY";

    [MenuItem("My Tools/DEFINEs to Include")]
    public static void ShowDefinesWindow() {
        var window = GetWindow<DefinesWindow>();
        window.Show();
    }

    public bool _isDebugLogEnabled;

    [SerializeField]    // UNCLEAR why?
    private bool _previousDebugLogEnabledValue = true;  // most of the time I have it enabled
    private bool _isGuiEnabled = true;

    #region Event and Property Change Handlers

    void OnEnable() {
        EditorApplication.playmodeStateChanged += PlayModeStateChangedEventHandler;
    }

    void OnDisable() {
        EditorApplication.playmodeStateChanged -= PlayModeStateChangedEventHandler;
    }

    void OnGUI() {
        CheckForRecompile();
        GUI.enabled = _isGuiEnabled;
        _isDebugLogEnabled = EditorGUILayout.Toggle("Enable Scripts Debug Log", _isDebugLogEnabled);
        GUI.enabled = true;
        if (_isGuiEnabled && _isDebugLogEnabled != _previousDebugLogEnabledValue) {
            _previousDebugLogEnabledValue = _isDebugLogEnabled;
            DebugLogEnabledChangedHandler();
        }
    }

    private void PlayModeStateChangedEventHandler() {
        _isGuiEnabled = !EditorApplication.isPlaying;
        Repaint();
        // Debug.Log(string.Format("OnPlayModeStateChanged(). Gui.enabled = {0}.", _isGuiEnabled));
    }

    private void DebugLogEnabledChangedHandler() {
        CheckConditionalCompilationSettings();
    }

    #endregion

    private RecompileChecker _recompileCheck;
    private void CheckForRecompile() {
        if (_recompileCheck == null) {
            // Unity has recompiled
            _recompileCheck = new RecompileChecker();
            IEnumerable<string> activeSymbols = EditorUserBuildSettings.activeScriptCompilationDefines;
            activeSymbols = activeSymbols.OrderBy(sym => sym, new DefineStringComparer(StringComparer.CurrentCulture));
            string activeSymbolText = string.Empty;
            bool isFirstSymbol = true;
            foreach (var symbol in activeSymbols) {
                if (isFirstSymbol) {
                    activeSymbolText += symbol;
                    isFirstSymbol = false;
                    continue;
                }
                activeSymbolText += (", " + symbol);
            }
            Debug.LogFormat("Recompile Completed. Active #Define Symbols: {0}.", activeSymbolText);
        }
    }

    private void CheckConditionalCompilationSettings() {
        BuildTargetGroup[] platformTargets = new BuildTargetGroup[1] { BuildTargetGroup.Standalone };
        IList<string> definesToInclude = new List<string>() { Define_DebugError, Define_DebugWarn, Define_GridFramework };
        if (_isDebugLogEnabled) {
            definesToInclude.Add(Define_DebugLog);
        }
        UnityEditorUtility.ResetConditionalCompilation(platformTargets, definesToInclude.ToArray<string>());
    }

    #region Nested Classes

    private class RecompileChecker { }

    private class DefineStringComparer : IComparer<string> {

        private readonly IComparer<string> _baseComparer;

        public DefineStringComparer(IComparer<string> baseComparer) {
            _baseComparer = baseComparer;
        }

        public int Compare(string x, string y) {
            if (_baseComparer.Compare(x, y) == 0) {
                return 0;
            }
            // DEBUG_LOG comes before everything else
            if (_baseComparer.Compare(x, DefinesWindow.Define_DebugLog) == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, DefinesWindow.Define_DebugLog) == 0) {
                return 1;
            }

            // DEBUG_WARN comes next
            if (_baseComparer.Compare(x, DefinesWindow.Define_DebugWarn) == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, DefinesWindow.Define_DebugWarn) == 0) {
                return 1;
            }

            // DEBUG_ERROR comes next
            if (_baseComparer.Compare(x, DefinesWindow.Define_DebugError) == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, DefinesWindow.Define_DebugError) == 0) {
                return 1;
            }

            // GRID_FRAMEWORK_VECTROSITY comes next
            if (_baseComparer.Compare(x, DefinesWindow.Define_GridFramework) == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, DefinesWindow.Define_GridFramework) == 0) {
                return 1;
            }

            return _baseComparer.Compare(x, y);
        }
    }

    #endregion
}

