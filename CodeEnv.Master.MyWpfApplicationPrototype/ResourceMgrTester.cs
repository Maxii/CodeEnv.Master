// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceMgrTester.cs
// TODO - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.MyWpfApplicationPrototype {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Resources;
    using CodeEnv.Master.Common.ResourceMgmt;

    /// <summary>
    /// TODO 
    /// </summary>
    public class ResourceMgrTester {

        public ResourceMgrTester() {
            ResourceManager errorStringsResourceMgr = ResourceMgrFactory.GetManager(ResourceMgrFactory.ResourceFileName.ErrorStrings);
            string myEmptyStringErrorText = errorStringsResourceMgr.GetString("EMPTY_STRING");
            Console.WriteLine(myEmptyStringErrorText);
        }

    }
}

