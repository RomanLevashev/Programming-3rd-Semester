// <copyright file="AfterAttribute.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MyNUnit.Core;

/// <summary>
/// Marks a method to be executed after each test in the containing class.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AfterAttribute : Attribute
{
}
