// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconInfoFactory.cs
//  Singleton. Abstract, generic base Factory that makes instances of IIconInfo, caches and reuses them.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
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
    /// Singleton. Abstract, generic base Factory that makes instances of IIconInfo, caches and reuses them. The reuse is critical as
    /// the object's equality comparer (same instance in memory) is used by the client of the factory to determine which icon is currently showing.
    /// </summary>
    /// <typeparam name="IconInfoType">The type of the IconInfo.</typeparam>
    /// <typeparam name="DataType">The type of CommandData.</typeparam>
    /// <typeparam name="FactoryType">The type of the Factory.</typeparam>
    public abstract class AIconInfoFactory<IconInfoType, DataType, FactoryType> : AGenericSingleton<FactoryType>
        where IconInfoType : AIconInfo
        where DataType : AUnitCmdItemData
        where FactoryType : class {

        protected abstract AtlasID AtlasID { get; }

        private IDictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, AIconInfo>> _infoCache;

        protected override void Initialize() {
            _infoCache = new Dictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, AIconInfo>>();
        }

        /// <summary>
        /// Makes an instance of IIconInfo. Clients should only use this method to make IIconInfo as they are compared against each other to determine equality.
        /// </summary>
        /// <param name="data">The item's data.</param>
        /// <param name="size">The size of the icon in pixels.</param>
        /// <param name="placement">The placement of the icon wrt the item.</param>
        /// <returns></returns>
        public IIconInfo MakeInstance(DataType data, Vector2 size, WidgetPlacement placement) {
            IconSection section = GetIconSection();
            IconSelectionCriteria[] criteria = GetSelectionCriteria(data);
            var iconInfo = MakeInstance(section, data.Owner.Color, criteria);
            iconInfo.AtlasID = AtlasID;
            iconInfo.Size = size;
            iconInfo.Placement = placement;
            return iconInfo;
        }

        private IconSelectionCriteria[] GetSelectionCriteria(DataType data) {
            IList<IconSelectionCriteria> criteria = new List<IconSelectionCriteria>();
            IntelCoverage humanPlayerIntelCoverage = data.GetUserIntelCoverage();
            //D.Log("{0} found HumanPlayerIntelCoverage = {1}.", GetType().Name, humanPlayerIntelCoverage.GetName());
            switch (humanPlayerIntelCoverage) {
                case IntelCoverage.None:
                    // always returns None
                    criteria.Add(IconSelectionCriteria.None);
                    break;
                case IntelCoverage.Basic:
                    // always returns Unknown
                    criteria.Add(IconSelectionCriteria.Unknown);
                    break;
                case IntelCoverage.Essential:
                    // always returns level 1-5
                    criteria.Add(GetCriteriaFromCategory(data));
                    break;
                case IntelCoverage.Broad:
                case IntelCoverage.Comprehensive:
                    // always returns a comprehensive icon
                    criteria.Add(GetCriteriaFromCategory(data));
                    GetCriteriaFromComposition(data).ForAll(isc => criteria.Add(isc));
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(humanPlayerIntelCoverage));
            }
            return criteria.ToArray();
        }

        protected abstract IconSelectionCriteria GetCriteriaFromCategory(DataType data);

        protected abstract IEnumerable<IconSelectionCriteria> GetCriteriaFromComposition(DataType data);

        protected virtual IconSection GetIconSection() {
            return IconSection.Base;
        }

        /// <summary>
        /// Makes or retrieves an already made instance of AIconInfo based on the provided section and criteria. 
        /// These instances hold the filename and color of the icon. The AtlasID, placement and size of the icon will be added later. 
        /// Clients should only use this method to make IIconInfo as they are compared against each other to determine equality.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        private AIconInfo MakeInstance(IconSection section, GameColor color, params IconSelectionCriteria[] criteria) {
            AIconInfo info;
            if (!TryCheckCache(section, out info, criteria)) {
                info = (IconInfoType)Activator.CreateInstance(typeof(IconInfoType), section, criteria);
                RecordToCache(info, section, criteria);
            }
            (info as IconInfoType).Color = color;
            return info;
        }

        private bool TryCheckCache(IconSection section, out AIconInfo info, params IconSelectionCriteria[] criteria) {
            IDictionary<IEnumerable<IconSelectionCriteria>, AIconInfo> criteriaCache;
            if (_infoCache.TryGetValue(section, out criteriaCache)) {
                IList<IEnumerable<IconSelectionCriteria>> cacheCritieraList = criteriaCache.Keys.ToList();
                foreach (var criteriaSequenceInCache in cacheCritieraList) {
                    // Note: cannot use SequenceEquals as enums don't implement IComparable<T>, just IComparable
                    bool criteriaIsEqual = criteriaSequenceInCache.OrderBy(c => c).SequenceEqual(criteria.OrderBy(c => c));
                    if (criteriaIsEqual) {
                        IEnumerable<IconSelectionCriteria> criteriaKey = criteriaSequenceInCache;
                        info = criteriaCache[criteriaKey];
                        D.Log("A {0} has been reused from cache.", typeof(IconInfoType).Name);
                        return true;
                    }
                }
            }
            info = null;
            return false;
        }

        private void RecordToCache(AIconInfo info, IconSection section, params IconSelectionCriteria[] criteria) {
            if (!_infoCache.ContainsKey(section)) {
                _infoCache.Add(section, new Dictionary<IEnumerable<IconSelectionCriteria>, AIconInfo>());
            }
            IDictionary<IEnumerable<IconSelectionCriteria>, AIconInfo> criteriaCache = _infoCache[section];
            if (!criteriaCache.ContainsKey(criteria)) {
                criteriaCache.Add(criteria, info);
            }
        }

    }
}


