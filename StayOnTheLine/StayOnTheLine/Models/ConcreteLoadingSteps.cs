namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: INHERITANCE and POLYMORPHISM
    /// Concrete subclasses inheriting from LoadingStepBase, each representing
    /// a specialized night-dispatch subsystem being initialized.
    /// </summary>
    public class FrequencyInitializationStep : LoadingStepBase
    {
        public FrequencyInitializationStep()
            : base("FrequencyInit", "ESTABLISHING EMERGENCY DISPATCH FREQUENCY...", 1.2f)
        {
        }

        protected override void OnStepCompleted()
        {
            StatusMessage = "FREQUENCY 911-A SYNCHRONIZED.";
        }
    }

    public class TrunkLineConnectionStep : LoadingStepBase
    {
        public TrunkLineConnectionStep()
            : base("TrunkLine", "CONNECTING COUNTY TELECOM TRUNK LINES...", 1.4f)
        {
        }

        protected override void OnStepCompleted()
        {
            StatusMessage = "TRUNK LINES ONLINE.";
        }
    }

    public class IncidentLogDatabaseStep : LoadingStepBase
    {
        public IncidentLogDatabaseStep()
            : base("IncidentLogs", "LOADING NIGHT SHIFT INCIDENT LOGS...", 1.5f)
        {
        }

        protected override void OnStepCompleted()
        {
            StatusMessage = "INCIDENT ARCHIVES READY.";
        }
    }

    public class AudioChannelCalibrationStep : LoadingStepBase
    {
        public AudioChannelCalibrationStep()
            : base("AudioCalibration", "CALIBRATING AUDIO TRANSCRIPTION CHANNELS...", 1.3f)
        {
        }

        protected override void OnStepCompleted()
        {
            StatusMessage = "TRANSCRIPTION FILTERS ACTIVE.";
        }
    }

    public class SupervisorMemoVerificationStep : LoadingStepBase
    {
        public SupervisorMemoVerificationStep()
            : base("SupervisorMemos", "VERIFYING SUPERVISOR MEMOS AND UNITS...", 1.1f)
        {
        }

        protected override void OnStepCompleted()
        {
            StatusMessage = "UNITS ALLOCATED: 2 AVAILABLE.";
        }
    }

    public class SystemReadyStep : LoadingStepBase
    {
        public SystemReadyStep()
            : base("SystemReady", "FINALIZING DISPATCH TERMINAL... OPERATOR LOGGED IN.", 0.8f)
        {
        }

        protected override void OnStepCompleted()
        {
            StatusMessage = "SYSTEM ONLINE. ALL LINES ACTIVE.";
        }
    }
}
