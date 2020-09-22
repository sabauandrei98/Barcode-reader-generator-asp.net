using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using System.Data;
using System.Data.SqlClient;
using ThoughtWorks.QRCode.Codec;
using ThoughtWorks.QRCode.Codec.Data;
using System.Text;
using ZXing.Common;

namespace BarcodeScanner.Controllers
{

    public class FileInfo
    {
        public FileInfo(string Path, string ShortPath, string Message)
        {
            path = Path;
            shortPath = ShortPath;
            message = Message;
        }

        public string path { get; }
        public string shortPath { get; }
        public string message { get; }
    }

    public class GenerateBarCodeController : Controller
    {
        SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\qrDataBase.mdf;Integrated Security=True");

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string barcode)
        {
            BarCode codeData = BarCode.GetData(barcode);
            if (!codeData.valid || codeData.quantity <= 0)
            {
                return View("Error");
            }
            
            if(!SqlValid(codeData))
            {
                return View("Error");
            }
            Encode(codeData);
            DecodeGeneratedCodes();

            return View();
        }

        private void Encode(BarCode data)
        {
            MessagingToolkit.QRCode.Codec.QRCodeEncoder encoder = new MessagingToolkit.QRCode.Codec.QRCodeEncoder();
            encoder.QRCodeScale = 10;

            var bitmap = encoder.Encode(data.code + " " + data.quantity);
            Image generatedImage = (Image)bitmap;

            string currentTime = DateTime.Now.Year.ToString() + "." + DateTime.Now.Month.ToString() + "." + DateTime.Now.Day.ToString() + " " +
                                 DateTime.Now.Hour.ToString() + "." + DateTime.Now.Minute.ToString() + "." + DateTime.Now.Second.ToString();
            generatedImage.Save(Server.MapPath("~/GeneratedImages/generated_qr" + currentTime + ".jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);
            ViewBag.GeneratedQrImagePath = "~/GeneratedImages/generated_qr" + currentTime + ".jpg";
        }

        private bool SqlValid(BarCode data)
        {
            string dbCommand = "";

            if (!CheckIfRecordExists(data.code))
            {
                dbCommand = "INSERT INTO Produse VALUES('" + data.code + "', '" + data.quantity.ToString() + "')";
            }

            if (!ExecuteDbCommand(dbCommand))
            {
                return false;
            }

            return true;
        }


        private bool CheckIfRecordExists(string code)
        {
            try
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                con.Open();

                var cmd = new SqlCommand
                {
                    CommandText = "SELECT COUNT(*) FROM Produse WHERE ([codProdus])=@cod ",
                    Connection = con

                };
                cmd.Parameters.AddWithValue("@cod", code);
                int recordExists = (int)cmd.ExecuteScalar();

                if (recordExists > 0)
                    return true;
            }
            catch
            {
                if (con.State == ConnectionState.Open)
                    con.Close();

                return true;
            }

            if (con.State == ConnectionState.Open)
                con.Close();

            return false;
        }


        private void DecodeGeneratedCodes()
        {
            const string path = "~/GeneratedImages/";
            List<FileInfo> fileInfo = new List<FileInfo>();

            foreach (string fileName in Directory.GetFiles(Server.MapPath(path)))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    using (Image original = Image.FromStream(fs))
                    {
                        try
                        {
                            // create a barcode reader instance
                            IBarcodeReader reader = new BarcodeReader();
                            // load a bitmap
                            var barcodeBitmap = (original as Bitmap);
                            // detect and decode the barcode inside the bitmap
                            var result = reader.Decode(barcodeBitmap);
                            // do something with the result

                            if (result != null)
                            {
                                string[] file = fileName.Split('\\');
                                FileInfo f = new FileInfo(path + file[file.Length - 1], getSimplifiedPath(fileName), "Message: " + result.Text);
                                fileInfo.Add(f);
                            }
                            else
                            {
                                string[] file = fileName.Split('\\');
                                FileInfo f = new FileInfo(path + file[file.Length - 1], getSimplifiedPath(fileName), "Error");
                                fileInfo.Add(f);
                            }
                        }
                        catch
                        {
                            string[] file = fileName.Split('\\');
                            FileInfo f = new FileInfo(path + file[file.Length - 1], getSimplifiedPath(fileName), "Error");
                            fileInfo.Add(f);
                        }
                    }
                }
            }
            ViewBag.FileInfo = fileInfo;
        }

        private bool ExecuteDbCommand(string command)
        {
            try
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                con.Open();

                SqlCommand cmd = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = command;
                cmd.ExecuteNonQuery();

                con.Close();
            }
            catch
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                return false;
            }

            if (con.State == ConnectionState.Open)
                con.Close();

            return true;
        }


        private string getSimplifiedPath(string path)
        {
            string newPath = "";
            int maxPathLen = 60;
            int startPath = path.Length - maxPathLen;

            if (startPath < 0)
                startPath = 0;

            for (int i = startPath; i < path.Length; i++)
            {
                newPath += path[i];
            }

            return newPath;
        }

    }
}