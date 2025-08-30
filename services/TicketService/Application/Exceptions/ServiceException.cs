using System;

namespace TicketService.Application.Exceptions;

/// <summary>
/// Exception thrown when a service operation fails.
/// </summary>
public class ServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class.
    /// </summary>
    public ServiceException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
