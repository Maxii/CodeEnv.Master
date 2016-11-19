// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CtxMenuInspector.cs
// Inspector editor class for CtxMenu.
// Derived from Troy Heere's Contextual with permission.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector editor class for CtxMenu.
/// </summary>
/// <remarks>Derived from Troy Heere's Contextual with permission.</remarks>
[CustomEditor(typeof(CtxMenu))]
public class CtxMenuInspector : ACtxMenuItemInspector {

    private CtxMenu _contextMenu;
    private bool _refresh = false;

    public override void RegisterUndo() {
        NGUIEditorTools.RegisterUndo("Context Menu Change", _contextMenu);
        _refresh = true;
    }

    public override void OnInspectorGUI() {
        _contextMenu = target as CtxMenu;

        EditorGUIUtility.labelWidth = 80f;

        ComponentSelector.Draw<UIAtlas>(_contextMenu.atlas, OnSelectAtlas, false, GUILayout.Width(140f));

        //if (NGUIEditorTools.DrawPrefixButton("Atlas"))
        //	ComponentSelector.Show<UIAtlas>(OnSelectAtlas);

        EditorGUILayout.BeginHorizontal();

        CtxMenu.Style style = (CtxMenu.Style)EditorGUILayout.EnumPopup("Style", _contextMenu.style, GUILayout.Width(180f));
        if (_contextMenu.style != style) {
            RegisterUndo();
            _contextMenu.style = style;

            if (style == CtxMenu.Style.Pie) {
                if (_contextMenu.isMenuBar) {
                    _contextMenu.isMenuBar = false;
                    CtxHelper.DestroyAllChildren(_contextMenu.transform);

                    UIPanel panel = NGUITools.FindInParents<UIPanel>(_contextMenu.gameObject);
                    if (panel != null) {
                        panel.Refresh();
                    }
                    _refresh = false;
                }
            }
        }

        if (_contextMenu.style != CtxMenu.Style.Pie) {
            GUILayout.Space(32f);

            bool menuBar = EditorGUILayout.Toggle("Menu Bar", _contextMenu.isMenuBar);
            if (_contextMenu.isMenuBar != menuBar) {
                RegisterUndo();
                _contextMenu.isMenuBar = menuBar;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (_contextMenu.style != CtxMenu.Style.Pie) {
            UIWidget.Pivot pivot = (UIWidget.Pivot)EditorGUILayout.EnumPopup("Pivot", _contextMenu.pivot, GUILayout.Width(180f));
            if (_contextMenu.pivot != pivot) {
                RegisterUndo();
                _contextMenu.pivot = pivot;
            }
            GUILayout.Space(32f);
        }

        bool isLocalized = EditorGUILayout.Toggle("Localized", _contextMenu.isLocalized);
        if (_contextMenu.isLocalized != isLocalized) {
            RegisterUndo();
            _contextMenu.isLocalized = isLocalized;
        }

        EditorGUILayout.EndHorizontal();

        Vector2 padding = CompactVector2Field("Padding", _contextMenu.padding);  //EditorGUILayout.Vector2Field("Padding", _contextMenu.padding);
        if (_contextMenu.padding != padding) {
            RegisterUndo();
            _contextMenu.padding = padding;
        }

        EditorGUIUtility.labelWidth = 100f;

        NGUIEditorTools.DrawEvents("On Selection", _contextMenu, _contextMenu.onSelection);
        NGUIEditorTools.DrawEvents("On Show", _contextMenu, _contextMenu.onShow);
        NGUIEditorTools.DrawEvents("On Hide", _contextMenu, _contextMenu.onHide);

        EditorGUILayout.Space();

        EditorGUIUtility.labelWidth = 80f;

        Rect box = EditorGUILayout.BeginVertical();
        GUI.Box(box, "");

        if (EditorFoldout(Flags.EditBackground, "Background Options:")) {
            _contextMenu.backgroundSprite = EditSprite(_contextMenu.atlas, "Sprite", _contextMenu.backgroundSprite, OnBackground);
            Color backgroundColor = EditorGUILayout.ColorField("Normal", _contextMenu.backgroundColor);
            if (_contextMenu.backgroundColor != backgroundColor) {
                RegisterUndo();
                _contextMenu.backgroundColor = backgroundColor;
            }

            Color highlightedColor, disabledColor;

            if (_contextMenu.style == CtxMenu.Style.Pie) {
                highlightedColor = EditorGUILayout.ColorField("Highlighted", _contextMenu.backgroundColorSelected);
                if (_contextMenu.backgroundColorSelected != highlightedColor) {
                    RegisterUndo();
                    _contextMenu.backgroundColorSelected = highlightedColor;
                }

                disabledColor = EditorGUILayout.ColorField("Disabled", _contextMenu.backgroundColorDisabled);
                if (_contextMenu.backgroundColorDisabled != disabledColor) {
                    RegisterUndo();
                    _contextMenu.backgroundColorDisabled = disabledColor;
                }
            }

            GUILayout.Space(4f);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (_contextMenu.style == CtxMenu.Style.Pie) {
            EditorGUIUtility.labelWidth = 100f;

            box = EditorGUILayout.BeginVertical();
            GUI.Box(box, "");
            if (EditorFoldout(Flags.EditPie, "Pie Menu Options:")) {
                GUILayout.Space(4f);

                float pieRadius = EditorGUILayout.FloatField("Radius", _contextMenu.pieRadius);
                if (_contextMenu.pieRadius != pieRadius) {
                    RegisterUndo();
                    _contextMenu.pieRadius = pieRadius;
                }

                float pieStartingAngle = EditorGUILayout.FloatField("Starting Angle", _contextMenu.pieStartingAngle);
                if (_contextMenu.pieStartingAngle != pieStartingAngle) {
                    RegisterUndo();
                    _contextMenu.pieStartingAngle = pieStartingAngle;
                }

                float pieArc = EditorGUILayout.FloatField("Placement Arc", _contextMenu.pieArc);
                if (_contextMenu.pieArc != pieArc) {
                    RegisterUndo();
                    _contextMenu.pieArc = pieArc;
                }

                bool pieCenterItem = EditorGUILayout.Toggle("Center Items", _contextMenu.toCenterPieItems);
                if (_contextMenu.toCenterPieItems != pieCenterItem) {
                    RegisterUndo();
                    _contextMenu.toCenterPieItems = pieCenterItem;
                }

                GUILayout.Space(4f);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = 80f;
        }
        else {
            box = EditorGUILayout.BeginVertical();
            GUI.Box(box, "");
            if (EditorFoldout(Flags.EditHighlight, "Highlight Options:")) {
                GUILayout.Space(4f);
                _contextMenu.highlightSprite = EditSprite(_contextMenu.atlas, "Sprite", _contextMenu.highlightSprite, OnHighlight);
                Color highlightColor = EditorGUILayout.ColorField("Color", _contextMenu.highlightColor);
                if (_contextMenu.highlightColor != highlightColor) {
                    RegisterUndo();
                    _contextMenu.highlightColor = highlightColor;
                }

                GUILayout.Space(4f);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        box = EditorGUILayout.BeginVertical();
        GUI.Box(box, "");
        if (EditorFoldout(Flags.EditText, "Text Options:")) {
            GUILayout.Space(4f);

            ComponentSelector.Draw<UIFont>(_contextMenu.font, OnSelectFont, false, GUILayout.Width(140f));

            if (_contextMenu.font == null) {
                EditorGUILayout.HelpBox("Warning: please select a valid font if you want this menu to behave correctly.", MessageType.Warning);
            }

            float labelScale = EditorGUILayout.FloatField("Scale", _contextMenu.labelScale);
            if (_contextMenu.labelScale != labelScale) {
                RegisterUndo();
                _contextMenu.labelScale = labelScale;
            }

            Color normalColor = EditorGUILayout.ColorField("Normal", _contextMenu.labelColorNormal);
            if (_contextMenu.labelColorNormal != normalColor) {
                RegisterUndo();
                _contextMenu.labelColorNormal = normalColor;
            }

            Color highlightedColor = EditorGUILayout.ColorField("Highlighted", _contextMenu.labelColorSelected);
            if (_contextMenu.labelColorSelected != highlightedColor) {
                RegisterUndo();
                _contextMenu.labelColorSelected = highlightedColor;
            }

            Color disabledColor = EditorGUILayout.ColorField("Disabled", _contextMenu.labelColorDisabled);
            if (_contextMenu.labelColorDisabled != disabledColor) {
                RegisterUndo();
                _contextMenu.labelColorDisabled = disabledColor;
            }

            GUILayout.Space(4f);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        box = EditorGUILayout.BeginVertical();
        GUI.Box(box, "");
        if (EditorFoldout(Flags.EditCheckmark, "Checkmark Options:")) {
            GUILayout.Space(4f);
            _contextMenu.checkmarkSprite = EditSprite(_contextMenu.atlas, "Sprite", _contextMenu.checkmarkSprite, OnCheckmark);
            Color checkmarkColor = EditorGUILayout.ColorField("Color", _contextMenu.checkmarkColor);
            if (_contextMenu.checkmarkColor != checkmarkColor) {
                RegisterUndo();
                _contextMenu.checkmarkColor = checkmarkColor;
            }

            GUILayout.Space(4f);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        box = EditorGUILayout.BeginVertical();
        GUI.Box(box, "");
        if (EditorFoldout(Flags.EditSubmenu, "Submenu Options:")) {
            GUILayout.Space(4f);

            _contextMenu.submenuIndicatorSprite =
                EditSprite(_contextMenu.atlas, "Indicator", _contextMenu.submenuIndicatorSprite, OnSubmenuIndicator);

            Color submenuIndColor = EditorGUILayout.ColorField("Color", _contextMenu.submenuIndicatorColor);
            if (_contextMenu.submenuIndicatorColor != submenuIndColor) {
                RegisterUndo();
                _contextMenu.submenuIndicatorColor = submenuIndColor;
            }

            GUILayout.Space(4f);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (_contextMenu.style != CtxMenu.Style.Pie) {
            box = EditorGUILayout.BeginVertical();
            GUI.Box(box, "");
            if (EditorFoldout(Flags.EditSeparator, "Separator Options:")) {
                GUILayout.Space(4f);
                _contextMenu.separatorSprite = EditSprite(_contextMenu.atlas, "Sprite", _contextMenu.separatorSprite, OnSeparator);
                Color separatorColor = EditorGUILayout.ColorField("Color", _contextMenu.separatorColor);
                if (_contextMenu.separatorColor != separatorColor) {
                    RegisterUndo();
                    _contextMenu.separatorColor = separatorColor;
                }

                GUILayout.Space(4f);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (!_contextMenu.isMenuBar) {
            box = EditorGUILayout.BeginVertical();
            GUI.Box(box, "");

            if (EditorFoldout(Flags.EditAnimation, "Animation Options:")) {
                bool isAnimated = EditorGUILayout.Toggle("Animated", _contextMenu.isAnimated);
                if (_contextMenu.isAnimated != isAnimated) {
                    RegisterUndo();
                    _contextMenu.isAnimated = isAnimated;
                }

                float animationDuration = EditorGUILayout.FloatField("Duration", _contextMenu.animationDuration);
                if (_contextMenu.animationDuration != animationDuration) {
                    RegisterUndo();
                    _contextMenu.animationDuration = animationDuration;
                }

                EditorGUIUtility.labelWidth = 100f;

                CtxMenu.GrowDirection growDirection = (CtxMenu.GrowDirection)EditorGUILayout.EnumPopup("Grow Direction",
                    _contextMenu.growDirection, GUILayout.Width(192f));

                if (_contextMenu.growDirection != growDirection) {
                    RegisterUndo();
                    _contextMenu.growDirection = growDirection;
                }

                GUILayout.Space(4f);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        box = EditorGUILayout.BeginVertical();
        GUI.Box(box, "");

        if (EditorFoldout(Flags.EditShadow, "Shadow Options:")) {
            _contextMenu.shadowSprite = EditSprite(_contextMenu.atlas, "Sprite", _contextMenu.shadowSprite, OnShadow);
            Color shadowColor = EditorGUILayout.ColorField("Color", _contextMenu.shadowColor);
            if (_contextMenu.shadowColor != shadowColor) {
                RegisterUndo();
                _contextMenu.shadowColor = shadowColor;
            }

            Vector2 shadowOffset = CompactVector2Field("Offset", _contextMenu.shadowOffset);
            if (shadowOffset != _contextMenu.shadowOffset) {
                RegisterUndo();
                _contextMenu.shadowOffset = shadowOffset;
            }

            Vector2 shadowSizeDelta = CompactVector2Field("Size +/-", _contextMenu.shadowSizeDelta);
            if (shadowSizeDelta != _contextMenu.shadowSizeDelta) {
                RegisterUndo();
                _contextMenu.shadowSizeDelta = shadowSizeDelta;
            }

            GUILayout.Space(4f);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        box = EditorGUILayout.BeginVertical();
        GUI.Box(box, "");
        if (EditorFoldout(Flags.EditAudio, "Audio Options:")) {
            GUILayout.Space(4f);

            EditorGUIUtility.labelWidth = 70f;
            EditorGUILayout.BeginHorizontal();

            AudioClip showSound = EditorGUILayout.ObjectField("Show", _contextMenu.showSound, typeof(AudioClip), false) as AudioClip;
            if (_contextMenu.showSound != showSound) {
                RegisterUndo();
                _contextMenu.showSound = showSound;
            }
            GUILayout.Space(20f);
            AudioClip hideSound = EditorGUILayout.ObjectField("Hide", _contextMenu.hideSound, typeof(AudioClip), false) as AudioClip;
            if (_contextMenu.hideSound != hideSound) {
                RegisterUndo();
                _contextMenu.hideSound = hideSound;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            AudioClip highlightSound = EditorGUILayout.ObjectField("Highlight", _contextMenu.highlightSound, typeof(AudioClip), false) as AudioClip;
            if (_contextMenu.highlightSound != highlightSound) {
                RegisterUndo();
                _contextMenu.highlightSound = highlightSound;
            }
            GUILayout.Space(20f);
            AudioClip selectSound = EditorGUILayout.ObjectField("Select", _contextMenu.selectSound, typeof(AudioClip), false) as AudioClip;
            if (_contextMenu.selectSound != selectSound) {
                RegisterUndo();
                _contextMenu.selectSound = selectSound;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 100f;

            GUILayout.Space(4f);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        bool isEditingItems = IsEditing(Flags.EditItems);
        EditMenuItemList(ref _contextMenu.items, _contextMenu.atlas, true, ref isEditingItems);
        SetEditing(Flags.EditItems, isEditingItems);

        if (_refresh) {
            if (_contextMenu.isMenuBar) {
                _contextMenu.Refresh();
            }

            _refresh = false;
        }
        // How to tell if undo or redo have been hit:
        else if (_contextMenu.isMenuBar && Event.current.type == EventType.ValidateCommand && (Event.current.commandName == "UndoRedoPerformed")) {
            _contextMenu.Refresh();
        }
    }

    #region Event and Property Change Handlers

    void OnSelectAtlas(Object obj) {
        RegisterUndo();
        _contextMenu.atlas = obj as UIAtlas;
    }

    void OnSelectFont(Object obj) {
        RegisterUndo();
        _contextMenu.font = obj as UIFont;
    }

    void OnBackground(string spriteName) {
        RegisterUndo();
        _contextMenu.backgroundSprite = spriteName;
        Repaint();
    }

    void OnShadow(string spriteName) {
        RegisterUndo();
        _contextMenu.shadowSprite = spriteName;
        Repaint();
    }

    void OnHighlight(string spriteName) {
        RegisterUndo();
        _contextMenu.highlightSprite = spriteName;
        Repaint();
    }

    void OnCheckmark(string spriteName) {
        RegisterUndo();
        _contextMenu.checkmarkSprite = spriteName;
        Repaint();
    }

    void OnSubmenuIndicator(string spriteName) {
        RegisterUndo();
        _contextMenu.submenuIndicatorSprite = spriteName;
        Repaint();
    }

    void OnSeparator(string spriteName) {
        RegisterUndo();
        _contextMenu.separatorSprite = spriteName;
        Repaint();
    }

    #endregion

    private bool EditorFoldout(Flags flag, string label) {
        bool isEditing = EditorGUILayout.Foldout(IsEditing(flag), label);
        GUILayout.Space(4f);

        SetEditing(flag, isEditing);
        return isEditing;
    }

    private bool IsEditing(Flags flag) {
        CtxMenu contextMenu = target as CtxMenu;
        if (contextMenu != null) {
            return (contextMenu.editorFlags & ((uint)flag)) != 0;
        }
        return false;
    }

    private void SetEditing(Flags flag, bool editing) {
        CtxMenu contextMenu = target as CtxMenu;
        if (contextMenu != null) {
            if (editing) {
                contextMenu.editorFlags |= ((uint)flag);
            }
            else {
                contextMenu.editorFlags &= ~((uint)flag);
            }
        }
    }

    private Vector2 CompactVector2Field(string label, Vector2 v) {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(76f));
        EditorGUIUtility.labelWidth = 20f;
        v.x = EditorGUILayout.FloatField("X", v.x, GUILayout.Width(115));
        GUILayout.Space(15f);
        v.y = EditorGUILayout.FloatField("Y", v.y, GUILayout.Width(115));
        EditorGUILayout.EndHorizontal();

        EditorGUIUtility.labelWidth = 100f;
        return v;
    }

    #region Nested Classes

    private enum Flags {
        EditItems = (1 << 0),
        EditBackground = (1 << 1),
        EditHighlight = (1 << 2),
        EditText = (1 << 3),
        EditCheckmark = (1 << 4),
        EditSubmenu = (1 << 5),
        EditSeparator = (1 << 6),
        EditPie = (1 << 7),
        EditAnimation = (1 << 8),
        EditShadow = (1 << 9),
        EditAudio = (1 << 10),
    }

    #endregion
}
