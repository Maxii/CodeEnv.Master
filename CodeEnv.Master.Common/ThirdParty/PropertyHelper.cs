// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PropertyHelper.cs
// Helper class for Properties.
// </summary> 
// <remarks> Courtesy of Marc Gravell </remarks>
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Helper class for Properties.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class PropertyHelper<T> {

        /// <summary>
        /// Gets a PropertyInfo from the lambda expression that refers to the class and property.
        /// Usage: PropertyInfo fooBarPropertyInfo = PropertyHelper&lt;Foo&gt;.GetPropertyInfo&lt;BarType&gt;(foo => foo.Bar);
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="selector">The selector in the form of a LambdaExpression.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static PropertyInfo GetPropertyInfo<TValue>(Expression<Func<T, TValue>> selector) {
            Expression body = selector;
            if (body is LambdaExpression) {
                body = ((LambdaExpression)body).Body;
            }
            switch (body.NodeType) {
                case ExpressionType.MemberAccess:
                    return (PropertyInfo)((MemberExpression)body).Member;
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Gets the name of a property from the lambda expression that refers to the class and property.
        /// Usage: string fooBarPropertyName = PropertyHelper&lt;Foo&gt;.GetPropertyName&lt;BarType&gt;(foo => foo.Bar);
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static string GetPropertyName<TValue>(Expression<Func<T, TValue>> selector) {
            return GetPropertyInfo<TValue>(selector).Name;
        }
    }
}

