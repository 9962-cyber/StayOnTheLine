namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: INHERITANCE and ABSTRACTION
    /// Abstract base class providing common state and behavior for all loading steps.
    /// Derived classes inherit these members and can override polymorphic hooks.
    /// </summary>
    public abstract class LoadingStepBase : ILoadingStep
    {
        // OOP Principle: ENCAPSULATION (private backing fields with controlled access)
        private float _stepProgress;

        public string StepName { get; }
        public string StatusMessage { get; protected set; }
        public float DurationWeight { get; }

        public float StepProgress
        {
            get => _stepProgress;
            private set => _stepProgress = Math.Clamp(value, 0f, 100f);
        }

        public bool IsCompleted => StepProgress >= 100f;

        protected LoadingStepBase(string stepName, string statusMessage, float durationWeight = 1.0f)
        {
            StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
            StatusMessage = statusMessage ?? throw new ArgumentNullException(nameof(statusMessage));
            DurationWeight = Math.Max(0.1f, durationWeight);
            StepProgress = 0f;
        }

        /// <summary>
        /// Advances step progress by a specified increment.
        /// </summary>
        public virtual void Advance(float amount)
        {
            if (IsCompleted) return;

            StepProgress += amount;
            if (IsCompleted)
            {
                OnStepCompleted();
            }
        }

        /// <summary>
        /// Instantly completes this step.
        /// </summary>
        public virtual void Complete()
        {
            StepProgress = 100f;
            OnStepCompleted();
        }

        /// <summary>
        /// Resets the step to initial state.
        /// </summary>
        public virtual void Reset()
        {
            StepProgress = 0f;
        }

        /// <summary>
        /// OOP Principle: POLYMORPHISM
        /// Virtual/abstract lifecycle hook overridden by specific derived steps
        /// to execute step-specific simulation or initialization behavior.
        /// </summary>
        protected virtual void OnStepCompleted()
        {
            // Base implementation: optional hook for subclasses
        }
    }
}
