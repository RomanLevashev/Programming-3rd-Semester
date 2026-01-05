// <copyright file="BeforeClassAttribute.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Marks a static method to be executed once before any tests in the containing class run.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BeforeClassAttribute : Attribute
{
}
