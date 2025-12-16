// <copyright file="ConnectionClosedException.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPClient;

/// <summary>
/// The exception that is thrown when a connection to the server is unexpectedly closed
/// by the remote host.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates that the remote server has terminated the connection,
/// typically due to network issues, server restart, or explicit connection termination
/// by the server.
/// </para>
/// <para>
/// This is different from normal operation cancellation and indicates an abnormal
/// termination of the connection.
/// </para>
/// <para>
/// Inherits from <see cref="IOException"/> to indicate it's an I/O related error.
/// </para>
/// </remarks>
public class ConnectionClosedException : IOException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionClosedException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConnectionClosedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionClosedException"/> class
    /// with a specified error message and a reference to the inner exception that is
    /// the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ConnectionClosedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
