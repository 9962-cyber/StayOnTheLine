namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates event data related to loading progress changes,
    /// protecting internal fields and exposing them via read-only properties.
    /// </summary>
    public class LoadingProgressEventArgs : EventArgs
    {
        public float OverallProgress { get; }
        public string StatusMessage { get; }
        public string ActiveStepName { get; }
        public bool IsCompleted { get; }

        public LoadingProgressEventArgs(float overallProgress, string statusMessage, string activeStepName, bool isCompleted)
        {
            OverallProgress = Math.Clamp(overallProgress, 0f, 100f);
            StatusMessage = statusMessage ?? string.Empty;
            ActiveStepName = activeStepName ?? string.Empty;
            IsCompleted = isCompleted;
        }
    }
}
