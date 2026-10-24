using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace StayOnTheLine.Systems
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Remembers the designer (1920x1080) bounds and fonts of a control tree and
    /// rescales them uniformly so fixed-layout screens fit the player's monitor.
    /// </summary>
    public sealed class DesignScaler
    {
        public const float DesignWidth = 1920f;
        public const float DesignHeight = 1080f;

        private readonly Control _root;
        private readonly bool _includeRoot;
        private readonly Dictionary<Control, (Rectangle Bounds, Font Font)> _baseline = new();

        public float Factor { get; private set; } = 1f;

        public DesignScaler(Control root, bool includeRoot = false)
        {
            _root = root;
            _includeRoot = includeRoot;
            if (includeRoot) _baseline[root] = (root.Bounds, root.Font);
            Capture(root);
        }

        /// <summary>Uniform factor that fits the 1920x1080 design inside the given area.</summary>
        public static float FactorFor(Size area) =>
            Math.Max(0.1f, Math.Min(area.Width / DesignWidth, area.Height / DesignHeight));

        /// <summary>
        /// Scales a fixed-size dialog to the monitor it opens on and re-centers it over its owner.
        /// </summary>
        public static void FitDialog(Form dialog)
        {
            Control anchor = dialog.Owner ?? (Control)dialog;
            var screenArea = Screen.FromControl(anchor).Bounds;
            new DesignScaler(dialog, includeRoot: true).Apply(FactorFor(screenArea.Size));

            var parentArea = dialog.Owner?.Bounds ?? screenArea;
            dialog.Location = new Point(
                parentArea.X + (parentArea.Width - dialog.Width) / 2,
                parentArea.Y + (parentArea.Height - dialog.Height) / 2);
        }

        private void Capture(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                _baseline[child] = (child.Bounds, child.Font);
                Capture(child);
            }
        }

        public void Apply(float factor)
        {
            if (Math.Abs(factor - Factor) < 0.001f) return;
            Factor = factor;

            _root.SuspendLayout();
            foreach (var (control, baseline) in _baseline)
            {
                var b = baseline.Bounds;
                var scaled = new Rectangle(
                    (int)Math.Round(b.X * factor), (int)Math.Round(b.Y * factor),
                    Math.Max(1, (int)Math.Round(b.Width * factor)), Math.Max(1, (int)Math.Round(b.Height * factor)));

                if (control == _root && _includeRoot)
                    control.Size = scaled.Size;
                else
                    control.Bounds = scaled;

                var f = baseline.Font;
                control.Font = new Font(f.FontFamily, Math.Max(1f, f.Size * factor), f.Style, f.Unit);
            }
            _root.ResumeLayout(true);
        }
    }
}
