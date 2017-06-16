using DecFtpNmb.Controllers;
using DecFtpNmb.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DecFtpNmb.App
{
    public partial class mainForm : Form
    {
        private int intOriginalExStyle = -1;
        private bool bEnableAntiFlicker = true;
        private readonly string _sqlConnectionForEdi;
        private readonly string _sqlConnectionForCustoms;
        private readonly string _sqlConnectionForDocuShare;
        private readonly string _ftpHost;
        private readonly string _ftpUser;
        private readonly string _ftpPass;
        private readonly string _ftpExpNmbPath;
        private readonly string _ftpImpNmbPath;
        private List<Invoice> invoiceObjs;
        private CultureInfo _cultureInfo = new CultureInfo("en-US");

        public mainForm()
        {
            ToggleAntiFlicker(false);
            InitializeComponent();

            _sqlConnectionForCustoms = ConfigurationManager.AppSettings["CustomsConnection"];
            _sqlConnectionForDocuShare = ConfigurationManager.AppSettings["DocuShareConnection"];
            _sqlConnectionForEdi = ConfigurationManager.AppSettings["EDIConnection"];
            _ftpHost = ConfigurationManager.AppSettings["ftpHost"];
            _ftpUser = ConfigurationManager.AppSettings["ftpUser"];
            _ftpPass = ConfigurationManager.AppSettings["ftpPass"];
            _ftpExpNmbPath = ConfigurationManager.AppSettings["ftpExpNmbPath"];
            _ftpImpNmbPath = ConfigurationManager.AppSettings["ftpImpNmbPath"];
            this.ResizeBegin += Form1_ResizeBegin;
            this.ResizeEnd += Form1_ResizeEnd;

            initDataGridViewColumns(dataGridView1);
            invoiceObjs = new List<Invoice>();
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            ToggleAntiFlicker(false);
        }

        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
            ToggleAntiFlicker(true);
        }

        private void ToggleAntiFlicker(bool Enable)
        {
            bEnableAntiFlicker = Enable;
            this.MaximizeBox = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                if (intOriginalExStyle == -1)
                {
                    intOriginalExStyle = base.CreateParams.ExStyle;
                }
                CreateParams cp = base.CreateParams;

                if (bEnableAntiFlicker)
                {
                    cp.ExStyle |= 0x02000000; //WS_EX_COMPOSITED
                }
                else
                {
                    cp.ExStyle = intOriginalExStyle;
                }

                return cp;
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (cboShipmentType.Text == "" || txtPeriod.Text == "" || txtPeriod.Text.Length != 6)
            {
                MessageBox.Show("Please enter correct filter.!!!");
                return;
            }

            try
            {
                var isPeriodNumber = Convert.ToInt32(txtPeriod.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid period : " + ex.Message);
                return;
            }

            var shipmentType = EnumShipment.EXPORT;

            if (cboShipmentType.Text == "IMPORT")
                shipmentType = EnumShipment.IMPORT;

            btnSearch.Tag = new object[] { shipmentType, txtPeriod.Text };

            backgroundWorker1.RunWorkerAsync(new object[] {
                JobType.InvoiceQuery,
                shipmentType,
                txtPeriod.Text });

            progressBar1.Style = ProgressBarStyle.Marquee;
            lblTotal.Text = "";
            lblStatus.Text = "Loading...";
            dataGridView1.DataSource = null;
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            txtPeriod.Text = DateTime.Now.ToString("yyyyMM", new CultureInfo("en-US"));
        }

        private DataGridViewTextBoxColumn createDataGridViewTextBoxColumn(
            string title, string propertyName, int width,
            DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
        {
            var col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = propertyName,
                HeaderText = title.ToUpper(),
                Name = "__col__" + propertyName,
                ReadOnly = true,
                Width = width
            };

            col.DefaultCellStyle.Alignment = alignment;
            return col;
        }

        private void initDataGridViewColumns(DataGridView dgv)
        {
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeight = 28;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Tahoma", 8.25f, FontStyle.Regular);
            dgv.AutoGenerateColumns = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowTemplate.Height = 24;
            dgv.RowTemplate.MinimumHeight = 22;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToResizeRows = false;

            dgv.MultiSelect = true;

            dgv.Columns.Add(createDataGridViewTextBoxColumn("CmpCd", "CmpCd", 60, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("DivCd", "DivCd", 60, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("TradCd", "TradCd", 60, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("InvoiceNo", "InvoiceNo", 250, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("DecNo", "DecNo", 250, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("MaterialType", "MaterialType", 100, DataGridViewContentAlignment.MiddleCenter));
            dgv.Columns.Add(createDataGridViewTextBoxColumn("DownloadStatus", "DownloadStatus", 150, DataGridViewContentAlignment.MiddleCenter));

            for (int i = 0; i < dgv.Columns.Count; i++)
            {
                dgv.Columns[i].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var args = e.Argument as object[];
                var jobType = (JobType)args[0];
                var shipmentType = (EnumShipment)args[1];
                var period = (string)args[2];
                var yearPeriod = Convert.ToInt32(period.Substring(0, 4));
                var monthPeriod = Convert.ToInt32(period.Substring(4, 2));

                var fromdate = new DateTime(yearPeriod, monthPeriod, 1);
                var todate = new DateTime(yearPeriod, monthPeriod, DateTime.DaysInMonth(yearPeriod, monthPeriod));

                var invList = new List<Invoice>();
                var invController = new InvoiceController(this._sqlConnectionForEdi);

                switch (jobType)
                {
                    case JobType.InvoiceQuery:

                        var invoiceQuery = invController.getInvoice(
                            shipmentType,
                            fromdate.ToString("yyyyMMdd", _cultureInfo),
                            todate.ToString("yyyyMMdd", _cultureInfo));

                        for (int i = 0; i < invoiceQuery.Count; i++)
                        {
                            invList.Add(invoiceQuery[i]);
                        }
                        e.Result = new object[] { jobType, invList };
                        break;

                    case JobType.CheckDocStatus:

                        invList = args[3] as List<Invoice>;

                        // Check DocStatus (98,99)
                        var customsController = new CustomsController(this._sqlConnectionForCustoms);
                        var customsQuery = customsController.getDeclarations(shipmentType,
                            fromdate.ToString("yyyyMMdd", _cultureInfo),
                            todate.ToString("yyyyMMdd", _cultureInfo));

                        var filterInvoices = new List<Invoice>();

                        for (int i = 0; i < invList.Count; i++)
                        {
                            var invObj = invList[i];
                            var queryInv = customsQuery.Find(
                                    q => q.DecNo.TrimEnd() == invObj.DecNo.TrimEnd() &&
                                    Convert.ToInt32(q.DocStatus) <= 4);

                            if (queryInv != null)
                                filterInvoices.Add(invObj);
                        }

                        e.Result = new object[] { jobType, filterInvoices };
                        break;

                    case JobType.CheckMaterial:

                        invList = args[3] as List<Invoice>;

                        var docuShareController = new DocuShareController(this._sqlConnectionForDocuShare);
                        var docuShareQuery = docuShareController.getDocMaterial(
                            fromdate.ToString("yyyyMMdd", _cultureInfo),
                            todate.ToString("yyyyMMdd", _cultureInfo));

                        for (int i = 0; i < invList.Count; i++)
                        {
                            var invObj = invList[i];
                            var queryInv = docuShareQuery.Find(
                                q => q.DecNo.TrimEnd() == invObj.DecNo.TrimEnd());

                            if (queryInv != null)
                            {
                                invObj.MaterialType = queryInv.MaterialType;
                            }
                        }

                        e.Result = new object[] { jobType, invList };

                        break;

                    case JobType.ExportPdf:

                        invList = args[3] as List<Invoice>;

                        var pdfController = new PdfController(_ftpHost, _ftpUser, _ftpPass);

                        var destFolder = args[4].ToString();
                        var location = _ftpExpNmbPath;

                        if (shipmentType == EnumShipment.IMPORT)
                        {
                            // create folder for R/M.
                            pdfController.CreateFolderForMaterial(destFolder);

                            for (int i = 0; i < invList.Count; i++)
                            {
                                var invObj = invList[i];
                                var downloadStatus = pdfController.Download(
                                    Path.Combine(destFolder, invObj.MaterialType), location, invObj.DecNo.TrimEnd());

                                if (downloadStatus)
                                    invObj.DownloadStatus = "Success";
                                else
                                    invObj.DownloadStatus = "Not Found";
                            }

                            // Create Index.
                            var rawInvList = invList.Where(q => q.MaterialType == "R").ToList();
                            var matInvList = invList.Where(q => q.MaterialType == "M").ToList();

                            if (rawInvList != null && rawInvList.Count > 0)
                                invController.WriteIndex(Path.Combine(destFolder, "R"), rawInvList);

                            if (matInvList != null && matInvList.Count > 0)
                                invController.WriteIndex(Path.Combine(destFolder, "M"), rawInvList);
                        }
                        else
                        {
                            for (int i = 0; i < invList.Count; i++)
                            {
                                var invObj = invList[i];
                                var downloadStatus = pdfController.Download(destFolder, location, invObj.DecNo.TrimEnd());

                                if (downloadStatus)
                                    invObj.DownloadStatus = "Success";
                                else
                                    invObj.DownloadStatus = "Not Found";
                            }

                            invController.WriteIndex(destFolder, invList);
                        }

                        e.Result = new object[] { jobType, invList };
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message);
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Blocks;
            lblStatus.Text = "Done";

            try
            {
                var args = e.Result as object[];
                var jobType = (JobType)args[0];
                invoiceObjs = args[1] as List<Invoice>;
                dataGridView1.DataSource = invoiceObjs;

                if (jobType == JobType.ExportPdf)
                {
                    var countPdfSuccess = invoiceObjs.Count(q => q.DownloadStatus == "Success");
                    var countPdfNotFound = invoiceObjs.Count(q => q.DownloadStatus == "Not Found");

                    lblTotal.Text = string.Format("Total {0} rec. | Download {1} Success/ {2} NotFound",
                        invoiceObjs.Count.ToString("N0"),
                        countPdfSuccess.ToString("N2"),
                        countPdfNotFound.ToString("N2"));
                }
                else if (jobType == JobType.CheckMaterial)
                {
                    var countNotFound = invoiceObjs.Count(q => q.MaterialType == "");
                    lblTotal.Text = string.Format("Total {0} rec. | NotFound {1} rec.",
                        invoiceObjs.Count.ToString("N0"),
                        countNotFound.ToString("N0"));
                }
                else
                    lblTotal.Text = string.Format("Total {0} rec.", invoiceObjs.Count.ToString("N0"));
            }
            catch { }
        }

        private void btnVerify_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
                return;

            var args = btnSearch.Tag as object[];

            backgroundWorker1.RunWorkerAsync(new object[] {
                JobType.CheckDocStatus,
                (EnumShipment)args[0], args[1], invoiceObjs });

            progressBar1.Style = ProgressBarStyle.Marquee;
            lblTotal.Text = "";
            lblStatus.Text = "check status...";
            dataGridView1.DataSource = null;
        }

        private void btnCheckRawMatch_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
                return;

            var args = btnSearch.Tag as object[];
            var shipmentType = (EnumShipment)args[0];

            if (shipmentType == EnumShipment.EXPORT)
                return;

            backgroundWorker1.RunWorkerAsync(new object[] {
                JobType.CheckMaterial,
                (EnumShipment)args[0], args[1], invoiceObjs });

            progressBar1.Style = ProgressBarStyle.Marquee;
            lblTotal.Text = "";
            lblStatus.Text = "check material...";
            dataGridView1.DataSource = null;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
                return;

            var args = btnSearch.Tag as object[];
            var shipmentType = (EnumShipment)args[0];

            if (shipmentType == EnumShipment.IMPORT)
            {
                var countMaterial = invoiceObjs.Count(q => q.MaterialType == "");

                if (countMaterial > 0)
                {
                    MessageBox.Show("ข้อมูลประเภท Material ยังไม่สมบูรณ์ กรุณาตรวจสอบ!!!");
                    return;
                }
            }

            var dlg = folderBrowserDialog1.ShowDialog();

            if (dlg == DialogResult.OK)
            {
                backgroundWorker1.RunWorkerAsync(new object[] {
                JobType.ExportPdf,
                (EnumShipment)args[0], args[1], invoiceObjs, folderBrowserDialog1.SelectedPath });

                progressBar1.Style = ProgressBarStyle.Marquee;
                lblTotal.Text = "";
                lblStatus.Text = "Export Pdf...";
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var notFoundItems = invoiceObjs.Where(q => q.MaterialType == "").ToList();

            if (notFoundItems == null || notFoundItems.Count == 0)
                return;

            var dlg = folderBrowserDialog1.ShowDialog();

            if (dlg == DialogResult.OK)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < notFoundItems.Count; i++)
                {
                    var item = notFoundItems[i];
                    sb.AppendLine(string.Format("{0}\t {1}", item.DecNo, item.InvoiceNo));
                }

                File.WriteAllText(Path.Combine(folderBrowserDialog1.SelectedPath, "NotFound.txt"), sb.ToString());

                MessageBox.Show("Export NotFound Record Success.");
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var notFoundItems = invoiceObjs.Where(q => q.DownloadStatus == "Not Found").ToList();

            if (notFoundItems == null || notFoundItems.Count == 0)
                return;

            var dlg = folderBrowserDialog1.ShowDialog();

            if (dlg == DialogResult.OK)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < notFoundItems.Count; i++)
                {
                    var item = notFoundItems[i];
                    sb.AppendLine(string.Format("{0}\t {1}", item.DecNo, item.InvoiceNo));
                }

                File.WriteAllText(Path.Combine(folderBrowserDialog1.SelectedPath, "DownloadUnComplete.txt"), sb.ToString());

                MessageBox.Show("Export UnComplete Record Success.");
            }
        }
    }

    public enum JobType
    {
        InvoiceQuery,
        CheckDocStatus,
        CheckMaterial,
        ExportPdf
    }
}