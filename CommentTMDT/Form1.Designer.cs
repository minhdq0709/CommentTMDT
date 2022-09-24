
namespace CommentTMDT
{
    partial class Form1
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
			this.panel1 = new System.Windows.Forms.Panel();
			this.cbNamePage = new System.Windows.Forms.ComboBox();
			this.lbStatus = new System.Windows.Forms.Label();
			this.lbSumComment = new System.Windows.Forms.Label();
			this.lbIsError = new System.Windows.Forms.Label();
			this.btnRun = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.txtCookie = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Location = new System.Drawing.Point(12, 107);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(776, 331);
			this.panel1.TabIndex = 0;
			// 
			// cbNamePage
			// 
			this.cbNamePage.FormattingEnabled = true;
			this.cbNamePage.Location = new System.Drawing.Point(12, 9);
			this.cbNamePage.Name = "cbNamePage";
			this.cbNamePage.Size = new System.Drawing.Size(121, 21);
			this.cbNamePage.TabIndex = 1;
			// 
			// lbStatus
			// 
			this.lbStatus.AutoSize = true;
			this.lbStatus.Location = new System.Drawing.Point(278, 12);
			this.lbStatus.Name = "lbStatus";
			this.lbStatus.Size = new System.Drawing.Size(24, 13);
			this.lbStatus.TabIndex = 2;
			this.lbStatus.Text = "Idle";
			// 
			// lbSumComment
			// 
			this.lbSumComment.AutoSize = true;
			this.lbSumComment.Location = new System.Drawing.Point(511, 12);
			this.lbSumComment.Name = "lbSumComment";
			this.lbSumComment.Size = new System.Drawing.Size(13, 13);
			this.lbSumComment.TabIndex = 3;
			this.lbSumComment.Text = "0";
			// 
			// lbIsError
			// 
			this.lbIsError.AutoSize = true;
			this.lbIsError.Location = new System.Drawing.Point(756, 12);
			this.lbIsError.Name = "lbIsError";
			this.lbIsError.Size = new System.Drawing.Size(32, 13);
			this.lbIsError.TabIndex = 4;
			this.lbIsError.Text = "ko lỗi";
			// 
			// btnRun
			// 
			this.btnRun.Location = new System.Drawing.Point(139, 7);
			this.btnRun.Name = "btnRun";
			this.btnRun.Size = new System.Drawing.Size(75, 23);
			this.btnRun.TabIndex = 5;
			this.btnRun.Text = "Run";
			this.btnRun.UseVisualStyleBackColor = true;
			this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(13, 30);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(775, 20);
			this.textBox1.TabIndex = 6;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 71);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(43, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Cookie:";
			// 
			// txtCookie
			// 
			this.txtCookie.Location = new System.Drawing.Point(58, 68);
			this.txtCookie.Name = "txtCookie";
			this.txtCookie.Size = new System.Drawing.Size(730, 20);
			this.txtCookie.TabIndex = 8;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.txtCookie);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.btnRun);
			this.Controls.Add(this.lbIsError);
			this.Controls.Add(this.lbSumComment);
			this.Controls.Add(this.lbStatus);
			this.Controls.Add(this.cbNamePage);
			this.Controls.Add(this.panel1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox cbNamePage;
        private System.Windows.Forms.Label lbStatus;
        private System.Windows.Forms.Label lbSumComment;
        private System.Windows.Forms.Label lbIsError;
        private System.Windows.Forms.Button btnRun;
    private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtCookie;
	}
}

