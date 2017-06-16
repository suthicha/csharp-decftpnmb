using DecFtpNmb.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace DecFtpNmb.Controllers
{
    public class DocuShareController
    {
        private readonly string _sqlConnectionString;

        public DocuShareController(string connectionString)
        {
            this._sqlConnectionString = connectionString;
        }

        public List<DocMaterial> getDocMaterial(string fromdate, string todate)
        {
            List<DocMaterial> materialObjs = new List<DocMaterial>();

            try
            {
                var fromdateObj = DateTime.ParseExact(fromdate, "yyyyMMdd",
                new CultureInfo("en-US"));

                var prevdateObj = fromdateObj.AddMonths(-1);
                var parms = new SqlParameter[2];
                parms[0] = new SqlParameter();
                parms[0].ParameterName = "@fromdate";
                parms[0].DbType = DbType.String;
                parms[0].Value = prevdateObj.ToString("yyyyMMdd", new CultureInfo("en-US"));

                parms[1] = new SqlParameter();
                parms[1].ParameterName = "@todate";
                parms[1].DbType = DbType.String;
                parms[1].Value = todate;

                var dsMaterial = getDataSet(parms);

                foreach (DataRow dr in dsMaterial.Tables[0].Rows)
                {
                    materialObjs.Add(convertToDocMaterial(dr));
                }
            }
            catch { }

            return materialObjs;
        }

        private DocMaterial convertToDocMaterial(DataRow row)
        {
            return new DocMaterial
            {
                DecNo = row["import_decno"].ToString(),
                MaterialType = row["import_materialtype"].ToString()
            };
        }

        private DataSet getDataSet(params SqlParameter[] parameters)
        {
            DataSet dsResult = new DataSet();

            using (SqlConnection conn = new SqlConnection(this._sqlConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"select Import_DecNO, Import_MaterialType from DSObject_table
                where Import_UDateDeclare >= @fromdate
                and Import_UDateDeclare <= @todate";

                cmd.Parameters.AddRange(parameters);

                var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }

            return dsResult;
        }
    }
}