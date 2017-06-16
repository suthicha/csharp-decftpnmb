using System;

namespace DecFtpNmb.Models
{
    public class Invoice
    {
        public string CmpCd { get; set; }
        public string DivCd { get; set; }
        public string TradCd { get; set; }
        public string InvoiceNo { get; set; }
        public string DecNo { get; set; }
        public DateTime ClearDate { get; set; }
        public string DownloadStatus { get; set; }
        public string MaterialType { get; set; }
    }
}