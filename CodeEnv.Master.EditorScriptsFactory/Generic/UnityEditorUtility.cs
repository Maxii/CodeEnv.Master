// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityEditorUtility.cs
// Static utility class with methods that support scripts in this project
// that access the Editor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Static utility class with methods that support scripts in this project that access the Editor.
/// </summary>
public static class UnityEditorUtility {

    /// <summary>
    /// Static helper method that makes sure the conditional compilation define symbols specified are included
    /// in PlayerSettings for the platform targets specified, and if they are not, they are added. This method does
    /// not remove symbols already present. Also, once present in PlayerSettings, they persist through editor sessions.
    /// </summary>
    /// <see cref="http://wiki.unity3d.com/index.php/Tip_of_the_day"/> Tip #50.
    /// <param name="platformTargets">The platform targets.</param>
    /// <param name="symbolsToInclude">The symbols automatic include.</param>
    public static void AddConditionalCompilation(BuildTargetGroup[] platformTargets, string[] symbolsToInclude) {
        foreach (var platformTarget in platformTargets) {
            bool[] isSymbolToIncludeAlreadyPresent = new bool[symbolsToInclude.Length];
            string symbolsAlreadyPresentString = PlayerSettings.GetScriptingDefineSymbolsForGroup(platformTarget).Trim();
            string[] symbolsAlreadyPresent = symbolsAlreadyPresentString.Split(';');
            bool isDirty = false;

            foreach (var symbolAlreadyPresent in symbolsAlreadyPresent) {
                for (int i = 0; i < symbolsToInclude.Length; i++) {
                    if (symbolAlreadyPresent.Trim() == symbolsToInclude[i].Trim()) {
                        isSymbolToIncludeAlreadyPresent[i] = true;
                        break;
                    }
                }
            }

            for (int i = 0; i < isSymbolToIncludeAlreadyPresent.Length; i++) {
                if (!isSymbolToIncludeAlreadyPresent[i]) {
                    symbolsAlreadyPresentString += (String.IsNullOrEmpty(symbolsAlreadyPresentString) ? String.Empty : ";") + symbolsToInclude[i];
                    isDirty = true;
                }
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(platformTarget, symbolsAlreadyPresentString);

            if (isDirty) {
                Debug.Log(String.Format("Updated player conditional compilation symbols for {0}: {1}", platformTarget, symbolsAlreadyPresentString));
            }
        }
    }

    /// <summary>
    /// Static helper method that checks if the symbolsToInclude are the same as the ones already present, and if not, 
    /// rebuilds the conditional compilation define symbols in PlayerSettings to those specified
    /// for the platform targets specified. This method potentially removes all previously present symbols and replaces them
    /// with those provided. Also, once present in PlayerSettings, they persist through editor sessions unless this method is
    /// run again.
    /// </summary>
    /// <param name="platformTargets">The platform targets.</param>
    /// <param name="symbolsToInclude">The symbols automatic include.</param>
    public static void ResetConditionalCompilation(BuildTargetGroup[] platformTargets, string[] symbolsToInclude) {
        foreach (var platformTarget in platformTargets) {
            string symbolsAlreadyPresentString = PlayerSettings.GetScriptingDefineSymbolsForGroup(platformTarget).Trim();
            string[] symbolsAlreadyPresent = symbolsAlreadyPresentString.Split(';');
            bool isSameSet = symbolsAlreadyPresent.OrderBy(s => s.Trim()).SequenceEqual<string>(symbolsToInclude.OrderBy(s => s.Trim()));
            if (isSameSet) {
                Debug.Log("Conditional Compilation Symbols unchanged.");
                continue;
            }

            string newSymbolString = string.Empty;

            bool isFirstSymbol = true;
            foreach (var symbol in symbolsToInclude) {
                if (isFirstSymbol) {
                    newSymbolString += symbol.Trim();
                    isFirstSymbol = false;
                }
                else {
                    newSymbolString += Constants.SemiColon + symbol.Trim();
                }
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(platformTarget, newSymbolString);
            Debug.LogWarning(string.Format("Reset player conditional compilation symbols for {0}: {1}", platformTarget, newSymbolString));
        }
    }

    /// <summary>
    /// Recursively draws default inspectors for the fields in target.
    /// </summary>
    /// <see cref="http://devmag.org.za/2012/07/12/50-tips-for-working-with-unity-best-practices/"/>
    /// <typeparam name="T">The specific type of MonoBehaviour.</typeparam>
    /// <param name="label">The label. not currently used.</param>
    /// <param name="target">The MonoBehaviour containing the fields to draw.</param>
    public static void DrawDefaultInspectors<T>(GUIContent label, T target) where T : MonoBehaviour {
        EditorGUILayout.Separator();
        Type type = typeof(T);
        FieldInfo[] fields = type.GetFields();
        EditorGUI.indentLevel++;

        foreach (FieldInfo field in fields) {
            if (field.IsPublic) {
                if (field.FieldType == typeof(int)) {
                    field.SetValue(target, EditorGUILayout.IntField(MakeLabel(field), (int)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(float)) {
                    field.SetValue(target, EditorGUILayout.FloatField(MakeLabel(field), (float)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(bool)) {
                    field.SetValue(target, EditorGUILayout.Toggle(MakeLabel(field), (bool)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(Vector3)) {
                    field.SetValue(target, EditorGUILayout.Vector3Field(MakeLabel(field).text, (Vector3)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(Vector2)) {
                    field.SetValue(target, EditorGUILayout.Vector2Field(MakeLabel(field).text, (Vector2)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(Color)) {
                    field.SetValue(target, EditorGUILayout.ColorField(MakeLabel(field), (Color)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(Enum)) { // OPTIMIZE not sure about this, need specific enum types?
                    field.SetValue(target, EditorGUILayout.EnumPopup(MakeLabel(field), (Enum)field.GetValue(target)));
                }
                else if (field.FieldType == typeof(LayerMask)) {
                    field.SetValue(target, LayerMaskField(MakeLabel(field).text, (LayerMask)field.GetValue(target)));
                }

                ///etc. for other primitive types

                else if (field.FieldType.IsClass) {
                    Type[] parmTypes = new Type[] { field.FieldType };

                    string methodName = "DrawDefaultInspectors";
                    MethodInfo drawMethod = typeof(UnityEditorUtility).GetMethod(methodName);
                    if (drawMethod == null) {
                        D.Error("No method found: {0}.", methodName);
                    }

                    //bool foldOut = true;

                    drawMethod.MakeGenericMethod(parmTypes).Invoke(null, new object[] {
                        MakeLabel(field),
                        field.GetValue(target)
                    });
                }
                else {
                    D.Error("DrawDefaultInspectors does not support fields of type {0}.", field.FieldType);
                }
            }
        }
        EditorGUI.indentLevel--;
    }

    private static GUIContent MakeLabel(FieldInfo field) {
        GUIContent guiContent = new GUIContent();
        guiContent.text = Utility.SplitCamelCase(field.Name);
        object[] descriptions = field.GetCustomAttributes(typeof(DescriptionAttribute), true);

        if (descriptions.Length > 0) {
            //just use the first one.
            guiContent.tooltip = (descriptions[0] as DescriptionAttribute).Description;
        }

        return guiContent;
    }

    /// <summary>
    /// Make a LayerMask field without a label, providing a drop down list for the selection of one or multiple layers.
    /// </summary>
    /// <param name="mask">The LayerMask or LayerMask.value as an int. Either will work as implicitly converted.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static int LayerMaskField(int mask, params GUILayoutOption[] options) {
        return LayerMaskField(null, mask, options);
    }

    /// <summary>
    /// Make a LayerMask field, providing a drop down list for the selection of one or multiple layers.    
    /// Originally from:
    /// http://answers.unity3d.com/questions/60959/mask-field-in-the-editor.html
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="mask">The LayerMask or LayerMask.value as an int. Either will work as implicitly converted. </param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static int LayerMaskField(string label, int mask, params GUILayoutOption[] options) {
        List<string> layers = new List<string>();
        List<int> layerNumbers = new List<int>();

        string selectedLayers = "";

        for (int i = 0; i < 32; ++i) {
            string layerName = LayerMask.LayerToName(i);

            if (!string.IsNullOrEmpty(layerName)) {
                if (mask == (mask | (1 << i))) {
                    if (string.IsNullOrEmpty(selectedLayers)) {
                        selectedLayers = layerName;
                    }
                    else {
                        selectedLayers = "Mixed";
                    }
                }
            }
        }

        if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.ExecuteCommand) {
            if (mask == 0) {
                layers.Add("Nothing");
            }
            else if (mask == -1) {
                layers.Add("Everything");
            }
            else {
                layers.Add(selectedLayers);
            }
            layerNumbers.Add(-1);
        }

        layers.Add((mask == 0 ? "[+] " : "      ") + "Nothing");
        layerNumbers.Add(-2);

        layers.Add((mask == -1 ? "[+] " : "      ") + "Everything");
        layerNumbers.Add(-3);

        for (int i = 0; i < 32; ++i) {
            string layerName = LayerMask.LayerToName(i);

            if (layerName != "") {
                if (mask == (mask | (1 << i))) {
                    layers.Add("[+] " + layerName);
                }
                else {
                    layers.Add("      " + layerName);
                }
                layerNumbers.Add(i);
            }
        }

        bool preChange = GUI.changed;

        GUI.changed = false;

        int newSelected = 0;

        if (Event.current.type == EventType.MouseDown) {
            newSelected = -1;
        }

        if (string.IsNullOrEmpty(label)) {
            newSelected = EditorGUILayout.Popup(newSelected, layers.ToArray(), EditorStyles.layerMaskField, options);
        }
        else {
            newSelected = EditorGUILayout.Popup(label, newSelected, layers.ToArray(), EditorStyles.layerMaskField, options);
        }

        if (GUI.changed && newSelected >= 0) {
            if (newSelected == 0) {
                mask = 0;
            }
            else if (newSelected == 1) {
                mask = -1;
            }
            else {
                if (mask == (mask | (1 << layerNumbers[newSelected]))) {
                    mask &= ~(1 << layerNumbers[newSelected]);
                }
                else {
                    mask = mask | (1 << layerNumbers[newSelected]);
                }
            }
        }
        else {
            GUI.changed = preChange;
        }
        return mask;
    }



}

