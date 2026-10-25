using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using StayOnTheLine.Controls;
using StayOnTheLine.Models;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// Chloe's calls (Shift 1, 2 and 4) play as a phone-call scene instead of on the terminal:
    /// her room, Chloe on the phone, the dialogue box and the choices as buttons beneath it.
    /// The call scripts are untouched — whatever the call would print (<see cref="StartTypewriter"/>)
    /// and offer (<see cref="SetDynamicActionChoices"/>) is mirrored into the scene, and a clicked
    /// button runs the same <see cref="ActionChoice.OnClick"/>. The terminal transcript still fills
    /// in behind it, so the call log is the same either way.
    /// </summary>
    public partial class ShiftPlayForm
    {
        private const string CallerTag = "INCOMING / VOICE";
        private const string DeskTag = "AT YOUR DESK";

        // Where each line takes place: Chloe on her end of the line, or someone at your desk
        private Image? _callerRoomBg;
        private Image? _deskBg;

        private PhoneCallScene? _callScene;
        private readonly Dictionary<string, Image?> _callSprites = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Regex SpeakerLine = new(@"^([A-Z][A-Z0-9 .'\-]{1,30}):\s*(.+)$");

        private static bool IsPhoneSceneCall(EmergencyCall call) =>
            !call.IsRestricted && call.CallerName.StartsWith("CHLOE", StringComparison.OrdinalIgnoreCase);

        /// <summary>True when this response belongs in the call scene rather than on the terminal.</summary>
        private bool ShouldRouteToCallScene()
        {
            var call = CurrentCall;
            if (!IsPhoneSceneCall(call)) return false;
            if (_callScene is { Active: true }) return true;
            return call.Stage != CallStage.Ringing && call.Stage != CallStage.Resolved;
        }

        private PhoneCallScene EnsureCallScene()
        {
            if (_callScene != null) return _callScene;
            _callerRoomBg = TryLoadImage("chloe_room_bg.png") ?? TryLoadImage("tutorialdialouge_bg.png") ?? _imgDeskRoom;
            _deskBg = TryLoadImage("dispatch_desk_bg.png") ?? _imgDeskRoom;
            _callScene = new PhoneCallScene { SceneBackground = _callerRoomBg };
            _callScene.ChoiceSelected += (s, index) =>
            {
                if (index >= 0 && index < _currentChoices.Count) _currentChoices[index].OnClick();
            };
            _callScene.Ended += (s, e) => ApplyResponsiveLayout();
            Controls.Add(_callScene);
            LayoutCallScene();
            Resize += (s, e) => LayoutCallScene();
            return _callScene;
        }

        private void LayoutCallScene()
        {
            if (_callScene == null) return;
            _callScene.Bounds = ClientRectangle;
            if (_callScene.Active) _callScene.BringToFront();
        }

        /// <summary>Feeds a call response to the scene, and to the terminal log behind it.</summary>
        private void PlayInCallScene(string text)
        {
            var scene = EnsureCallScene();
            LayoutCallScene();

            // Terminal log behind the scene, without typing or Charm's pop-up
            _targetDialogue = _displayedDialogue = text;
            _typewriterCharIndex = text.Length;
            _lblCallTranscript.Text = text;

            scene.PlayLines(ToCallSceneLines(text));
            scene.BringToFront();
        }

        /// <summary>Mirrors the call's current choices into the scene (or closes it when the call is over).</summary>
        private void SyncCallSceneChoices()
        {
            if (_callScene is not { Active: true }) return;
            var call = CurrentCall;
            if (!IsPhoneSceneCall(call))
            {
                _callScene.Close();
                return;
            }
            if (call.Stage == CallStage.Resolved)
            {
                _callScene.EndAfterLines();
                return;
            }
            _callScene.SetChoices(_currentChoices
                .Select(c => (StripIcon(c.Caption), c.IsRecommended && Scenario.HasTutorialGuidance))
                .ToList());
        }

        /// <summary>
        /// The caller's name only appears once you've identified her (asked her name);
        /// until then the box says CALLER, like the terminal's "UNIDENTIFIED CALLER".
        /// </summary>
        private string CallerLabel => CurrentCall.IdentityVerified ? "CHLOE" : "CALLER";

        private static bool IsCallerSpeaker(string speaker) =>
            speaker.StartsWith("CHLOE", StringComparison.OrdinalIgnoreCase) ||
            speaker.StartsWith("CALLER", StringComparison.OrdinalIgnoreCase);

        private static readonly HashSet<string> DeskSpeakers = new(StringComparer.OrdinalIgnoreCase)
            { "YOU", "KRISHA", "CHARM", "CHARMEE", "YSA", "STEFF", "SYSTEM" };

        /// <summary>Terminal readouts are written in capitals ("CRITICAL SYSTEM DISCREPANCY DETECTED").</summary>
        private static bool IsTerminalReadout(string line) =>
            line.Any(char.IsLetter) && !line.Any(char.IsLower);

        private IEnumerable<CallSceneLine> ToCallSceneLines(string text)
        {
            foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                var m = SpeakerLine.Match(line);
                string label = m.Success ? m.Groups[1].Value.Trim() : string.Empty;
                bool knownSpeaker = m.Success && (IsCallerSpeaker(label) || DeskSpeakers.Contains(label) ||
                                                  label.Equals(Profile.OperatorName, StringComparison.OrdinalIgnoreCase));

                // Capitalised lines that aren't someone talking are the terminal printing: nobody on screen
                if (!knownSpeaker && !line.StartsWith("[") && IsTerminalReadout(line))
                {
                    yield return new CallSceneLine(string.Empty, line, null, "TERMINAL", ClearPortrait: true);
                    continue;
                }
                if (!knownSpeaker) m = Match.Empty;

                if (!m.Success)
                {
                    // [stage directions] are narration; any other unlabelled line is the caller talking
                    // (Shift 1 writes Chloe's lines without a "CHLOE:" prefix)
                    bool stage = line.StartsWith("[") || line.StartsWith("(");
                    string said0 = line.Trim('"', '“', '”');
                    yield return stage
                        ? new CallSceneLine(string.Empty, line, DeskPersonIn(line), string.Empty, DeskPersonIn(line) != null ? _deskBg : null)
                        : new CallSceneLine(CallerLabel, said0, ChloePortraitFor(said0), CallerTag, _callerRoomBg);
                    continue;
                }

                string speaker = m.Groups[1].Value.Trim();
                string said = m.Groups[2].Value.Trim().Trim('"');
                if (speaker == "SYSTEM")
                {
                    // Terminal output: nobody is speaking, so the caller leaves the screen
                    yield return new CallSceneLine(string.Empty, "SYSTEM: " + said, null, "TERMINAL", ClearPortrait: true);
                    continue;
                }

                bool caller = IsCallerSpeaker(speaker);
                var deskPortrait = caller ? null : DeskPortraitFor(speaker);
                yield return new CallSceneLine(
                    caller ? CallerLabel : speaker == "CHARMEE" ? "CHARM" : speaker,
                    said,
                    caller ? ChloePortraitFor(said) : deskPortrait,
                    caller ? CallerTag : deskPortrait != null ? DeskTag : string.Empty,
                    caller ? _callerRoomBg : deskPortrait != null ? _deskBg : null);
            }
        }

        /// <summary>Chloe on the phone; her expression follows what she's saying.</summary>
        private Image? ChloePortraitFor(string said)
        {
            string l = said.ToLowerInvariant();
            string mood =
                l.Contains("please") || l.Contains("help") || l.Contains("hear") || l.Contains("door") ||
                l.Contains("happening") || l.Contains("danger") || l.Contains("near") ? "distressed" :
                l.Contains("nobody") || l.Contains("never") || l.Contains("waiting") || l.Contains("disappeared") ||
                l.Contains("wasn't just me") || l.Contains("hang up") || l.Contains("everyone does") ? "sad" :
                "calm";

            string fallback = mood switch { "distressed" => "chloe_distressed", "sad" => "chloe_sad", _ => "chloe_eeriecalm" };
            return CallSprite($"chloe_phone_{mood}", fallback);
        }

        /// <summary>The desk colleague a stage direction is about ("[Charm stops smiling.]"), if any.</summary>
        private Image? DeskPersonIn(string direction) =>
            direction.Contains("Charm", StringComparison.OrdinalIgnoreCase) ? DeskPortraitFor("CHARM") :
            direction.Contains("Krisha", StringComparison.OrdinalIgnoreCase) ? DeskPortraitFor("KRISHA") :
            direction.Contains("Ysa", StringComparison.OrdinalIgnoreCase) ? DeskPortraitFor("YSA") : null;

        /// <summary>Someone at the desk cutting in (Krisha, Charm, Ysa) appears in place of the caller.</summary>
        private Image? DeskPortraitFor(string speaker) => speaker switch
        {
            "KRISHA" => CallSprite("krisha_talking", "krisha_serious"),
            "CHARM" or "CHARMEE" => CallSprite("charm_default", "charm_smile"),
            "YSA" => CallSprite("ysa_talking", "ysa_serious"),
            _ => null // the operator or anyone else: keep whoever is on screen
        };

        private Image? CallSprite(string name, string fallback)
        {
            if (!_callSprites.TryGetValue(name, out var img))
            {
                img = TryLoadImage(name + ".png") ?? TryLoadImage(fallback + ".png");
                _callSprites[name] = img;
            }
            return img;
        }

        /// <summary>Choice captions start with an emoji icon on the terminal; the scene buttons don't need it.</summary>
        private static string StripIcon(string caption)
        {
            int i = 0;
            while (i < caption.Length && !char.IsLetterOrDigit(caption[i]) && caption[i] != '"' && caption[i] != '[') i++;
            return caption.Substring(i).Trim();
        }
    }
}
