using System;
using System.Drawing;
using System.Windows.Forms;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principles Demonstrated:
    /// 1. INHERITANCE: Inherits from BaseGameForm, standardizing borderless fullscreen,
    ///    double buffering, and black background across all dispatch gameplay shifts.
    /// 2. ABSTRACTION: Serves as an abstract base for Shift 1 (Tutorial), Shift 2, and future shifts,
    ///    handling shared audio loops, settings dialogs, and exit confirmations.
    /// 3. POLYMORPHISM: Provides virtual and abstract lifecycle hooks (OnShiftStarted,
    ///    OnCallSelected, OnActionExecuted, OnShiftCompleted) for scenario-specific behavior.
    /// 4. ENCAPSULATION: Encapsulates runtime ShiftState and OperatorProfile with controlled access.
    /// </summary>
    public abstract class BaseShiftForm : BaseGameForm
    {
        public OperatorProfile Profile { get; }
        public IShiftScenario Scenario { get; }
        public ShiftState State { get; }

        protected BaseShiftForm(OperatorProfile profile, IShiftScenario scenario)
        {
            Profile = profile ?? new OperatorProfile(1, "OPERATOR");
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            State = new ShiftState(scenario.InitialUnits);

            InitializeBaseShift();
        }

        private void InitializeBaseShift()
        {
            KeyDown += BaseShiftForm_KeyDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AudioManager.Instance.PlayAmbientLoop();
            OnShiftStarted();
        }

        private void BaseShiftForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                OpenInGameSettings();
            }
        }

        protected void OpenInGameSettings()
        {
            using var settings = new SettingsForm
            {
                CurrentProfile = Profile,
                ShowExitButton = true
            };

            var result = settings.ShowDialog(this);
            if (result == DialogResult.Abort)
            {
                // Player chose to return to Main Menu from in-game settings
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        // Virtual & Abstract Lifecycle Hooks (Polymorphism)
        protected virtual void OnShiftStarted() { }
        protected virtual void OnCallSelected(EmergencyCall call) { }
        protected virtual void OnActionExecuted(DispatchActionType action, EmergencyCall call) { }
        protected virtual void OnShiftCompleted() { }
    }
}
