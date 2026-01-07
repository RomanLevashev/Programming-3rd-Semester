// <copyright file="AttributeExtensions.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

using System.Reflection;

/// <summary>
/// Extension helpers for working with custom attributes.
/// </summary>
internal static class AttributeExtensions
{
    /// <summary>
    /// Determines whether the specified member is decorated with an attribute of the given type.
    /// </summary>
    /// <typeparam name="T">The attribute type to look for.</typeparam>
    /// <param name="member">The member to inspect.</param>
    /// <returns><c>true</c> if the attribute is present; otherwise, <c>false</c>.</returns>
    public static bool HasAttribute<T>(this MemberInfo member)
    where T : Attribute =>
        member.GetCustomAttribute<T>() != null;
}
