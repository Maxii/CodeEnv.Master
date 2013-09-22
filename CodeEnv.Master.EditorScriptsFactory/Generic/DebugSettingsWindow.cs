// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSettingsWindow.cs
// Editor window that holds my toggle for changing my unity project
// Conditional Compilation Symbols, in particular DEBUG_LOG.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
// WARNING: As this script causes an immediate recompile whenever the toggle
// value is changed, I should not add any usings from the rest of the project! 

/// <summary>
/// Editor window that holds my toggle for changing my unity project
/// Conditional Compilation Symbols, in particular DEBUG_LOG.
/// </summary>
public class DebugSettingsWindow : EditorWindow {

    public bool _isDebugLogEnabled;

    [SerializeField]
    private bool _previousDebugLogEnabledValue;
    private bool _isGuiEnabled = true;

    [MenuItem("My Tools/Debug Settings")]
    public static void ShowDebugSettingsWindow() {
        var window = GetWindow<DebugSettingsWindow>();
        window.Show();
    }

    void OnEnable() {
        EditorApplication.playmodeStateChanged = OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged() {
        _isGuiEnabled = !EditorApplication.isPlaying;
        Repaint();
        // Debug.Log(string.Format("OnPlayModeStateChanged(). Gui.enabled = {0}.", _isGuiEnabled));
    }

    void OnGUI() {
        CheckForRecompile();
        //EditorGUIUtility.LookLikeInspector();
        //EditorGUIUtility.LookLikeControls();    // the default as Toggle is a control
        GUI.enabled = _isGuiEnabled;
        _isDebugLogEnabled = EditorGUILayout.Toggle("Enable Debug Log", _isDebugLogEnabled);
        //_enableDebugLog = EditorGUI.Toggle(new Rect(0, 5, position.width, 20), "Enable Debug Log", _enableDebugLog);
        GUI.enabled = true;
        if (_isGuiEnabled && _isDebugLogEnabled != _previousDebugLogEnabledValue) {
            _previousDebugLogEnabledValue = _isDebugLogEnabled;
            OnDebugLogEnabledChanged();
        }
    }

    private class RecompileChecker { }
    private RecompileChecker _recompileCheck;
    private void CheckForRecompile() {
        if (_recompileCheck == null) {
            // Unity has recompiled
            _recompileCheck = new RecompileChecker();
            IEnumerable<string> activeSymbols = EditorUserBuildSettings.activeScriptCompilationDefines;
            //string[] debugSymbols = { "DEBUG_LOG", "DEBUG_WARN", "DEBUG_ERROR" };
            activeSymbols = activeSymbols.OrderBy(sym => sym, new DebugStringComparer(StringComparer.CurrentCulture));
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
            Debug.Log(string.Format("Recompile Completed. Active Symbols: {0}.", activeSymbolText));
        }
    }

    private void OnDebugLogEnabledChanged() {
        CheckConditionalCompilationSettings();
    }

    private void CheckConditionalCompilationSettings() {
        BuildTargetGroup[] platformTargets = new BuildTargetGroup[1] { BuildTargetGroup.Standalone };
        IList<string> definesToInclude = new List<string>() { "DEBUG_ERROR", "DEBUG_WARN" };
        if (_isDebugLogEnabled) {
            definesToInclude.Add("DEBUG_LOG");
        }
        UnityEditorUtility.ResetConditionalCompilation(platformTargets, definesToInclude.ToArray<string>());

    }

    private class DebugStringComparer : IComparer<string> {

        private readonly IComparer<string> _baseComparer;
        public DebugStringComparer(IComparer<string> baseComparer) {
            _baseComparer = baseComparer;
        }
        public int Compare(string x, string y) {
            if (_baseComparer.Compare(x, y) == 0) {
                return 0;
            }
            // DEBUG_LOG comes before everything else
            if (_baseComparer.Compare(x, "DEBUG_LOG") == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, "DEBUG_LOG") == 0) {
                return 1;
            }

            // DEBUG_WARN comes next
            if (_baseComparer.Compare(x, "DEBUG_WARN") == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, "DEBUG_WARN") == 0) {
                return 1;
            }

            // DEBUG_ERROR comes next
            if (_baseComparer.Compare(x, "DEBUG_ERROR") == 0) {
                return -1;
            }
            if (_baseComparer.Compare(y, "DEBUG_ERROR") == 0) {
                return 1;
            }

            return _baseComparer.Compare(x, y);
        }
    }
}

