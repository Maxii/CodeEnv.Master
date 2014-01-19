// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AComposition.cs
// An abstract, generic base dictionary wrapper class that holds the Elements for a Command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// An abstract, generic base dictionary wrapper class that holds the Elements for a Command.
    /// </summary>
    /// <typeparam name="ElementCategoryType">The Type that defines the possible sub-categories of the Elements held by this composition, eg. a ShipItem can be sub-categorized as a Frigate which is defined within the ShipCategory Type.</typeparam>
    /// <typeparam name="ElementDataType">The Type of Data associated with the ElementType this composition holds.</typeparam>
    public abstract class AComposition<ElementCategoryType, ElementDataType>
        where ElementCategoryType : struct
        where ElementDataType : AElementData<ElementCategoryType> {

        /// <summary>
        /// The Categories of the Items present in this Composition.
        /// </summary>
        public IList<ElementCategoryType> ElementCategories {
            get {
                return _composition.Keys.ToList();
            }
        }

        private IDictionary<ElementCategoryType, IList<ElementDataType>> _composition;

        /// <summary>
        /// Initializes a new instance of the <see cref="AComposition{ElementCategoryType, ElementDataType}"/> class.
        /// </summary>
        public AComposition() {
            _composition = new SortedDictionary<ElementCategoryType, IList<ElementDataType>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AComposition{ElementCategoryType, ElementDataType}"/> class.
        /// </summary>
        /// <param name="compositionToCopy">The composition automatic copy.</param>
        public AComposition(AComposition<ElementCategoryType, ElementDataType> compositionToCopy) {
            _composition = compositionToCopy._composition;
            // UNCLEAR does compositionToCopy get collected by the garbage collector now?
        }

        public bool Add(ElementDataType elementData) {
            ElementCategoryType elementCategory = elementData.ElementCategory;
            if (!_composition.ContainsKey(elementCategory)) {
                _composition.Add(elementCategory, new List<ElementDataType>());
            }
            if (_composition[elementCategory].Contains(elementData)) {
                return false;
            }
            _composition[elementCategory].Add(elementData);
            return true;
        }

        public bool Remove(ElementDataType elementData) {
            ElementCategoryType elementCategory = elementData.ElementCategory;
            bool isRemoved = _composition[elementCategory].Remove(elementData);
            if (_composition[elementCategory].Count == Constants.Zero) {
                _composition.Remove(elementCategory);
            }
            return isRemoved;
        }

        public bool Contains(ElementDataType elementData) {
            ElementCategoryType elementCategory = elementData.ElementCategory;
            return _composition[elementCategory].Contains(elementData);
        }

        public IList<ElementDataType> GetData(ElementCategoryType elementCategory) {
            return _composition[elementCategory];
        }

        public IEnumerable<ElementDataType> GetAllData() {
            IEnumerable<ElementDataType> allData = new List<ElementDataType>();
            foreach (var type in ElementCategories) {
                allData = allData.Concat(GetData(type));
            }
            return allData;
        }

    }
}

