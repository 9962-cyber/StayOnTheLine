using System.ComponentModel;
using StayOnTheLine.Models;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates operator dossier purge confirmation state, warning visuals, and dialog result.
    /// </summary>
    public partial class DeleteShiftDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public OperatorProfile TargetProfile { get; }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth flicker-free compositing
                return cp;
            }
        }

        public DeleteShiftDialog(OperatorProfile profile)
        {
            InitializeComponent();
            TargetProfile = profile ?? throw new ArgumentNullException(nameof(profile));

            EnableDoubleBuffering(pnlParchmentContainer);
            EnableDoubleBuffering(pnlDossierCard);

            ApplyFonts();
            LoadParchmentBackground();
            SetupCustomCardPainting();
            WireButtonEvents();
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control).InvokeMember(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null, control, new object[] { true });
            }
            catch { }
        }

        private void LoadParchmentBackground()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string croppedPath = Path.Combine(basePath, "Assets", "parchment_modal_bg.png");
                if (!File.Exists(croppedPath))
                    croppedPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "parchment_modal_bg.png");

                if (File.Exists(croppedPath))
                {
                    pnlParchmentContainer.BackgroundImage = Image.FromFile(croppedPath);
                    pnlParchmentContainer.BackgroundImageLayout = ImageLayout.Stretch;
                }
                else
                {
                    string path = Path.Combine(basePath, "Assets", "creategame_bg.png");
                    if (!File.Exists(path))
                        path = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "creategame_bg.png");

                    if (File.Exists(path))
                    {
                        pnlParchmentContainer.BackgroundImage = Image.FromFile(path);
                        pnlParchmentContainer.BackgroundImageLayout = ImageLayout.Stretch;
                    }
                }
            }
            catch
            {
                pnlParchmentContainer.BackColor = Color.FromArgb(220, 210, 185);
            }
        }

        private void ApplyFonts()
        {
            lblFileHeader.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblTitle.Font = FontManager.GetSmallCapsFont(22F, FontStyle.Bold);
            lblSubtitle.Font = FontManager.GetItalicFont(11.5F);
            lblOperatorName.Font = FontManager.GetSmallCapsFont(18F, FontStyle.Bold);
            lblOperatorStats.Font = FontManager.GetSmallCapsFont(11.5F, FontStyle.Bold);
            lblTimestamp.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            lblAdvisory.Font = FontManager.GetItalicFont(9.5F);
            btnCancel.Font = FontManager.GetSmallCapsFont(11.5F, FontStyle.Bold);
            btnConfirmPurge.Font = FontManager.GetSmallCapsFont(12F, FontStyle.Bold);
        }

        private void SetupCustomCardPainting()
        {
            pnlDossierCard.Paint += (s, e) =>
            {
                var rect = pnlDossierCard.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;

                // Vintage pen card border
                using var pen = new Pen(Color.FromArgb(155, 130, 98), 1);
                e.Graphics.DrawRectangle(pen, rect);

                // Crimson hazard spine marker
                using var hazardBrush = new SolidBrush(Color.FromArgb(120, 32, 20));
                e.Graphics.FillRectangle(hazardBrush, 0, 0, 5, pnlDossierCard.Height);
            };
        }

        private void WireButtonEvents()
        {
            btnCancel.MouseEnter += (s, e) =>
            {
                btnCancel.BackColor = Color.FromArgb(248, 240, 222);
                btnCancel.ForeColor = Color.FromArgb(10, 5, 2);
            };
            btnCancel.MouseLeave += (s, e) =>
            {
                btnCancel.BackColor = Color.FromArgb(232, 222, 200);
                btnCancel.ForeColor = Color.FromArgb(35, 22, 12);
            };

            btnConfirmPurge.MouseEnter += (s, e) =>
            {
                btnConfirmPurge.BackColor = Color.FromArgb(160, 38, 24);
                btnConfirmPurge.ForeColor = Color.White;
            };
            btnConfirmPurge.MouseLeave += (s, e) =>
            {
                btnConfirmPurge.BackColor = Color.FromArgb(120, 32, 20);
                btnConfirmPurge.ForeColor = Color.FromArgb(250, 244, 230);
            };

            lblClose.MouseEnter += (s, e) => lblClose.ForeColor = Color.FromArgb(190, 20, 20);
            lblClose.MouseLeave += (s, e) => lblClose.ForeColor = Color.FromArgb(40, 25, 12);
        }

        private void DeleteShiftDialog_Load(object? sender, EventArgs e)
        {
            lblFileHeader.Text = $"INCINERATION PROTOCOL // FILE {TargetProfile.SlotIndex:D2}";
            lblOperatorName.Text = $"OPERATOR: {TargetProfile.OperatorName.ToUpper()}";
            lblOperatorStats.Text = $"SHIFT {TargetProfile.CurrentShift:D2} - {TargetProfile.CallsLogged} CALLS LOGGED";
            lblTimestamp.Text = $"LAST DISPATCH: {TargetProfile.FormattedTimestamp}";

            btnCancel.Focus();
        }

        private void btnConfirmPurge_Click(object? sender, EventArgs e)
        {
            AudioManager.Instance.PlayBurnHiss();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void lblClose_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                btnConfirmPurge_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
