using DecFtpNmb.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace DecFtpNmb.Controllers
{
    public class CustomsController
    {
        private readonly string _sqlConnectionString;

        public CustomsController(string connectionString)
        {
            this._sqlConnectionString = connectionString;
        }

        public List<Declaration> getDeclarations(EnumShipment shipmentType, string fromdate, string todate)
        {
            List<Declaration> declObjs = new List<Declaration>();

            try
            {
                string sqlText = string.Empty;

                if (shipmentType == EnumShipment.EXPORT)
                {
                    sqlText = @"select decno, docstatus from DecX_Declare
                    where UdateDeclare >= @fromdate and UDateDeclare <= @todate";
                }
                else
                {
                    sqlText = @"select decno, docstatus from DecI_Declare
                    where UdateDeclare >= @fromdate and UDateDeclare <= @todate";
                }

                var formdateObj = DateTime.ParseExact(fromdate, "yyyyMMdd",
                new CultureInfo("en-US"));

                var prevdateObj = formdateObj.AddMonths(-1);

                var parms = new SqlParameter[2];
                parms[0] = new SqlParameter();
                parms[0].ParameterName = "@fromdate";
                parms[0].DbType = DbType.String;
                parms[0].Value = prevdateObj.ToString("yyyyMMdd", new CultureInfo("en-US"));

                parms[1] = new SqlParameter();
                parms[1].ParameterName = "@todate";
                parms[1].DbType = DbType.String;
                parms[1].Value = todate;

                var dsDecl = getDataSet(sqlText, parms);

                foreach (DataRow dr in dsDecl.Tables[0].Rows)
                {
                    declObjs.Add(convertToDeclaration(dr));
                }
            }
            catch { }

            return declObjs;
        }

        private Declaration convertToDeclaration(DataRow row)
        {
            return new Declaration
            {
                DecNo = row["DecNo"].ToString(),
                DocStatus = row["DocStatus"].ToString()
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
    }
}