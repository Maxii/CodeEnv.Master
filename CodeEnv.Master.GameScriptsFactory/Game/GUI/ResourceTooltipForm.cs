// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceTooltipForm.cs
// Form that is used to display a static description of a Resource in the TooltipHudWindow.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Form that is used to display a static description of a Resource in the TooltipHudWindow.
/// <remarks>Does not handle dynamic value changes.</remarks>
/// </summary>
public class ResourceTooltipForm : AForm {

    public override FormID FormID { get { return FormID.ResourceTooltip; } }

    [SerializeField]
    private UILabel _categoryLabel = null;
    [SerializeField]
    private UILabel _descriptionLabel = null;
    [SerializeField]
    private UILabel _imageNameLabel = null;
    [SerializeField]
    private UISprite _imageSprite = null;

    private ResourceID _resourceID;
    public ResourceID ResourceID {
        get { return _resourceID; }
        set {
            D.AssertDefault((int)_resourceID);   // occurs only once between Resets
            SetProperty<ResourceID>(ref _resourceID, value, "ResourceID");
        }
    }

    protected override void InitializeValuesAndReferences() { }

    public sealed override void PopulateValues() {
        D.AssertNotDefault((int)ResourceID);
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        _categoryLabel.text = ResourceID.GetResourceCategory().GetValueName();
        _descriptionLabel.text = ResourceID.GetDescription();
        _imageSprite.atlas = ResourceID.GetImageAtlasID().GetAtlas();
        _imageSprite.spriteName = ResourceID.GetImageFilename();
        _imageNameLabel.text = ResourceID.GetValueName();
    }

    protected override void ResetForReuse_Internal() {
        _resourceID = default(ResourceID);
    }

    protected override void Cleanup() { }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_categoryLabel);
        D.AssertNotNull(_descriptionLabel);
        D.AssertNotNull(_imageNameLabel);
        D.AssertNotNull(_imageSprite);
    }

    #endregion


}

