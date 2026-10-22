using StayOnTheLine.Models;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION and POLYMORPHISM
    /// Encapsulates the complete loading workflow, orchestrating multiple
    /// polymorphic ILoadingStep instances and calculating overall progress.
    /// </summary>
    public class LoadingManager
    {
        private readonly List<ILoadingStep> _steps = new();
        private readonly Random _random = new();
        private int _currentStepIndex = 0;
        private bool _isCompleted = false;

        // OOP Principle: ENCAPSULATION (public getters, private setters)
        public float OverallProgress { get; private set; }
        public string CurrentStatus { get; private set; } = string.Empty;
        public bool IsCompleted => _isCompleted;
        public IReadOnlyList<ILoadingStep> Steps => _steps.AsReadOnly();

        public event EventHandler<LoadingProgressEventArgs>? ProgressChanged;
        public event EventHandler? LoadingCompleted;

        public LoadingManager()
        {
            RegisterDefaultSteps();
            RecalculateProgress();
        }

        /// <summary>
        /// Registers the sequence of loading operations (Polymorphism: collection of ILoadingStep).
        /// </summary>
        private void RegisterDefaultSteps()
        {
            _steps.Add(new FrequencyInitializationStep());
            _steps.Add(new TrunkLineConnectionStep());
            _steps.Add(new IncidentLogDatabaseStep());
            _steps.Add(new AudioChannelCalibrationStep());
            _steps.Add(new SupervisorMemoVerificationStep());
            _steps.Add(new SystemReadyStep());
        }

        /// <summary>
        /// Advances the loading simulation by one tick, simulating realistic atmospheric delays.
        /// </summary>
        public void Tick()
        {
            if (_isCompleted || _steps.Count == 0) return;

            if (_currentStepIndex < _steps.Count)
            {
                var activeStep = _steps[_currentStepIndex];

                // Variable step progression speed with psychological horror slowdowns
                float stepIncrement = (float)(_random.NextDouble() * 3.5 + 1.2);

                // Eerie slight hesitation mid-way (step 2 database or step 4 audio)
                if (_currentStepIndex == 2 && activeStep.StepProgress is >= 40f and <= 55f)
                {
                    stepIncrement = (float)(_random.NextDouble() * 0.8 + 0.2);
                }

                activeStep.Advance(stepIncrement);

                if (activeStep.IsCompleted)
                {
                    _currentStepIndex++;
                }
            }

            RecalculateProgress();

            if (_currentStepIndex >= _steps.Count && OverallProgress >= 100f && !_isCompleted)
            {
                _isCompleted = true;
                LoadingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Instantly accelerates remaining steps if player clicks or presses a key during loading.
        /// </summary>
        public void FastForward()
        {
            if (_isCompleted) return;

            foreach (var step in _steps)
            {
                step.Complete();
            }

            _currentStepIndex = _steps.Count;
            RecalculateProgress();

            if (!_isCompleted)
            {
                _isCompleted = true;
                LoadingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Recalculates weighted total progress across all steps.
        /// </summary>
        private void RecalculateProgress()
        {
            if (_steps.Count == 0)
            {
                OverallProgress = 100f;
                return;
            }

            float totalWeight = 0f;
            float completedWeight = 0f;

            foreach (var step in _steps)
            {
                totalWeight += step.DurationWeight;
                completedWeight += (step.StepProgress / 100f) * step.DurationWeight;
            }

            OverallProgress = totalWeight > 0f ? (completedWeight / totalWeight) * 100f : 100f;
            OverallProgress = Math.Clamp(OverallProgress, 0f, 100f);

            // Determine active status message
            if (_currentStepIndex < _steps.Count)
            {
                CurrentStatus = _steps[_currentStepIndex].StatusMessage;
            }
            else
            {
                CurrentStatus = "SYSTEM ONLINE. ALL LINES ACTIVE.";
            }

            string activeName = _currentStepIndex < _steps.Count ? _steps[_currentStepIndex].StepName : "Complete";
            ProgressChanged?.Invoke(this, new LoadingProgressEventArgs(OverallProgress, CurrentStatus, activeName, _isCompleted));
        }

        public void Reset()
        {
            _currentStepIndex = 0;
            _isCompleted = false;
            foreach (var step in _steps)
            {
                step.Reset();
            }
            RecalculateProgress();
        }
    }
}
