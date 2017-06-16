using System.IO;
using System.Net;
using System.Net.FtpClient;

namespace DecFtpNmb.Controllers
{
    public class PdfController
    {
        private readonly string _ftpHost;
        private readonly string _ftpUser;
        private readonly string _ftpPass;

        public PdfController(string ftpHost, string ftpUser, string ftpPass)
        {
            this._ftpHost = ftpHost;
            this._ftpPass = ftpPass;
            this._ftpUser = ftpUser;
        }

        public void CreateFolderForMaterial(string destFolder)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(destFolder, "R"));
                Directory.CreateDirectory(Path.Combine(destFolder, "M"));
            }
            catch { }
        }

        public bool Download(string destFolder, string location, string decno)
        {
            var destinationFile = Path.Combine(destFolder, decno.TrimEnd() + ".pdf");

            using (var ftpClient = new FtpClient())
            {
                ftpClient.Host = _ftpHost;
                ftpClient.Credentials = new NetworkCredential(_ftpUser, _ftpPass);

                ftpClient.Connect();

                var uri = string.Format("{0}/{1}.pdf", location, decno.TrimEnd());

                try
                {
                    using (var ftpStream = ftpClient.OpenRead(uri))
                    using (var fileStream = File.Create(destinationFile, (int)ftpStream.Length))
                    {
                        var buffer = new byte[8 * 1024];
                        int count;
                        while ((count = ftpStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, count);
                        }
                    }
                }
                catch
                {
                    // MessageBox.Show("Find not found " + uri);
                }
            }

            if (File.Exists(destinationFile))
                return true;
            else
                return false;
        }
    }
}