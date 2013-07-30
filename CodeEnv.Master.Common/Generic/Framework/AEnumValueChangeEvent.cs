// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEnumValueChangeEvent.cs
// Abstract helper GameEvent class that makes it easier to create events that simply announce
// the change in value of an enum.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Abstract helper GameEvent class that makes it easier to create events that simply announce
    /// the change in value of an enum.
    /// </summary>
    /// <typeparam name="EnumType">The type of the Enum value being changed.</typeparam>
    public abstract class AEnumValueChangeEvent<EnumType> : AGameEvent where EnumType : struct {

        public EnumType NewValue { get; private set; }

        public AEnumValueChangeEvent(object source, EnumType newValue)
            : base(source) {
            NewValue = newValue;
        }
    }
}

