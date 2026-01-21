namespace ERFX_Q03UDV_20260121_01
{
    partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.dgvDevices = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colWrite = new System.Windows.Forms.DataGridViewButtonColumn();
            this.tmrMonitor = new System.Windows.Forms.Timer(this.components);
            this.pnlTop = new System.Windows.Forms.Panel();
            this.lblInterval = new System.Windows.Forms.Label();
            this.lblZmqStatus = new System.Windows.Forms.Label();
            this.lblMqttStatus = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDevices)).BeginInit();
            this.pnlTop.SuspendLayout();
            this.SuspendLayout();
            //
            // btnConnect
            //
            this.btnConnect.Location = new System.Drawing.Point(12, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(100, 30);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "연결";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            //
            // btnDisconnect
            //
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(118, 12);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(100, 30);
            this.btnDisconnect.TabIndex = 1;
            this.btnDisconnect.Text = "연결 해제";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblStatus.ForeColor = System.Drawing.Color.Red;
            this.lblStatus.Location = new System.Drawing.Point(234, 19);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(60, 15);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "연결 안됨";
            //
            // dgvDevices
            //
            this.dgvDevices.AllowUserToAddRows = false;
            this.dgvDevices.AllowUserToDeleteRows = false;
            this.dgvDevices.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDevices.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDevices.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colName,
            this.colAddress,
            this.colType,
            this.colValue,
            this.colWrite});
            this.dgvDevices.Location = new System.Drawing.Point(12, 60);
            this.dgvDevices.Name = "dgvDevices";
            this.dgvDevices.RowHeadersWidth = 51;
            this.dgvDevices.RowTemplate.Height = 23;
            this.dgvDevices.Size = new System.Drawing.Size(576, 328);
            this.dgvDevices.TabIndex = 3;
            this.dgvDevices.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDevices_CellClick);
            //
            // colName
            //
            this.colName.DataPropertyName = "Name";
            this.colName.HeaderText = "이름";
            this.colName.MinimumWidth = 6;
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            this.colName.Width = 100;
            //
            // colAddress
            //
            this.colAddress.DataPropertyName = "Address";
            this.colAddress.HeaderText = "주소";
            this.colAddress.MinimumWidth = 6;
            this.colAddress.Name = "colAddress";
            this.colAddress.ReadOnly = true;
            this.colAddress.Width = 80;
            //
            // colType
            //
            this.colType.DataPropertyName = "Type";
            this.colType.HeaderText = "타입";
            this.colType.MinimumWidth = 6;
            this.colType.Name = "colType";
            this.colType.ReadOnly = true;
            this.colType.Width = 60;
            //
            // colValue
            //
            this.colValue.DataPropertyName = "DisplayValue";
            this.colValue.HeaderText = "값";
            this.colValue.MinimumWidth = 6;
            this.colValue.Name = "colValue";
            this.colValue.ReadOnly = true;
            this.colValue.Width = 120;
            //
            // colWrite
            //
            this.colWrite.HeaderText = "쓰기";
            this.colWrite.MinimumWidth = 6;
            this.colWrite.Name = "colWrite";
            this.colWrite.Text = "쓰기";
            this.colWrite.UseColumnTextForButtonValue = true;
            this.colWrite.Width = 80;
            //
            // tmrMonitor
            //
            this.tmrMonitor.Interval = 100;
            this.tmrMonitor.Tick += new System.EventHandler(this.tmrMonitor_Tick);
            //
            // lblZmqStatus
            //
            this.lblZmqStatus.AutoSize = true;
            this.lblZmqStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblZmqStatus.Location = new System.Drawing.Point(310, 19);
            this.lblZmqStatus.Name = "lblZmqStatus";
            this.lblZmqStatus.Size = new System.Drawing.Size(90, 15);
            this.lblZmqStatus.TabIndex = 4;
            this.lblZmqStatus.Text = "ZMQ: 연결 안됨";
            //
            // lblMqttStatus
            //
            this.lblMqttStatus.AutoSize = true;
            this.lblMqttStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblMqttStatus.Location = new System.Drawing.Point(405, 19);
            this.lblMqttStatus.Name = "lblMqttStatus";
            this.lblMqttStatus.Size = new System.Drawing.Size(98, 15);
            this.lblMqttStatus.TabIndex = 5;
            this.lblMqttStatus.Text = "MQTT: 연결 안됨";
            //
            // pnlTop
            //
            this.pnlTop.Controls.Add(this.lblMqttStatus);
            this.pnlTop.Controls.Add(this.lblZmqStatus);
            this.pnlTop.Controls.Add(this.lblInterval);
            this.pnlTop.Controls.Add(this.btnConnect);
            this.pnlTop.Controls.Add(this.btnDisconnect);
            this.pnlTop.Controls.Add(this.lblStatus);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(600, 54);
            this.pnlTop.TabIndex = 4;
            //
            // lblInterval
            //
            this.lblInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblInterval.AutoSize = true;
            this.lblInterval.Location = new System.Drawing.Point(480, 19);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(108, 15);
            this.lblInterval.TabIndex = 3;
            this.lblInterval.Text = "갱신 주기: 100ms";
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 400);
            this.Controls.Add(this.dgvDevices);
            this.Controls.Add(this.pnlTop);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.MinimumSize = new System.Drawing.Size(500, 300);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Q03UDV PLC 메모리 모니터";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDevices)).EndInit();
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridView dgvDevices;
        private System.Windows.Forms.Timer tmrMonitor;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Label lblInterval;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAddress;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValue;
        private System.Windows.Forms.DataGridViewButtonColumn colWrite;
        private System.Windows.Forms.Label lblZmqStatus;
        private System.Windows.Forms.Label lblMqttStatus;
    }
}
