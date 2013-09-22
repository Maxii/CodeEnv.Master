// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconFactory.cs
// Singleton. Factory that makes instances of IIcon, caches and reuses them.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Singleton. Factory that makes instances of IIcon, caches and reuses them. The reuse is critical as 
    /// the object's equality comparer (same instance in memory) is used by the client of the factory
    /// to determine which icon is currently showing.
    /// </summary>
    public class IconFactory : AGenericSingleton<IconFactory> {  // IMPROVE Without an IconFactory of each Icon type T, there can be IconSections and IconSelectionCriteria 
        // combos in the dictionary that are duplicates of that from another icon type

        private static IDictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, IIcon>> iconCache;

        private IconFactory() {
            Initialize();
        }

        protected override void Initialize() {
            iconCache = new Dictionary<IconSection, IDictionary<IEnumerable<IconSelectionCriteria>, IIcon>>();
        }

        /// <summary>
        /// Makes or retrieves an already made instance of IIcon based on the provided section and criteria. Clients should only 
        /// use this method to make IIcon as they are compared against each other to determine equality.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="section">The section.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public IIcon MakeInstance<T>(IconSection section, params IconSelectionCriteria[] criteria) where T : IIcon {
            IIcon icon;
            if (!TryCheckCache(section, out icon, criteria)) {
                icon = (T)Activator.CreateInstance(typeof(T), section, criteria);
                RecordToCache(icon, section, criteria);
            }
            return icon;
        }

        private bool TryCheckCache(IconSection section, out IIcon icon, params IconSelectionCriteria[] criteria) {
            IDictionary<IEnumerable<IconSelectionCriteria>, IIcon> criteriaCache;
            if (iconCache.TryGetValue(section, out criteriaCache)) {
                IList<IEnumerable<IconSelectionCriteria>> cacheCritieraList = criteriaCache.Keys.ToList();
                foreach (var criteriaSequenceInCache in cacheCritieraList) {
                    if (criteriaSequenceInCache.SequenceEquals(criteria, ignoreOrder: true)) {
                        IEnumerable<IconSelectionCriteria> criteriaKey = criteriaSequenceInCache;
                        icon = criteriaCache[criteriaKey];
                        return true;
                    }
                }
            }
            icon = null;
            return false;
        }

        private void RecordToCache(IIcon icon, IconSection section, params IconSelectionCriteria[] criteria) {
            if (!iconCache.ContainsKey(section)) {
                iconCache.Add(section, new Dictionary<IEnumerable<IconSelectionCriteria>, IIcon>());
            }
            IDictionary<IEnumerable<IconSelectionCriteria>, IIcon> criteriaCache = iconCache[section];
            if (!criteriaCache.ContainsKey(criteria)) {
                criteriaCache.Add(criteria, icon);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

