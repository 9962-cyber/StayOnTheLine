using System;
using System.Collections.Generic;

namespace StayOnTheLine.Models.Dialogue
{
    /// <summary>
    /// OOP Principles: ENCAPSULATION & COMPOSITION
    /// A beat of the conversation: the lines delivered when it is entered, followed by exactly
    /// one way forward - a set of choices, a required player action, an automatic jump to the
    /// next node, or nothing (the thread ends). Built with a small fluent API so the script in
    /// <c>StefConversation</c> reads like the flowchart.
    /// </summary>
    public sealed class DialogueNode
    {
        public const string DefaultPrompt = "SELECT A RESPONSE BEFORE SENDING";

        private readonly List<ChatMessage> _messages;
        private readonly List<DialogueChoice> _choices = new();

        public string Id { get; }
        public IReadOnlyList<ChatMessage> Messages => _messages;
        public IReadOnlyList<DialogueChoice> Choices => _choices;
        public string? NextNodeId { get; private set; }
        public PlayerAction? Action { get; private set; }
        public string ChoicePrompt { get; private set; } = DefaultPrompt;

        /// <summary>
        /// True when the choices are spoken out loud to someone at the desk (shown under their
        /// dialogue box) rather than typed into the messenger.
        /// </summary>
        public bool ChoicesInRoom { get; private set; }

        public bool IsEnding => NextNodeId == null && Action == null && _choices.Count == 0;

        public DialogueNode(string id, params ChatMessage[] messages)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Node id is required.", nameof(id));
            Id = id;
            _messages = new List<ChatMessage>(messages);
        }

        public DialogueNode Choice(string text, string targetNodeId, ChatMessage? reply = null, string? evidenceFlag = null)
        {
            EnsureNoExit(allowChoices: true);
            _choices.Add(new DialogueChoice(text, targetNodeId) { Reply = reply, EvidenceFlag = evidenceFlag });
            return this;
        }

        public DialogueNode Then(string nextNodeId)
        {
            EnsureNoExit(allowChoices: false);
            NextNodeId = nextNodeId;
            return this;
        }

        public DialogueNode Requires(PlayerAction action)
        {
            EnsureNoExit(allowChoices: false);
            Action = action ?? throw new ArgumentNullException(nameof(action));
            return this;
        }

        /// <summary>Header shown above the choices (defaults to the messenger's prompt).</summary>
        public DialogueNode Prompt(string prompt)
        {
            ChoicePrompt = prompt;
            return this;
        }

        /// <summary>The choices are answered out loud in the room, not in the messenger.</summary>
        public DialogueNode InRoom()
        {
            ChoicesInRoom = true;
            return this;
        }

        /// <summary>Every node id this node can lead to (used to validate the graph).</summary>
        public IEnumerable<string> Targets()
        {
            foreach (var c in _choices) yield return c.TargetNodeId;
            if (NextNodeId != null) yield return NextNodeId;
            if (Action != null) yield return Action.TargetNodeId;
        }

        private void EnsureNoExit(bool allowChoices)
        {
            bool hasOther = NextNodeId != null || Action != null || (!allowChoices && _choices.Count > 0);
            if (hasOther)
                throw new InvalidOperationException($"Node '{Id}' can only have one kind of exit (choices, action or next).");
        }
    }
}
