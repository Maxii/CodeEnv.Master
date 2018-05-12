// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AnimateOnHover.cs
// Changes the sprite to an 'onHover' version when hovered.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Changes the sprite to an 'onHover' version when hovered. 
/// <remarks>IMPROVE Use of UISprite.MakePixelPerfect increases the size of the sprite when the sprite has padding.</remarks>
/// </summary>
public class AnimateOnHover : AMonoBase {

    private const string DebugNameFormat = "{0}.{1}";

    /// <summary>
    /// The sprite to change when moving between hovered and not hovered.
    /// </summary>
    public string DebugName { get { return DebugNameFormat.Inject(gameObject.name, GetType().Name); } }

    public UISprite targetSprite;   // Editor requires public

    /// <summary>
    /// The name of the sprite to use when hovered.
    /// </summary>
    [SerializeField]
    private string _hoveredSpriteName = null;

    private string _normalSpriteName;

    protected override void Awake() {
        base.Awake();
        __ValidateOnAwake();
    }

    protected override void Start() {
        base.Start();
        if (targetSprite != null) {
            _normalSpriteName = targetSprite.spriteName;
            //targetSprite.MakePixelPerfect();
        }
    }

    void OnHover(bool isOver) {
        //D.Log("{0}.OnHover({1}) called. HoveredSpriteName = {2}.", DebugName, isOver, _hoveredSpriteName);
        if (targetSprite != null && !_hoveredSpriteName.IsNullOrEmpty()) {
            targetSprite.spriteName = isOver ? _hoveredSpriteName : _normalSpriteName;
            //targetSprite.MakePixelPerfect();
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateOnAwake() {
        if (targetSprite == null) {
            D.WarnContext(gameObject, "{0} has no target sprite selected.", DebugName);
        }
        else {
            if (targetSprite.type != UIBasicSprite.Type.Simple && targetSprite.type != UIBasicSprite.Type.Filled) {
                D.WarnContext(gameObject, "{0} Type {1} may not scale properly during runtime spriteName change.",
                    DebugName, targetSprite.type.GetValueName());
                // see http://www.tasharen.com/forum/index.php?topic=1200.0
                // targetSprite.MakePixelPerfect can increase the size of the sprite as it includes any texture padding
                targetSprite.type = UIBasicSprite.Type.Simple;
            }
            if (targetSprite.spriteName.IsNullOrEmpty()) {
                D.WarnContext(gameObject, "{0} _targetSprite has no sprite name.", DebugName);
            }
        }
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

}

