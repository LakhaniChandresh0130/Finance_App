namespace Finance.Application.Abstractions;

/// <summary>Raised when a row was updated or deleted under another session (optimistic concurrency).</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() : base("The record was modified concurrently.")
    {
    }

    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}
