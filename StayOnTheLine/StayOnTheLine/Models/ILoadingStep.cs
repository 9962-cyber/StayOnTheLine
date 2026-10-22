namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ABSTRACTION
    /// Defines the contract for an individual loading operation without exposing
    /// internal execution details to the UI layer.
    /// </summary>
    public interface ILoadingStep
    {
        /// <summary>
        /// Unique identifier/name of the loading step.
        /// </summary>
        string StepName { get; }

        /// <summary>
        /// Atmospheric horror status message displayed to the player.
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// Relative weight of this step in calculating total progress.
        /// </summary>
        float DurationWeight { get; }

        /// <summary>
        /// Current completion progress of this individual step (0 - 100).
        /// </summary>
        float StepProgress { get; }

        /// <summary>
        /// Whether this step has finished executing.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Advances the progress of this step by a specified delta.
        /// </summary>
        void Advance(float amount);

        /// <summary>
        /// Instantly completes this step.
        /// </summary>
        void Complete();

        /// <summary>
        /// Resets the step to its initial state.
        /// </summary>
        void Reset();
    }
}
