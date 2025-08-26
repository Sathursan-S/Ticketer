using System;

namespace TicketService.Services;

/// <summary>
/// Custom exception for service layer errors
/// </summary>
public class ServiceException : Exception
{
    public ServiceException(string message) : base(message)
    {
    }

    public ServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
