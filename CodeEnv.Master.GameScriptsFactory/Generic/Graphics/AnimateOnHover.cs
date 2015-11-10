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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Changes the sprite to an 'onHover' version when hovered. 
/// </summary>
public class AnimateOnHover : AMonoBase {

    /// <summary>
    /// The sprite to change when moving between hovered and not hovered.
    /// </summary>
    public UISprite targetSprite;   // Has Editor

    /// <summary>
    /// The name of the sprite to use when hovered.
    /// </summary>
    public string hoveredSpriteName;

    private string _normalSpriteName;

    protected override void Awake() {
        base.Awake();
        D.WarnContext(targetSprite == null, gameObject, "{0}.{1} has no target sprite selected.", gameObject.name, GetType().Name);
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override void Start() {
        base.Start();
        if (targetSprite != null) {
            //D.WarnContext(targetSprite.spriteName.IsNullOrEmpty(), gameObject, "{0}.{1}.targetSprite has no sprite name.", gameObject.name, GetType().Name); // sprite without a name?
            _normalSpriteName = targetSprite.spriteName;
        }
    }

    void OnHover(bool isOver) {
        //D.Log("OnHover({0}) called. HoveredSpriteName = {1}.", isOver, hoveredSpriteName);
        if (targetSprite != null && !hoveredSpriteName.IsNullOrEmpty()) {
            targetSprite.spriteName = isOver ? hoveredSpriteName : _normalSpriteName;
            targetSprite.MakePixelPerfect();
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

