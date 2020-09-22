using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZXing;
using System.Drawing.Imaging;
using System.Drawing;
using System.Web.UI;
using System.Data;
using System.Data.SqlClient;
using MessagingToolkit.QRCode.Codec;
using MessagingToolkit.QRCode.Codec.Data;
using ZXing.Common;

namespace BarcodeScanner.Controllers
{
    public class WebCamScanBarCodeController : Controller
    {

        SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\qrDataBase.mdf;Integrated Security=True");

        [HttpGet]
        public ActionResult Index()
        {
            Session["val"] = "";
            return View();
        }

        public ActionResult Result()
        {
            string decoded = DecodeCapturedPhoto();
            if (decoded == "")
                return View("Error");

            BarCode barcode = BarCode.GetData(decoded);

            if (!barcode.valid)
                ViewBag.QrResult = barcode.code + " are un format invalid";
            else
            if (!CheckIfRecordExists(barcode.code, barcode.quantity))
                ViewBag.QrResult = barcode.code + " nu a putut fi gasit in baza de date";
            else
            if (!DeleteEntry(barcode.code, barcode.quantity))
                ViewBag.QrResult = barcode.code + " nu a putut fi sters din baza de date";
            else
                ViewBag.QrResult = barcode.code + " " + barcode.quantity + " a fost sters din baza de date";

            return View();
        }



        public JsonResult Rebind()
        {
            string path = "http://localhost:57801/WebImages/" + Session["val"].ToString();
            return Json(path, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Capture()
        {
            var stream = Request.InputStream;
            string dump;
            using (var reader = new StreamReader(stream))
            {
                dump = reader.ReadToEnd();
                var path = Server.MapPath("~/WebImages/captured_photo.jpg");
                System.IO.File.WriteAllBytes(path, String_To_Bytes2(dump));
                ViewData["path"] = "captured_photo.jpg";
                Session["val"] = "captured_photo.jpg";
            }
            return View("Result");
        }

        private bool DeleteEntry(string code, int quantity)
        {
            try
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                con.Open();

                SqlCommand cmd = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DELETE FROM Produse WHERE ([codProdus])=@codProdus AND ([cantitate])=@cantitate";
                    cmd.Parameters.AddWithValue("@codProdus", code);
                cmd.Parameters.AddWithValue("@cantitate", quantity);
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


        private string DecodeCapturedPhoto()
        {
            const string fileName = "~/WebImages/captured_photo.jpg";
            string output = "";

            using (FileStream fs = new FileStream(Server.MapPath(fileName), FileMode.Open, FileAccess.Read))
            {
                using (Image original = Image.FromStream(fs))
                {
                    try
                    {
                        IBarcodeReader reader = new BarcodeReader();
                        var barcodeBitmap = (original as Bitmap);
                        var result = reader.Decode(barcodeBitmap);

                        if (result != null)
                        {
                            output += "Message: " + result.Text;
                            return output;
                        }
                        else
                        {
                            return "";
                        }
                    }
                    catch
                    {
                        return "";
                    }
                }
            }
        }

        private bool CheckIfRecordExists(string code, int quantity)
        {
            try
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                con.Open();

                var cmd = new SqlCommand
                {
                    CommandText = "SELECT COUNT(*) FROM Produse WHERE ([codProdus])=@cod AND ([cantitate])=@cantitate ",
                    Connection = con

                };
                cmd.Parameters.AddWithValue("@cod", code);
                cmd.Parameters.AddWithValue("@cantitate", quantity);
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




        private byte[] String_To_Bytes2(string strInput)
        {
            int numBytes = (strInput.Length) / 2;
            byte[] bytes = new byte[numBytes];
            for (int x = 0; x < numBytes; ++x)
            {
                bytes[x] = Convert.ToByte(strInput.Substring(x * 2, 2), 16);
            }
            return bytes;
        }
    }
}