// <copyright file="IncompleteDataException.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPClient;

/// <summary>
/// The exception that is thrown when a data transfer operation is interrupted
/// before all expected data has been received.
/// </summary>
/// <remarks>
/// <para>
/// This exception is typically thrown when a network connection is closed
/// prematurely during file download or when a response from the server
/// is incomplete.
/// </para>
/// <para>
/// Inherits from <see cref="IOException"/> to indicate it's an I/O related error.
/// </para>
/// </remarks>
public class IncompleteDataException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncompleteDataException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public IncompleteDataException(string message) : base(message) { }
}