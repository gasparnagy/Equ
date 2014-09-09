﻿namespace Equ
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides an implementation of <see cref="IEqualityComparer{T}"/>that performs memberwise
    /// equality comparison of objects of type T. Use the <see cref="ByFields"/> or <see cref="ByProperties"/>
    /// static instances to get one of the two comparison strategies that are supported by default. Note that both
    /// honor <see cref="MemberwiseEqualityIgnoreAttribute"/>.
    /// 
    /// For more advanced scenarios, use the <see cref="Custom"/> creator and pass a <see cref="EqualityFunctionGenerator"/>
    /// that matches your requirements.
    /// </summary>
    public class MemberwiseEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<object, object, bool> _equalsFunc;

        private readonly Func<object, int> _getHashCodeFunc;

        private static readonly Lazy<MemberwiseEqualityComparer<T>> _fieldsComparer =
            new Lazy<MemberwiseEqualityComparer<T>>(
                () =>
                    new MemberwiseEqualityComparer<T>(
                        new EqualityFunctionGenerator(
                            typeof(T),
                            AllFieldsExceptIgnored,
                            t => new List<PropertyInfo>())));

        private static readonly Lazy<MemberwiseEqualityComparer<T>> _propertiesComparer =
            new Lazy<MemberwiseEqualityComparer<T>>(
                () =>
                    new MemberwiseEqualityComparer<T>(
                        new EqualityFunctionGenerator(
                            typeof(T),
                            t => new List<FieldInfo>(),
                            AllPropertiesExceptIgnored)));

        public static MemberwiseEqualityComparer<T> ByFields { get { return _fieldsComparer.Value; } }

        public static MemberwiseEqualityComparer<T> ByProperties { get { return _propertiesComparer.Value; } }

        public static MemberwiseEqualityComparer<T> Custom(EqualityFunctionGenerator equalityFunctionGenerator)
        {
            return new MemberwiseEqualityComparer<T>(equalityFunctionGenerator);
        } 

        private MemberwiseEqualityComparer(EqualityFunctionGenerator equalityFunctionGenerator)
        {
            _equalsFunc = equalityFunctionGenerator.MakeEqualsMethod();
            _getHashCodeFunc = equalityFunctionGenerator.MakeGetHashCodeMethod();
        }

        private static IEnumerable<FieldInfo> AllFieldsExceptIgnored(Type t)
        {
            return t.GetFields(AllInstanceMembers).Where(IsNotMarkedAsIgnore);
        }

        private static IEnumerable<PropertyInfo> AllPropertiesExceptIgnored(Type t)
        {
            return t.GetProperties(AllInstanceMembers).Where(IsNotMarkedAsIgnore);
        }

        private static BindingFlags AllInstanceMembers
        {
            get { return BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic; }
        }

        private static bool IsNotMarkedAsIgnore(MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttributes(typeof(MemberwiseEqualityIgnoreAttribute), true).Length == 0;
        }

        /// <summary>
        /// This method delegates to the generated equality function. Note that first a reference check 
        /// on <paramref name="x"/> and <paramref name="y"/> (reference equality and null-check) is performed.
        /// </summary>
        /// <returns>True if <paramref name="x"/> and <paramref name="y"/> are memberwise equal.</returns>
        public bool Equals(T x, T y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (ReferenceEquals(null, x) || ReferenceEquals(null, y))
            {
                return false;
            }
            return _equalsFunc(x, y);
        }

        /// <summary>
        /// This method delegates to the generated hash code function. If <paramref name="obj"/> is null,
        /// 0 is returned.
        /// </summary>
        /// <returns>The hash code for <paramref name="obj"/></returns>
        public int GetHashCode(T obj)
        {
            return ReferenceEquals(null, obj) ? 0 : _getHashCodeFunc(obj);
        }
    }
}
