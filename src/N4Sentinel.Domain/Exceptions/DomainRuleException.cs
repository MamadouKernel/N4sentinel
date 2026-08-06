namespace N4Sentinel.Domain.Exceptions;

/// <summary>
/// Raised when an operation would violate an invariant of the N4 Sentinel domain
/// (e.g. an invalid environment status transition, or an invalid component dependency).
/// </summary>
public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string message) : base(message)
    {
    }
}
