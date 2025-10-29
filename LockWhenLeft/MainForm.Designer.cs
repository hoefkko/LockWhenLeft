using System.Windows.Forms;

namespace LockWhenLeft
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox cameraFeedBox;
        private System.Windows.Forms.TrackBar confidenceTresholdTrackBar;
        private System.Windows.Forms.Label confidenceLabel;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.CheckBox chkForceCamera;
        private System.Windows.Forms.Label sensitivityLabel;
        private System.Windows.Forms.TrackBar sensitivityTrackBar;

        // *** REPLACED TrackBars WITH NumericUpDown ***
        private System.Windows.Forms.NumericUpDown noInputActiveDelayNumericUpDown;
        private System.Windows.Forms.Label noInputActiveDelayLabel;
        private System.Windows.Forms.NumericUpDown noPersonDetectedDelayNumericUpDown;
        private System.Windows.Forms.Label noPersonDetectedDelayLabel;
        private System.Windows.Forms.NumericUpDown popupTimeoutNumericUpDown;
        private System.Windows.Forms.Label popupTimeoutLabel;


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            cameraFeedBox = new System.Windows.Forms.PictureBox();
            confidenceTresholdTrackBar = new System.Windows.Forms.TrackBar();
            confidenceLabel = new System.Windows.Forms.Label();
            chkForceCamera = new System.Windows.Forms.CheckBox();
            sensitivityLabel = new System.Windows.Forms.Label();
            sensitivityTrackBar = new System.Windows.Forms.TrackBar();
            chkStartup = new System.Windows.Forms.CheckBox();
            noInputActiveDelayNumericUpDown = new System.Windows.Forms.NumericUpDown();
            noInputActiveDelayLabel = new System.Windows.Forms.Label();
            noPersonDetectedDelayNumericUpDown = new System.Windows.Forms.NumericUpDown();
            noPersonDetectedDelayLabel = new System.Windows.Forms.Label();
            popupTimeoutNumericUpDown = new System.Windows.Forms.NumericUpDown();
            popupTimeoutLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) cameraFeedBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize) confidenceTresholdTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize) sensitivityTrackBar).BeginInit();
            ((System.ComponentModel.ISupportInitialize) noInputActiveDelayNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize) noPersonDetectedDelayNumericUpDown).BeginInit();
            ((System.ComponentModel.ISupportInitialize) popupTimeoutNumericUpDown).BeginInit();
            SuspendLayout();
            //
            // cameraFeedBox
            //
            cameraFeedBox.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            cameraFeedBox.Location = new System.Drawing.Point(12, 11);
            cameraFeedBox.Name = "cameraFeedBox";
            cameraFeedBox.Size = new System.Drawing.Size(742, 453);
            cameraFeedBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            cameraFeedBox.TabIndex = 0;
            cameraFeedBox.TabStop = false;
            //
            // confidenceTresholdTrackBar
            //
            confidenceTresholdTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            confidenceTresholdTrackBar.Location = new System.Drawing.Point(276, 504);
            confidenceTresholdTrackBar.Maximum = 20;
            confidenceTresholdTrackBar.Minimum = 1;
            confidenceTresholdTrackBar.Name = "confidenceTresholdTrackBar";
            confidenceTresholdTrackBar.Size = new System.Drawing.Size(478, 69);
            confidenceTresholdTrackBar.TabIndex = 1;
            confidenceTresholdTrackBar.TickFrequency = 20;
            confidenceTresholdTrackBar.Value = 20;
            //
            // confidenceLabel
            //
            confidenceLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            confidenceLabel.AutoSize = true;
            confidenceLabel.Location = new System.Drawing.Point(12, 504);
            confidenceLabel.Name = "confidenceLabel";
            confidenceLabel.Size = new System.Drawing.Size(266, 25);
            confidenceLabel.TabIndex = 2;
            confidenceLabel.Text = "Confidence treshold (low - high)";
            //
            // chkForceCamera
            //
            chkForceCamera.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            chkForceCamera.AutoSize = true;
            chkForceCamera.Location = new System.Drawing.Point(12, 619);
            chkForceCamera.Name = "chkForceCamera";
            chkForceCamera.Size = new System.Drawing.Size(183, 29);
            chkForceCamera.TabIndex = 3;
            chkForceCamera.Text = "Force camera feed";
            chkForceCamera.UseVisualStyleBackColor = true;
            //
            // sensitivityLabel
            //
            sensitivityLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            sensitivityLabel.AutoSize = true;
            sensitivityLabel.Location = new System.Drawing.Point(12, 580);
            sensitivityLabel.Name = "sensitivityLabel";
            sensitivityLabel.Size = new System.Drawing.Size(176, 25);
            sensitivityLabel.TabIndex = 5;
            sensitivityLabel.Text = "Sensitivity (low-high)";
            //
            // sensitivityTrackBar
            //
            sensitivityTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right));
            sensitivityTrackBar.Location = new System.Drawing.Point(276, 579);
            sensitivityTrackBar.Maximum = 100;
            sensitivityTrackBar.Minimum = 1;
            sensitivityTrackBar.Name = "sensitivityTrackBar";
            sensitivityTrackBar.Size = new System.Drawing.Size(478, 69);
            sensitivityTrackBar.TabIndex = 4;
            sensitivityTrackBar.TickFrequency = 20;
            sensitivityTrackBar.Value = 5;
            //
            // chkStartup
            //
            chkStartup.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            chkStartup.AutoSize = true;
            chkStartup.Location = new System.Drawing.Point(12, 669);
            chkStartup.Name = "chkStartup";
            chkStartup.Size = new System.Drawing.Size(191, 29);
            chkStartup.TabIndex = 6;
            chkStartup.Text = "Start with Windows";
            //
            // noInputActiveDelayNumericUpDown
            //
            noInputActiveDelayNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            noInputActiveDelayNumericUpDown.Location = new System.Drawing.Point(276, 719);
            noInputActiveDelayNumericUpDown.Maximum = new decimal(new int[] {20, 0, 0, 0});
            noInputActiveDelayNumericUpDown.Minimum = new decimal(new int[] {1, 0, 0, 0});
            noInputActiveDelayNumericUpDown.Name = "noInputActiveDelayNumericUpDown";
            noInputActiveDelayNumericUpDown.Size = new System.Drawing.Size(120, 31);
            noInputActiveDelayNumericUpDown.TabIndex = 8;
            noInputActiveDelayNumericUpDown.Value = new decimal(new int[] {5, 0, 0, 0});
            //
            // noInputActiveDelayLabel
            //
            noInputActiveDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            noInputActiveDelayLabel.AutoSize = true;
            noInputActiveDelayLabel.Location = new System.Drawing.Point(12, 719);
            noInputActiveDelayLabel.Name = "noInputActiveDelayLabel";
            noInputActiveDelayLabel.Size = new System.Drawing.Size(222, 25);
            noInputActiveDelayLabel.TabIndex = 7;
            noInputActiveDelayLabel.Text = "No Input Pause Delay (sec)";
            //
            // noPersonDetectedDelayNumericUpDown
            //
            noPersonDetectedDelayNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            noPersonDetectedDelayNumericUpDown.Location = new System.Drawing.Point(276, 769);
            noPersonDetectedDelayNumericUpDown.Maximum = new decimal(new int[] {20, 0, 0, 0});
            noPersonDetectedDelayNumericUpDown.Minimum = new decimal(new int[] {1, 0, 0, 0});
            noPersonDetectedDelayNumericUpDown.Name = "noPersonDetectedDelayNumericUpDown";
            noPersonDetectedDelayNumericUpDown.Size = new System.Drawing.Size(120, 31);
            noPersonDetectedDelayNumericUpDown.TabIndex = 10;
            noPersonDetectedDelayNumericUpDown.Value = new decimal(new int[] {5, 0, 0, 0});
            //
            // noPersonDetectedDelayLabel
            //
            noPersonDetectedDelayLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            noPersonDetectedDelayLabel.AutoSize = true;
            noPersonDetectedDelayLabel.Location = new System.Drawing.Point(12, 769);
            noPersonDetectedDelayLabel.Name = "noPersonDetectedDelayLabel";
            noPersonDetectedDelayLabel.Size = new System.Drawing.Size(224, 25);
            noPersonDetectedDelayLabel.TabIndex = 9;
            noPersonDetectedDelayLabel.Text = "No Person Lock Delay (sec)";
            //
            // popupTimeoutNumericUpDown
            //
            popupTimeoutNumericUpDown.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            popupTimeoutNumericUpDown.Location = new System.Drawing.Point(276, 819);
            popupTimeoutNumericUpDown.Maximum = new decimal(new int[] {20, 0, 0, 0});
            popupTimeoutNumericUpDown.Minimum = new decimal(new int[] {1, 0, 0, 0});
            popupTimeoutNumericUpDown.Name = "popupTimeoutNumericUpDown";
            popupTimeoutNumericUpDown.Size = new System.Drawing.Size(120, 31);
            popupTimeoutNumericUpDown.TabIndex = 12;
            popupTimeoutNumericUpDown.Value = new decimal(new int[] {5, 0, 0, 0});
            //
            // popupTimeoutLabel
            //
            popupTimeoutLabel.Anchor = ((System.Windows.Forms.AnchorStyles) (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            popupTimeoutLabel.AutoSize = true;
            popupTimeoutLabel.Location = new System.Drawing.Point(12, 819);
            popupTimeoutLabel.Name = "popupTimeoutLabel";
            popupTimeoutLabel.Size = new System.Drawing.Size(174, 25);
            popupTimeoutLabel.TabIndex = 11;
            popupTimeoutLabel.Text = "Popup Timeout (sec)";
            //
            // MainForm
            //
            ClientSize = new System.Drawing.Size(770, 900);
            Controls.Add(sensitivityLabel);
            Controls.Add(sensitivityTrackBar);
            Controls.Add(chkForceCamera);
            Controls.Add(confidenceLabel);
            Controls.Add(confidenceTresholdTrackBar);
            Controls.Add(cameraFeedBox);
            Controls.Add(chkStartup);
            Controls.Add(noInputActiveDelayLabel);
            Controls.Add(noInputActiveDelayNumericUpDown);
            Controls.Add(noPersonDetectedDelayLabel);
            Controls.Add(noPersonDetectedDelayNumericUpDown);
            Controls.Add(popupTimeoutLabel);
            Controls.Add(popupTimeoutNumericUpDown);
            Icon = ((System.Drawing.Icon) resources.GetObject("$this.Icon"));
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Motion Lock App";
            WindowState = System.Windows.Forms.FormWindowState.Minimized;
            FormClosing += MainForm_FormClosing;
            ((System.ComponentModel.ISupportInitialize) cameraFeedBox).EndInit();
            ((System.ComponentModel.ISupportInitialize) confidenceTresholdTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize) sensitivityTrackBar).EndInit();
            ((System.ComponentModel.ISupportInitialize) noInputActiveDelayNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize) noPersonDetectedDelayNumericUpDown).EndInit();
            ((System.ComponentModel.ISupportInitialize) popupTimeoutNumericUpDown).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}