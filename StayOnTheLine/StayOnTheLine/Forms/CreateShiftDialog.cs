using System.ComponentModel;
using StayOnTheLine.Systems;

namespace StayOnTheLine.Forms
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// Encapsulates operator name validation, input state, and dialog result.
    /// </summary>
    public partial class CreateShiftDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SlotIndex { get; set; } = 1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string OperatorName { get; private set; } = string.Empty;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED: smooth flicker-free compositing
                return cp;
            }
        }

        public CreateShiftDialog(int slotIndex = 1)
        {
            InitializeComponent();
            SlotIndex = slotIndex;

            EnableDoubleBuffering(pnlParchmentContainer);
            EnableDoubleBuffering(pnlInputBox);

            ApplyFonts();
            LoadParchmentBackground();
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
            lblTitle.Font = FontManager.GetSmallCapsFont(24F, FontStyle.Bold);
            lblSubtitle.Font = FontManager.GetItalicFont(12F);
            lblInputLabel.Font = FontManager.GetSmallCapsFont(10.5F, FontStyle.Bold);
            txtOperatorName.Font = FontManager.GetSmallCapsFont(16F, FontStyle.Bold);
            lblAdvisory.Font = FontManager.GetItalicFont(10F);
            btnCancel.Font = FontManager.GetSmallCapsFont(11.5F, FontStyle.Bold);
            btnStartShift.Font = FontManager.GetSmallCapsFont(12F, FontStyle.Bold);
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

            btnStartShift.MouseEnter += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtOperatorName.Text))
                {
                    btnStartShift.BackColor = Color.FromArgb(52, 36, 20);
                    btnStartShift.ForeColor = Color.FromArgb(255, 252, 245);
                }
            };
            btnStartShift.MouseLeave += (s, e) =>
            {
                UpdateStartButtonState();
            };

            lblClose.MouseEnter += (s, e) => lblClose.ForeColor = Color.FromArgb(190, 20, 20);
            lblClose.MouseLeave += (s, e) => lblClose.ForeColor = Color.FromArgb(40, 25, 12);
        }

        private void CreateShiftDialog_Load(object? sender, EventArgs e)
        {
            lblFileHeader.Text = $"RESTRICTED PERSONNEL RECORD // FILE {SlotIndex:D2}";
            UpdateStartButtonState();
            txtOperatorName.Focus();
            AudioManager.Instance.PlayPaperRustle();
        }

        private void txtOperatorName_TextChanged(object? sender, EventArgs e)
        {
            UpdateStartButtonState();
            AudioManager.Instance.PlayTypewriterKeystroke();
        }

        private void UpdateStartButtonState()
        {
            bool isValid = !string.IsNullOrWhiteSpace(txtOperatorName.Text);

            if (isValid)
            {
                btnStartShift.BackColor = Color.FromArgb(32, 22, 12);
                btnStartShift.ForeColor = Color.FromArgb(250, 244, 230);
                btnStartShift.Cursor = Cursors.Hand;
            }
            else
            {
                btnStartShift.BackColor = Color.FromArgb(218, 208, 188);
                btnStartShift.ForeColor = Color.FromArgb(125, 110, 92);
                btnStartShift.Cursor = Cursors.Default;
            }
        }

        private void btnStartShift_Click(object? sender, EventArgs e)
        {
            string name = txtOperatorName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            AudioManager.Instance.PlayInkStamp();

            OperatorName = name;
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

        private void txtOperatorName_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                btnStartShift_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                btnCancel_Click(this, EventArgs.Empty);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter && !string.IsNullOrWhiteSpace(txtOperatorName.Text))
            {
                btnStartShift_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
