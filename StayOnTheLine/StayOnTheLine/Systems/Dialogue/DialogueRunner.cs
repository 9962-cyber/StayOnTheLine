using System;
using System.Collections.Generic;
using System.Linq;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Systems.Dialogue
{
    /// <summary>
    /// OOP Principles: ENCAPSULATION & SEPARATION OF CONCERNS
    /// UI-free dialogue engine. Holds the node graph, a queue of lines waiting to be delivered,
    /// the delivered history (used by SAVE LOG) and the evidence flags the player earned.
    /// The form decides *when* to deliver (typing delays, animations); this class only decides
    /// *what* comes next, so the branching logic can be read and tested without any controls.
    /// </summary>
    public sealed class DialogueRunner
    {
        private readonly Dictionary<string, DialogueNode> _nodes;
        private readonly Queue<ChatMessage> _pending = new();
        private readonly List<ChatMessage> _history = new();
        private readonly HashSet<string> _flags = new(StringComparer.Ordinal);
        private readonly Func<DateTime> _clock;
        private DialogueNode? _current;

        public DialogueRunner(IEnumerable<DialogueNode> nodes, Func<DateTime> clock)
        {
            _nodes = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            // Fail fast on a typo in the script instead of dead-ending mid-scene
            foreach (var node in _nodes.Values)
                foreach (var target in node.Targets())
                    if (!_nodes.ContainsKey(target))
                        throw new InvalidOperationException($"Node '{node.Id}' points to missing node '{target}'.");
        }

        public DialogueNode? CurrentNode => _current;
        public IReadOnlyList<ChatMessage> History => _history;
        public IReadOnlyCollection<string> EvidenceFlags => _flags;

        public bool HasPendingMessages => _pending.Count > 0;
        public bool IsAwaitingChoice => !HasPendingMessages && _current is { Choices.Count: > 0 };
        public bool IsAwaitingAction => !HasPendingMessages && _current?.Action != null;
        public bool IsFinished => !HasPendingMessages && _current is { IsEnding: true };

        public void Start(string nodeId)
        {
            _pending.Clear();
            Enter(nodeId);
        }

        public ChatMessage? PeekNext() => _pending.Count > 0 ? _pending.Peek() : null;

        /// <summary>
        /// Delivers the next queued line, stamped with the terminal clock.
        /// When the node's lines run out and it has an automatic exit, the next node is entered.
        /// </summary>
        public ChatMessage? DeliverNext()
        {
            if (_pending.Count == 0) return null;

            var line = _pending.Dequeue();
            var delivered = line with { Timestamp = _clock().AddMinutes(line.TimestampSkewMinutes) };
            _history.Add(delivered);

            if (_pending.Count == 0 && _current?.NextNodeId is { } next)
                Enter(next);

            return delivered;
        }

        public void Choose(int index)
        {
            if (!IsAwaitingChoice)
                throw new InvalidOperationException("The conversation is not waiting for a choice.");
            if (index < 0 || index >= _current!.Choices.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var choice = _current.Choices[index];
            if (choice.EvidenceFlag != null) _flags.Add(choice.EvidenceFlag);
            if (choice.Reply != null) _pending.Enqueue(choice.Reply);
            Enter(choice.TargetNodeId);
        }

        public void CompleteAction()
        {
            if (!IsAwaitingAction)
                throw new InvalidOperationException("The conversation is not waiting for a player action.");

            var action = _current!.Action!;
            if (action.EvidenceFlag != null) _flags.Add(action.EvidenceFlag);
            Enter(action.TargetNodeId);
        }

        /// <summary>Adds a line that is not part of the script (e.g. a save confirmation) to the history.</summary>
        public ChatMessage Record(ChatMessage line)
        {
            var delivered = line with { Timestamp = _clock() };
            _history.Add(delivered);
            return delivered;
        }

        public void AddFlag(string flag) => _flags.Add(flag);

        private void Enter(string nodeId)
        {
            _current = _nodes[nodeId];
            foreach (var line in _current.Messages)
                _pending.Enqueue(line);

            // A node with no lines of its own just passes through
            if (_current.Messages.Count == 0 && _current.NextNodeId != null && _pending.Count == 0)
                Enter(_current.NextNodeId);
        }
    }
}
