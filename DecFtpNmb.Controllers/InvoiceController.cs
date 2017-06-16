using DecFtpNmb.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace DecFtpNmb.Controllers
{
    public class InvoiceController
    {
        private readonly string _sqlConnectionString;

        public InvoiceController(string connectionString)
        {
            this._sqlConnectionString = connectionString;
        }

        public List<Invoice> getInvoice(EnumShipment shipmentType, string fromdate, string todate)
        {
            List<Invoice> invoiceObjs = new List<Invoice>();

            try
            {
                string sqlText = string.Empty;

                if (shipmentType == EnumShipment.EXPORT)
                {
                    sqlText = @"select CompCd as CmpCd, TradCd as TrdCd, InvNo, DecNo, ClrDate as ClearDate,
                    (select top 1 IHDEPT from hd010 a where a.IHINVN=nmb02.InvNo) as DivCd
                    from nmb02
                    where ClrDate >= @fromdate
                    and ClrDate <= @todate
                    order by ClrDate asc";
                }
                else
                {
                    sqlText = @"select ImportCD as CmpCd, ImportDV as DivCd, InvNo, EntryNo as DecNo, ClearDate,
                    (select top 1 H5TRAD from hd050 a where a.H5INVN=nmb05.InvNo) as TrdCD
                    from nmb05
                    where ClearDate >= @fromdate
                    and ClearDate <= @todate
                    order by ClearDate asc";
                }

                var parms = new SqlParameter[2];
                parms[0] = new SqlParameter();
                parms[0].ParameterName = "@fromdate";
                parms[0].DbType = DbType.String;
                parms[0].Value = fromdate;

                parms[1] = new SqlParameter();
                parms[1].ParameterName = "@todate";
                parms[1].DbType = DbType.String;
                parms[1].Value = todate;

                var dsInvoice = getDataSet(sqlText, parms);

                foreach (DataRow dr in dsInvoice.Tables[0].Rows)
                {
                    invoiceObjs.Add(convertToInvoice(dr));
                }
            }
            catch { }

            return invoiceObjs;
        }

        private Invoice convertToInvoice(DataRow row)
        {
            return new Invoice
            {
                CmpCd = row["CmpCd"].ToString(),
                DivCd = row["DivCd"].ToString(),
                TradCd = row["TrdCd"].ToString(),
                InvoiceNo = row["InvNo"].ToString(),
                DecNo = row["DecNo"].ToString(),
                ClearDate = DateTime.ParseExact(row["ClearDate"].ToString(), "yyyyMMdd",
                new CultureInfo("en-US"))
            };
        }

        private DataSet getDataSet(string commandText, params SqlParameter[] parameters)
        {
            DataSet dsResult = null;

            using (SqlConnection conn = new SqlConnection(this._sqlConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = commandText;
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddRange(parameters);

                var adapter = new SqlDataAdapter(cmd);
                dsResult = new DataSet();
                adapter.Fill(dsResult);
            }

            return dsResult;
        }

        public void WriteIndex(string location, List<Invoice> data)
        {
            var saveToFileName = Path.Combine(location, "INDEX.xml");

            if (File.Exists(saveToFileName))
                File.Delete(saveToFileName);

            var xmlSetting = new XmlWriterSettings();
            xmlSetting.Indent = true;

            XmlWriter xmlWriter = XmlWriter.Create(saveToFileName, xmlSetting);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("DocumentElement");

            for (int i = 0; i < data.Count; i++)
            {
                if (data.ElementAt(i).DownloadStatus == "") continue;
                var inv = data.ElementAt(i);
                xmlWriter.WriteStartElement("NMBDATA");
                xmlWriter.WriteElementString("DecNo", inv.DecNo.TrimEnd());
                xmlWriter.WriteElementString("Inv", inv.InvoiceNo.TrimEnd());
                xmlWriter.WriteElementString("CompCd", inv.CmpCd.TrimEnd());
                xmlWriter.WriteElementString("Divi", inv.DivCd.TrimEnd());
                xmlWriter.WriteElementString("IHTRAD", inv.TradCd.TrimEnd());
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }
    }
}