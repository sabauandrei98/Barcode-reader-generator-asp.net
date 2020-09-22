using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace BarcodeScanner.Controllers
{
    public class BarCode
    {
        public BarCode(string Code, int Quantity, bool Valid = true)
        {
            code = Code;
            quantity = Quantity;
            valid = Valid;
        }

        public int id { get; set; }
        public string code { get; set; }
        public int quantity { get; set; }

        public bool valid { get; set; }

        public override string ToString() => $"({code}, {quantity})";

        public static BarCode GetData(string barcode)
        {
            BarCode generatedCode = new BarCode(barcode, 0, false);

            if (barcode.Split(' ').Length != 2)
                return generatedCode;

            try
            {
                string code = barcode.Split(' ')[0];
                int quantity = int.Parse(barcode.Split(' ')[1]);

                generatedCode.code = code;
                generatedCode.quantity = quantity;
                generatedCode.valid = true;
            }
            catch
            {
                return generatedCode;
            }
            return generatedCode;
        }

    }


    public class DataBaseController : Controller
    {
        SqlConnection con = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\qrDataBase.mdf;Integrated Security=True");
        
        // GET: DataBase
        public ActionResult Index()
        {
            try
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                con.Open();


                var model = new List<BarCode>();
                SqlCommand cmd = con.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT * FROM Produse";
                cmd.ExecuteNonQuery();

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    BarCode bc = new BarCode("", 0);
                    bc.id = int.Parse(rdr["Id"].ToString());
                    bc.code = rdr["codProdus"].ToString();
                    bc.quantity = int.Parse(rdr["cantitate"].ToString());

                    model.Add(bc);
                }
                ViewBag.List = model;

                con.Close();
            }
            catch
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                return View("Error");
            }

            if (con.State == ConnectionState.Open)
                con.Close();

            return View();
        }


    }
}