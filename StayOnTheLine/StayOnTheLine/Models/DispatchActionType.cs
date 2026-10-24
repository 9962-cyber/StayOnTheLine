namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Defines the 6 standardized dispatch terminal operations available to operators.
    /// </summary>
    public enum DispatchActionType
    {
        Ask,   // Ask for details / location
        Log,   // Check incident log / records
        Trace, // Trace signal
        Send,  // Dispatch emergency unit
        Hold,  // Place on hold
        Flag   // Mark evidence / flag discrepancy
    }

    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Represents the queue state of an incoming emergency trunk line.
    /// </summary>
    public enum CallQueueStatus
    {
        Live,
        Waiting,
        OnHold,
        Resolved,
        Restricted
    }

    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Represents the sequential work stages of an active emergency call.
    /// </summary>
    public enum CallStage
    {
        Ringing,
        Inquiry,
        LogInspection,
        DiscrepancyChoice,
        Resolution,
        Resolved
    }
}
