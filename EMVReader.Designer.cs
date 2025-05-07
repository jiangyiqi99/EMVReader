namespace EMVCard
{
    partial class MainEMVReaderBin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainEMVReaderBin));
            this.Label1 = new System.Windows.Forms.Label();
            this.bReset = new System.Windows.Forms.Button();
            this.bClear = new System.Windows.Forms.Button();
            this.bConnect = new System.Windows.Forms.Button();
            this.bInit = new System.Windows.Forms.Button();
            this.cbReader = new System.Windows.Forms.ComboBox();
            this.bQuit = new System.Windows.Forms.Button();
            this.labelApduLogs = new System.Windows.Forms.Label();
            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox();
            this.cbPSE = new System.Windows.Forms.ComboBox();
            this.bReadApp = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textCardNum = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textHolder = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textEXP = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textTrack = new System.Windows.Forms.TextBox();
            this.bLoadPSE = new System.Windows.Forms.Button();
            this.bLoadPPSE = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(7, 22);
            this.Label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(99, 16);
            this.Label1.TabIndex = 10;
            this.Label1.Text = "Select Reader";
            // 
            // bReset
            // 
            this.bReset.Location = new System.Drawing.Point(510, 506);
            this.bReset.Margin = new System.Windows.Forms.Padding(4);
            this.bReset.Name = "bReset";
            this.bReset.Size = new System.Drawing.Size(108, 28);
            this.bReset.TabIndex = 18;
            this.bReset.Text = "Reset";
            this.bReset.UseVisualStyleBackColor = true;
            this.bReset.Click += new System.EventHandler(this.bReset_Click);
            // 
            // bClear
            // 
            this.bClear.Location = new System.Drawing.Point(386, 506);
            this.bClear.Margin = new System.Windows.Forms.Padding(4);
            this.bClear.Name = "bClear";
            this.bClear.Size = new System.Drawing.Size(108, 28);
            this.bClear.TabIndex = 17;
            this.bClear.Text = "Clear";
            this.bClear.UseVisualStyleBackColor = true;
            this.bClear.Click += new System.EventHandler(this.bClear_Click);
            // 
            // bConnect
            // 
            this.bConnect.Location = new System.Drawing.Point(206, 87);
            this.bConnect.Margin = new System.Windows.Forms.Padding(4);
            this.bConnect.Name = "bConnect";
            this.bConnect.Size = new System.Drawing.Size(160, 28);
            this.bConnect.TabIndex = 13;
            this.bConnect.Text = "Connect";
            this.bConnect.UseVisualStyleBackColor = true;
            this.bConnect.Click += new System.EventHandler(this.bConnect_Click);
            // 
            // bInit
            // 
            this.bInit.Location = new System.Drawing.Point(206, 52);
            this.bInit.Margin = new System.Windows.Forms.Padding(4);
            this.bInit.Name = "bInit";
            this.bInit.Size = new System.Drawing.Size(160, 28);
            this.bInit.TabIndex = 12;
            this.bInit.Text = "Initialize";
            this.bInit.UseVisualStyleBackColor = true;
            this.bInit.Click += new System.EventHandler(this.bInit_Click);
            // 
            // cbReader
            // 
            this.cbReader.FormattingEnabled = true;
            this.cbReader.Location = new System.Drawing.Point(106, 18);
            this.cbReader.Margin = new System.Windows.Forms.Padding(4);
            this.cbReader.Name = "cbReader";
            this.cbReader.Size = new System.Drawing.Size(258, 24);
            this.cbReader.TabIndex = 11;
            // 
            // bQuit
            // 
            this.bQuit.Location = new System.Drawing.Point(632, 506);
            this.bQuit.Margin = new System.Windows.Forms.Padding(4);
            this.bQuit.Name = "bQuit";
            this.bQuit.Size = new System.Drawing.Size(121, 28);
            this.bQuit.TabIndex = 19;
            this.bQuit.Text = "Quit";
            this.bQuit.UseVisualStyleBackColor = true;
            this.bQuit.Click += new System.EventHandler(this.bQuit_Click);
            // 
            // labelApduLogs
            // 
            this.labelApduLogs.AutoSize = true;
            this.labelApduLogs.Location = new System.Drawing.Point(383, 18);
            this.labelApduLogs.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelApduLogs.Name = "labelApduLogs";
            this.labelApduLogs.Size = new System.Drawing.Size(77, 16);
            this.labelApduLogs.TabIndex = 20;
            this.labelApduLogs.Text = "APDU Logs";
            // 
            // richTextBoxLogs
            // 
            this.richTextBoxLogs.Location = new System.Drawing.Point(386, 37);
            this.richTextBoxLogs.Name = "richTextBoxLogs";
            this.richTextBoxLogs.Size = new System.Drawing.Size(367, 462);
            this.richTextBoxLogs.TabIndex = 21;
            this.richTextBoxLogs.Text = "";
            // 
            // cbPSE
            // 
            this.cbPSE.FormattingEnabled = true;
            this.cbPSE.Location = new System.Drawing.Point(24, 192);
            this.cbPSE.Name = "cbPSE";
            this.cbPSE.Size = new System.Drawing.Size(148, 24);
            this.cbPSE.TabIndex = 24;
            // 
            // bReadApp
            // 
            this.bReadApp.Location = new System.Drawing.Point(190, 192);
            this.bReadApp.Name = "bReadApp";
            this.bReadApp.Size = new System.Drawing.Size(136, 23);
            this.bReadApp.TabIndex = 25;
            this.bReadApp.Text = "ReadApp";
            this.bReadApp.UseVisualStyleBackColor = true;
            this.bReadApp.Click += new System.EventHandler(this.bReadApp_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(32, 233);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(91, 16);
            this.label2.TabIndex = 26;
            this.label2.Text = "Card Number";
            // 
            // textCardNum
            // 
            this.textCardNum.Location = new System.Drawing.Point(169, 226);
            this.textCardNum.Name = "textCardNum";
            this.textCardNum.Size = new System.Drawing.Size(134, 23);
            this.textCardNum.TabIndex = 27;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(32, 268);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 16);
            this.label3.TabIndex = 28;
            this.label3.Text = "Holder Name";
            // 
            // textHolder
            // 
            this.textHolder.Location = new System.Drawing.Point(169, 265);
            this.textHolder.Name = "textHolder";
            this.textHolder.Size = new System.Drawing.Size(134, 23);
            this.textHolder.TabIndex = 29;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(35, 302);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 16);
            this.label4.TabIndex = 30;
            this.label4.Text = "EXP Date";
            // 
            // textEXP
            // 
            this.textEXP.Location = new System.Drawing.Point(169, 302);
            this.textEXP.Name = "textEXP";
            this.textEXP.Size = new System.Drawing.Size(134, 23);
            this.textEXP.TabIndex = 31;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(47, 343);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 16);
            this.label5.TabIndex = 32;
            this.label5.Text = "Track 2";
            // 
            // textTrack
            // 
            this.textTrack.Location = new System.Drawing.Point(169, 340);
            this.textTrack.Multiline = true;
            this.textTrack.Name = "textTrack";
            this.textTrack.Size = new System.Drawing.Size(134, 69);
            this.textTrack.TabIndex = 33;
            // 
            // bLoadPSE
            // 
            this.bLoadPSE.Location = new System.Drawing.Point(35, 133);
            this.bLoadPSE.Name = "bLoadPSE";
            this.bLoadPSE.Size = new System.Drawing.Size(121, 45);
            this.bLoadPSE.TabIndex = 34;
            this.bLoadPSE.Text = "Load PSE\r\n接触式";
            this.bLoadPSE.UseVisualStyleBackColor = true;
            this.bLoadPSE.Click += new System.EventHandler(this.bLoadPSE_Click);
            // 
            // bLoadPPSE
            // 
            this.bLoadPPSE.Location = new System.Drawing.Point(196, 133);
            this.bLoadPPSE.Name = "bLoadPPSE";
            this.bLoadPPSE.Size = new System.Drawing.Size(129, 44);
            this.bLoadPPSE.TabIndex = 35;
            this.bLoadPPSE.Text = "Load PPSE\r\n（非接触）";
            this.bLoadPPSE.UseVisualStyleBackColor = true;
            this.bLoadPPSE.Click += new System.EventHandler(this.bLoadPPSE_Click);
            // 
            // MainEMVReaderBin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(766, 542);
            this.Controls.Add(this.bLoadPPSE);
            this.Controls.Add(this.bLoadPSE);
            this.Controls.Add(this.textTrack);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textEXP);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textHolder);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textCardNum);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.bReadApp);
            this.Controls.Add(this.cbPSE);
            this.Controls.Add(this.richTextBoxLogs);
            this.Controls.Add(this.labelApduLogs);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.bReset);
            this.Controls.Add(this.bClear);
            this.Controls.Add(this.bConnect);
            this.Controls.Add(this.bInit);
            this.Controls.Add(this.cbReader);
            this.Controls.Add(this.bQuit);
            this.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainEMVReaderBin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PS/SC EMV Card Reader";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.Button bReset;
        internal System.Windows.Forms.Button bClear;
        internal System.Windows.Forms.Button bConnect;
        internal System.Windows.Forms.Button bInit;
        internal System.Windows.Forms.ComboBox cbReader;
        internal System.Windows.Forms.Button bQuit;
        internal System.Windows.Forms.Label labelApduLogs;
        private System.Windows.Forms.RichTextBox richTextBoxLogs;
        private System.Windows.Forms.ComboBox cbPSE;
        private System.Windows.Forms.Button bReadApp;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textCardNum;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textHolder;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textEXP;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textTrack;
        private System.Windows.Forms.Button bLoadPSE;
        private System.Windows.Forms.Button bLoadPPSE;
    }
}

