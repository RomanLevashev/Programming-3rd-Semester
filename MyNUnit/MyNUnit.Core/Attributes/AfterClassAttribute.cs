// <copyright file="AfterClassAttribute.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Marks a static method to be executed once after all tests in the containing class finish.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterClassAttribute : Attribute
{
}
