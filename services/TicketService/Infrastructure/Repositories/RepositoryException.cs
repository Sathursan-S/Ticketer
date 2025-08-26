using System;

namespace TicketService.Repository;

/// <summary>
/// Custom exception for repository layer errors
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message)
    {
    }

    public RepositoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
