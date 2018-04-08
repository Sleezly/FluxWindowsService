namespace HueController
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.labelCurrentFluxTemperature = new System.Windows.Forms.Label();
            this.labelInfo = new System.Windows.Forms.Label();
            this.labelData = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(129, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Current Flux Temperature:";
            // 
            // labelCurrentFluxTemperature
            // 
            this.labelCurrentFluxTemperature.AutoSize = true;
            this.labelCurrentFluxTemperature.Font = new System.Drawing.Font("Microsoft Sans Serif", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCurrentFluxTemperature.Location = new System.Drawing.Point(12, 22);
            this.labelCurrentFluxTemperature.Name = "labelCurrentFluxTemperature";
            this.labelCurrentFluxTemperature.Size = new System.Drawing.Size(108, 55);
            this.labelCurrentFluxTemperature.TabIndex = 1;
            this.labelCurrentFluxTemperature.Text = "416";
            // 
            // labelInfo
            // 
            this.labelInfo.AutoSize = true;
            this.labelInfo.Location = new System.Drawing.Point(12, 77);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(59, 13);
            this.labelInfo.TabIndex = 2;
            this.labelInfo.Text = "Info Labels";
            // 
            // labelData
            // 
            this.labelData.AutoSize = true;
            this.labelData.Location = new System.Drawing.Point(112, 77);
            this.labelData.Name = "labelData";
            this.labelData.Size = new System.Drawing.Size(51, 13);
            this.labelData.TabIndex = 3;
            this.labelData.Text = "Info Data";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(250, 261);
            this.Controls.Add(this.labelData);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.labelCurrentFluxTemperature);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Hue Flux";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelCurrentFluxTemperature;
        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.Label labelData;
    }
}

