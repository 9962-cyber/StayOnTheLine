using System;

namespace StayOnTheLine.Models
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates the complete state, dialogue tree, telemetry,
    /// and action responses of an emergency dispatch trunk line.
    /// </summary>
    public class EmergencyCall
    {
        public int SlotNumber { get; set; } = 1;
        public string CallId { get; set; } = "999-0000";
        public string CallerName { get; set; } = "UNKNOWN_CALLER";
        public string Location { get; set; } = "UNKNOWN LOCATION";
        public string InitialDialogue { get; set; } = string.Empty;
        public string SignalStatus { get; set; } = "STABLE";
        public int TrustPercentage { get; set; } = 80;
        public CallQueueStatus Status { get; set; } = CallQueueStatus.Waiting;

        // Action Responses (shown in CRT terminal)
        public string AskResponse { get; set; } = string.Empty;
        public string LogResponse { get; set; } = string.Empty;
        public string TraceResponse { get; set; } = string.Empty;
        public string SendResponse { get; set; } = string.Empty;
        public string HoldResponse { get; set; } = string.Empty;
        public string FlagResponse { get; set; } = string.Empty;

        // Supervisor Guidance (Krisha Narciso in Shift 1)
        public string SupervisorHint { get; set; } = string.Empty;
        public string SupervisorAsk { get; set; } = string.Empty;
        public string SupervisorLog { get; set; } = string.Empty;
        public string SupervisorTrace { get; set; } = string.Empty;
        public string SupervisorSend { get; set; } = string.Empty;
        public string SupervisorHold { get; set; } = string.Empty;
        public string SupervisorFlag { get; set; } = string.Empty;

        // Game Logic State
        public CallStage Stage { get; set; } = CallStage.Ringing;
        public bool RequiresUnit { get; set; } = false;
        public bool HasDiscrepancy { get; set; } = false;
        public bool DiscrepancyFlagged { get; set; } = false;
        public bool UnitDispatched { get; set; } = false;
        public bool AddressVerified { get; set; } = false;
        public bool IsRestricted { get; set; } = false;

        public EmergencyCall()
        {
        }

        public string GetActionResponse(DispatchActionType action) => action switch
        {
            DispatchActionType.Ask => AskResponse,
            DispatchActionType.Log => LogResponse,
            DispatchActionType.Trace => TraceResponse,
            DispatchActionType.Send => SendResponse,
            DispatchActionType.Hold => HoldResponse,
            DispatchActionType.Flag => FlagResponse,
            _ => string.Empty
        };

        public string GetSupervisorResponse(DispatchActionType action) => action switch
        {
            DispatchActionType.Ask => SupervisorAsk,
            DispatchActionType.Log => SupervisorLog,
            DispatchActionType.Trace => SupervisorTrace,
            DispatchActionType.Send => SupervisorSend,
            DispatchActionType.Hold => SupervisorHold,
            DispatchActionType.Flag => SupervisorFlag,
            _ => SupervisorHint
        };
    }
}
