using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Timers;
using System.Xml;

namespace StockMarketNSC
{
    public partial class Service1 : ServiceBase
    {
        HttpClient client = new HttpClient();
        DataSet myDataSet = new DataSet();
        Timer timer = new Timer();
        private string _filePath = ConfigurationManager.AppSettings["FilePath"];
        private string _nIFTY = ConfigurationManager.AppSettings["NIFTY"];
        private string _bANKNIFTY = ConfigurationManager.AppSettings["BANKNIFTY"];
        private string _saveNifty = ConfigurationManager.AppSettings["SaveNifty"];
        private string _saveBankNifty = ConfigurationManager.AppSettings["SaveBankNifty"];
        private string _connectionString = ConfigurationManager.AppSettings["ConnectionString"];
        private string _servicePath = ConfigurationManager.AppSettings["ServicePath"];

        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        //  Run service every minute from start
        protected override void OnStart(string[] args)
        {
            WriteToFile("Service Start at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 1000;
            timer.Enabled = true;
            timer.Interval = (60 * 2000);
            timer.Enabled = true;
            timer.Elapsed += new ElapsedEventHandler(Click);
        }


        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
        }

        public void WriteToFile(string Message)
        {
            string path = _servicePath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = _filePath + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        public async void Click(object sourece, ElapsedEventArgs e)
        {
            try
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = await client.GetAsync(_nIFTY);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject o = JObject.Parse(responseBody);
                XmlDocument xd1 = (XmlDocument)JsonConvert.DeserializeXmlNode(responseBody, "records");
                xd1.Save(_saveNifty);
                myDataSet.ReadXml(_saveNifty);
                NiftyHeader();
                NiftyPE();
                NiftyCE();
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            try
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (HttpResponseMessage response = await client.GetAsync(_bANKNIFTY))
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject o = JObject.Parse(responseBody);
                    XmlDocument xd1 = (XmlDocument)JsonConvert.DeserializeXmlNode(responseBody, "records");
                    xd1.Save(_saveBankNifty);
                    myDataSet.ReadXml(_saveBankNifty);
                    BankNiftyHeader();
                    BankNiftyPE();
                    BankNiftyCE();
                    BankNiftyIndex();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        #region Nifty
        //  Insert data into NiftyHeader table
        public void NiftyHeader()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["data"];
            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from NiftyHeader";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();
            foreach (DataRow row in data.Rows)
            {
                string strikePrice = Convert.ToString(row["strikePrice"]) != "" ? Convert.ToString(row["strikePrice"]) : "";
                string expiryDate = Convert.ToString(row["expiryDate"]) ?? "";
                con.Open();
                string insQuery = @"insert into NiftyHeader 
                                                    (
                                                    batch_id,StrikePrice,ExpiryDate,[Date],[Time]) Values
                                                   (" + USRole + ",'" + strikePrice + "', '" + expiryDate + "','" + DateTime.Now + "','" + DateTime.Now.TimeOfDay + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }

        //  Insert data into NiftyPE table
        public void NiftyPE()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["PE"];

            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from NiftyPE";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();

            foreach (DataRow row in data.Rows)
            {
                string strikePrice = Convert.ToString(row["strikePrice"]) != "" ? Convert.ToString(row["strikePrice"]) : "";
                string expiryDate = Convert.ToString(row["expiryDate"]) ?? "";
                string underlying = Convert.ToString(row["underlying"]) ?? "";
                string identifier = Convert.ToString(row["identifier"]) ?? "";
                string openInterest = Convert.ToString(row["openInterest"]) != "" ? Convert.ToString(row["openInterest"]) : "";
                string changeinOpenInterest = Convert.ToString(row["changeinOpenInterest"]) != "" ? Convert.ToString(row["changeinOpenInterest"]) : "";
                string pchangeinOpenInterest = Convert.ToString(row["pchangeinOpenInterest"]) != "" ? Convert.ToString(row["pchangeinOpenInterest"]) : "";
                string totalTradedVolume = Convert.ToString(row["totalTradedVolume"]) != "" ? Convert.ToString(row["totalTradedVolume"]) : "";
                string impliedVolatility = Convert.ToString(row["impliedVolatility"]) != "" ? Convert.ToString(row["impliedVolatility"]) : "";
                string lastPrice = Convert.ToString(row["lastPrice"]) != "" ? Convert.ToString(row["lastPrice"]) : "";
                string change = Convert.ToString(row["change"]) != "" ? Convert.ToString(row["change"]) : "";
                string pChange = Convert.ToString(row["pChange"]) != "" ? Convert.ToString(row["pChange"]) : "";
                string totalBuyQuantity = Convert.ToString(row["totalBuyQuantity"]) != "" ? Convert.ToString(row["totalBuyQuantity"]) : "";
                string totalSellQuantity = Convert.ToString(row["totalSellQuantity"]) != "" ? Convert.ToString(row["totalSellQuantity"]) : "";
                string bidQty = Convert.ToString(row["bidQty"]) != "" ? Convert.ToString(row["bidQty"]) : "";
                string bidPrice = Convert.ToString(row["bidPrice"]) != "" ? Convert.ToString(row["bidPrice"]) : "";
                string askQty = Convert.ToString(row["askQty"]) != "" ? Convert.ToString(row["askQty"]) : "";
                string askPrice = Convert.ToString(row["askPrice"]) != "" ? Convert.ToString(row["askPrice"]) : "";
                string underlyingValue = Convert.ToString(row["underlyingValue"]) != "" ? Convert.ToString(row["underlyingValue"]) : "";
                string data_Id = Convert.ToString(row["data_Id"]) != "" ? Convert.ToString(row["data_Id"]) : "";
                string totOI = Convert.ToString(row["totOI"]) != "" ? Convert.ToString(row["totOI"]) : "";
                string totVol = Convert.ToString(row["totVol"]) != "" ? Convert.ToString(row["totVol"]) : "";
                string filtered_Id = Convert.ToString(row["filtered_Id"]) != "" ? Convert.ToString(row["filtered_Id"]) : "";

                con.Open();
                string insQuery = @"insert into NiftyPE 
                                                    (
                                                    batch_id,strikePrice,expiryDate,underlying,identifier,openInterest,changeinOpenInterest,pchangeinOpenInterest,
                                                    totalTradedVolume,impliedVolatility,lastPrice,change,pChange,totalBuyQuantity,totalSellQuantity,bidQty,bidPrice,
                                                    askQty,askPrice,underlyingValue,data_Id,totOI,totVol,filtered_Id) Values
                                                    (" + USRole + ",'" + strikePrice + "', '" + expiryDate + "','" + underlying + "','" + identifier + "'," +
                                                    "'" + openInterest + "','" + changeinOpenInterest + "','" + pchangeinOpenInterest + "'," +
                                                    "'" + totalTradedVolume + "','" + impliedVolatility + "','" + lastPrice + "'" +
                                                    ",'" + change + "','" + pChange + "','" + totalBuyQuantity + "','" + totalSellQuantity + "'," +
                                                    "'" + bidQty + "','" + bidPrice + "','" + askQty + "','" + askPrice + "','" + underlyingValue + "'," +
                                                    "'" + data_Id + "','" + totOI + "','" + totVol + "','" + filtered_Id + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }

        //  Insert data into NiftyCE table
        public void NiftyCE()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["CE"];

            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from NiftyCE";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();

            foreach (DataRow row in data.Rows)
            {
                string strikePrice = Convert.ToString(row["strikePrice"]) != "" ? Convert.ToString(row["strikePrice"]) : "";
                string expiryDate = Convert.ToString(row["expiryDate"]) ?? "";
                string underlying = Convert.ToString(row["underlying"]) ?? "";
                string identifier = Convert.ToString(row["identifier"]) ?? "";
                string openInterest = Convert.ToString(row["openInterest"]) != "" ? Convert.ToString(row["openInterest"]) : "";
                string changeinOpenInterest = Convert.ToString(row["changeinOpenInterest"]) != "" ? Convert.ToString(row["changeinOpenInterest"]) : "";
                string pchangeinOpenInterest = Convert.ToString(row["pchangeinOpenInterest"]) != "" ? Convert.ToString(row["pchangeinOpenInterest"]) : "";
                string totalTradedVolume = Convert.ToString(row["totalTradedVolume"]) != "" ? Convert.ToString(row["totalTradedVolume"]) : "";
                string impliedVolatility = Convert.ToString(row["impliedVolatility"]) != "" ? Convert.ToString(row["impliedVolatility"]) : "";
                string lastPrice = Convert.ToString(row["lastPrice"]) != "" ? Convert.ToString(row["lastPrice"]) : "";
                string change = Convert.ToString(row["change"]) != "" ? Convert.ToString(row["change"]) : "";
                string pChange = Convert.ToString(row["pChange"]) != "" ? Convert.ToString(row["pChange"]) : "";
                string totalBuyQuantity = Convert.ToString(row["totalBuyQuantity"]) != "" ? Convert.ToString(row["totalBuyQuantity"]) : "";
                string totalSellQuantity = Convert.ToString(row["totalSellQuantity"]) != "" ? Convert.ToString(row["totalSellQuantity"]) : "";
                string bidQty = Convert.ToString(row["bidQty"]) != "" ? Convert.ToString(row["bidQty"]) : "";
                string bidPrice = Convert.ToString(row["bidprice"]) != "" ? Convert.ToString(row["bidprice"]) : "";
                string askQty = Convert.ToString(row["askQty"]) != "" ? Convert.ToString(row["askQty"]) : "";
                string askPrice = Convert.ToString(row["askPrice"]) != "" ? Convert.ToString(row["askPrice"]) : "";
                string underlyingValue = Convert.ToString(row["underlyingValue"]) != "" ? Convert.ToString(row["underlyingValue"]) : "";
                string data_Id = Convert.ToString(row["data_Id"]) != "" ? Convert.ToString(row["data_Id"]) : "";
                string totOI = Convert.ToString(row["totOI"]) != "" ? Convert.ToString(row["totOI"]) : "";
                string totVol = Convert.ToString(row["totVol"]) != "" ? Convert.ToString(row["totVol"]) : "";
                string filtered_Id = Convert.ToString(row["filtered_Id"]) != "" ? Convert.ToString(row["filtered_Id"]) : "";
                con.Open();
                string insQuery = @"insert into NiftyCE 
                                                    (
                                                    batch_id,strikePrice,expiryDate,underlying,identifier,openInterest,changeinOpenInterest,pchangeinOpenInterest,
                                                    totalTradedVolume,impliedVolatility,lastPrice,change,pChange,totalBuyQuantity,totalSellQuantity,bidQty,bidPrice,
                                                    askQty,askPrice,underlyingValue,data_Id,totOI,totVol,filtered_Id) Values
                                                    (" + USRole + ",'" + strikePrice + "', '" + expiryDate + "','" + underlying + "','" + identifier + "'," +
                                                    "'" + openInterest + "','" + changeinOpenInterest + "','" + pchangeinOpenInterest + "'," +
                                                    "'" + totalTradedVolume + "','" + impliedVolatility + "','" + lastPrice + "'" +
                                                    ",'" + change + "','" + pChange + "','" + totalBuyQuantity + "','" + totalSellQuantity + "'," +
                                                    "'" + bidQty + "','" + bidPrice + "','" + askQty + "','" + askPrice + "','" + underlyingValue + "'," +
                                                    "'" + data_Id + "','" + totOI + "','" + totVol + "','" + filtered_Id + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }
        #endregion

        #region BankNifty
        //  Insert data into BankNiftyHeader table
        public void BankNiftyHeader()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["data"];
            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from BankNiftyHeader";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();
            foreach (DataRow row in data.Rows)
            {
                string strikePrice = Convert.ToString(row["strikePrice"]) != "" ? Convert.ToString(row["strikePrice"]) : "";
                string expiryDate = Convert.ToString(row["expiryDate"]) ?? "";
                con.Open(); string insQuery = @"insert into BankNiftyHeader 
                                                    (
                                                    batch_id,StrikePrice,ExpiryDate,[Date],[Time]) Values
                                                    (" + USRole + ",'" + strikePrice + "', '" + expiryDate + "','" + DateTime.Now + "','" + DateTime.Now.TimeOfDay + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }

        //  Insert Data into BankNiftyPE table
        public void BankNiftyPE()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["PE"];
            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from BankNiftyPE";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();
            foreach (DataRow row in data.Rows)
            {
                string strikePrice = Convert.ToString(row["strikePrice"]) != "" ? Convert.ToString(row["strikePrice"]) : "";
                string expiryDate = Convert.ToString(row["expiryDate"]) ?? "";
                string underlying = Convert.ToString(row["underlying"]) ?? "";
                string identifier = Convert.ToString(row["identifier"]) ?? "";
                string openInterest = Convert.ToString(row["openInterest"]) != "" ? Convert.ToString(row["openInterest"]) : "";
                string changeinOpenInterest = Convert.ToString(row["changeinOpenInterest"]) != "" ? Convert.ToString(row["changeinOpenInterest"]) : "";
                string pchangeinOpenInterest = Convert.ToString(row["pchangeinOpenInterest"]) != "" ? Convert.ToString(row["pchangeinOpenInterest"]) : "";
                string totalTradedVolume = Convert.ToString(row["totalTradedVolume"]) != "" ? Convert.ToString(row["totalTradedVolume"]) : "";
                string impliedVolatility = Convert.ToString(row["impliedVolatility"]) != "" ? Convert.ToString(row["impliedVolatility"]) : "";
                string lastPrice = Convert.ToString(row["lastPrice"]) != "" ? Convert.ToString(row["lastPrice"]) : "";
                string change = Convert.ToString(row["change"]) != "" ? Convert.ToString(row["change"]) : "";
                string pChange = Convert.ToString(row["pChange"]) != "" ? Convert.ToString(row["pChange"]) : "";
                string totalBuyQuantity = Convert.ToString(row["totalBuyQuantity"]) != "" ? Convert.ToString(row["totalBuyQuantity"]) : "";
                string totalSellQuantity = Convert.ToString(row["totalSellQuantity"]) != "" ? Convert.ToString(row["totalSellQuantity"]) : "";
                string bidQty = Convert.ToString(row["bidQty"]) != "" ? Convert.ToString(row["bidQty"]) : "";
                string bidPrice = Convert.ToString(row["bidPrice"]) != "" ? Convert.ToString(row["bidPrice"]) : "";
                string askQty = Convert.ToString(row["askQty"]) != "" ? Convert.ToString(row["askQty"]) : "";
                string askPrice = Convert.ToString(row["askPrice"]) != "" ? Convert.ToString(row["askPrice"]) : "";
                string underlyingValue = Convert.ToString(row["underlyingValue"]) != "" ? Convert.ToString(row["underlyingValue"]) : "";
                string data_Id = Convert.ToString(row["data_Id"]) != "" ? Convert.ToString(row["data_Id"]) : "";
                string totOI = Convert.ToString(row["totOI"]) != "" ? Convert.ToString(row["totOI"]) : "";
                string totVol = Convert.ToString(row["totVol"]) != "" ? Convert.ToString(row["totVol"]) : "";
                string filtered_Id = Convert.ToString(row["filtered_Id"]) != "" ? Convert.ToString(row["filtered_Id"]) : "";
                con.Open();
                string insQuery = @"insert into BankNiftyPE
                                                    (
                                                    batch_id,strikePrice,expiryDate,underlying,identifier,openInterest,changeinOpenInterest,pchangeinOpenInterest,
                                                    totalTradedVolume,impliedVolatility,lastPrice,change,pChange,totalBuyQuantity,totalSellQuantity,bidQty,bidPrice,
                                                    askQty,askPrice,underlyingValue,data_Id,totOI,totVol,filtered_Id) Values
                                                    (" + USRole + ",'" + strikePrice + "', '" + expiryDate + "','" + underlying + "','" + identifier + "'," +
                                                    "'" + openInterest + "','" + changeinOpenInterest + "','" + pchangeinOpenInterest + "'," +
                                                    "'" + totalTradedVolume + "','" + impliedVolatility + "','" + lastPrice + "'" +
                                                    ",'" + change + "','" + pChange + "','" + totalBuyQuantity + "','" + totalSellQuantity + "'," +
                                                    "'" + bidQty + "','" + bidPrice + "','" + askQty + "','" + askPrice + "','" + underlyingValue + "'," +
                                                    "'" + data_Id + "','" + totOI + "','" + totVol + "','" + filtered_Id + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }

        //  Insert data into BankNiftyCE table
        public void BankNiftyCE()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["CE"];
            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from BankNiftyCE";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();
            foreach (DataRow row in data.Rows)
            {
                string strikePrice = Convert.ToString(row["strikePrice"]) != "" ? Convert.ToString(row["strikePrice"]) : "";
                string expiryDate = Convert.ToString(row["expiryDate"]) ?? "";
                string underlying = Convert.ToString(row["underlying"]) ?? "";
                string identifier = Convert.ToString(row["identifier"]) ?? "";
                string openInterest = Convert.ToString(row["openInterest"]) != "" ? Convert.ToString(row["openInterest"]) : "";
                string changeinOpenInterest = Convert.ToString(row["changeinOpenInterest"]) != "" ? Convert.ToString(row["changeinOpenInterest"]) : "";
                string pchangeinOpenInterest = Convert.ToString(row["pchangeinOpenInterest"]) != "" ? Convert.ToString(row["pchangeinOpenInterest"]) : "";
                string totalTradedVolume = Convert.ToString(row["totalTradedVolume"]) != "" ? Convert.ToString(row["totalTradedVolume"]) : "";
                string impliedVolatility = Convert.ToString(row["impliedVolatility"]) != "" ? Convert.ToString(row["impliedVolatility"]) : "";
                string lastPrice = Convert.ToString(row["lastPrice"]) != "" ? Convert.ToString(row["lastPrice"]) : "";
                string change = Convert.ToString(row["change"]) != "" ? Convert.ToString(row["change"]) : "";
                string pChange = Convert.ToString(row["pChange"]) != "" ? Convert.ToString(row["pChange"]) : "";
                string totalBuyQuantity = Convert.ToString(row["totalBuyQuantity"]) != "" ? Convert.ToString(row["totalBuyQuantity"]) : "";
                string totalSellQuantity = Convert.ToString(row["totalSellQuantity"]) != "" ? Convert.ToString(row["totalSellQuantity"]) : "";
                string bidQty = Convert.ToString(row["bidQty"]) != "" ? Convert.ToString(row["bidQty"]) : "";
                string bidPrice = Convert.ToString(row["bidprice"]) != "" ? Convert.ToString(row["bidprice"]) : "";
                string askQty = Convert.ToString(row["askQty"]) != "" ? Convert.ToString(row["askQty"]) : "";
                string askPrice = Convert.ToString(row["askPrice"]) != "" ? Convert.ToString(row["askPrice"]) : "";
                string underlyingValue = Convert.ToString(row["underlyingValue"]) != "" ? Convert.ToString(row["underlyingValue"]) : "";
                string data_Id = Convert.ToString(row["data_Id"]) != "" ? Convert.ToString(row["data_Id"]) : "";
                string totOI = Convert.ToString(row["totOI"]) != "" ? Convert.ToString(row["totOI"]) : "";
                string totVol = Convert.ToString(row["totVol"]) != "" ? Convert.ToString(row["totVol"]) : "";
                string filtered_Id = Convert.ToString(row["filtered_Id"]) != "" ? Convert.ToString(row["filtered_Id"]) : "";
                con.Open();
                string insQuery = @"insert into BankNiftyCE 
                                                    (
                                                    batch_id,strikePrice,expiryDate,underlying,identifier,openInterest,changeinOpenInterest,pchangeinOpenInterest,
                                                    totalTradedVolume,impliedVolatility,lastPrice,change,pChange,totalBuyQuantity,totalSellQuantity,bidQty,bidPrice,
                                                    askQty,askPrice,underlyingValue,data_Id,totOI,totVol,filtered_Id) Values
                                                    (" + USRole + ",'" + strikePrice + "', '" + expiryDate + "','" + underlying + "','" + identifier + "'," +
                                                    "'" + openInterest + "','" + changeinOpenInterest + "','" + pchangeinOpenInterest + "'," +
                                                    "'" + totalTradedVolume + "','" + impliedVolatility + "','" + lastPrice + "'" +
                                                    ",'" + change + "','" + pChange + "','" + totalBuyQuantity + "','" + totalSellQuantity + "'," +
                                                    "'" + bidQty + "','" + bidPrice + "','" + askQty + "','" + askPrice + "','" + underlyingValue + "'," +
                                                    "'" + data_Id + "','" + totOI + "','" + totVol + "','" + filtered_Id + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }

        //  Insert data into BankNiftyIndex table
        public void BankNiftyIndex()
        {
            string ConnectionString = _connectionString;
            SqlConnection con = new SqlConnection(ConnectionString);
            DataTable data = myDataSet.Tables["index"];
            con.Open();
            string batch = @"select isnull(MAX(batch_Id),0) from BankNiftyIndex";
            SqlCommand myCommand1 = new SqlCommand(batch, con);
            myCommand1.ExecuteNonQuery();
            int USRole = (Int32)myCommand1.ExecuteScalar() + 1;
            con.Close();
            foreach (DataRow row in data.Rows)
            {
                string key = Convert.ToString(row["key"]) != "" ? Convert.ToString(row["key"]) : "";
                string index_Id = Convert.ToString(row["index_Id"]) != "" ? Convert.ToString(row["index_Id"]) : "";
                string indexSymbol = Convert.ToString(row["indexSymbol"]) != "" ? Convert.ToString(row["indexSymbol"]) : "";
                string last = Convert.ToString(row["last"]) != "" ? Convert.ToString(row["last"]) : "";
                string variation = Convert.ToString(row["variation"]) != "" ? Convert.ToString(row["variation"]) : "";
                string percentChange = Convert.ToString(row["percentChange"]) != "" ? Convert.ToString(row["percentChange"]) : "";
                string open = Convert.ToString(row["open"]) != "" ? Convert.ToString(row["open"]) : "";
                string high = Convert.ToString(row["high"]) != "" ? Convert.ToString(row["high"]) : "";
                string low = Convert.ToString(row["low"]) != "" ? Convert.ToString(row["low"]) : "";
                string previousClose = Convert.ToString(row["previousClose"]) != "" ? Convert.ToString(row["previousClose"]) : "";
                string yearHigh = Convert.ToString(row["yearHigh"]) != "" ? Convert.ToString(row["yearHigh"]) : "";
                string yearLow = Convert.ToString(row["yearLow"]) != "" ? Convert.ToString(row["yearLow"]) : "";
                string pe = Convert.ToString(row["pe"]) != "" ? Convert.ToString(row["pe"]) : "";
                string pb = Convert.ToString(row["pb"]) != "" ? Convert.ToString(row["pb"]) : "";
                string dy = Convert.ToString(row["dy"]) != "" ? Convert.ToString(row["dy"]) : "";
                string declines = Convert.ToString(row["declines"]) != "" ? Convert.ToString(row["declines"]) : "";
                string advances = Convert.ToString(row["advances"]) != "" ? Convert.ToString(row["advances"]) : "";
                string unchanged = Convert.ToString(row["unchanged"]) != "" ? Convert.ToString(row["unchanged"]) : "";
                string index_Id_0 = Convert.ToString(row["index_Id_0"]) != "" ? Convert.ToString(row["index_Id_0"]) : "";
                string records_Id = Convert.ToString(row["records_Id"]) != "" ? Convert.ToString(row["records_Id"]) : "";
                con.Open();
                string insQuery = @"insert into BankNiftyIndex 
                                                    (
                                                    batch_id,[key],index_Id,IndexSymbol,[last],variation,percentChange,[open],high,low,previousClose,yearHigh,
                                                    yearLow,pe,pb,dy,declines,advances,unchanged,index_Id_0,records_Id) Values
                                                    (" + USRole + ",'" + key + "', '" + index_Id + "','" + indexSymbol + "','" + last + "','" + variation + "'," +
                                                    "'" + percentChange + "','" + open + "','" + high + "','" + low + "','" + previousClose + "'" +
                                                    ",'" + yearHigh + "','" + yearLow + "','" + pe + "','" + pb + "','" + dy + "','" + declines + "'," +
                                                    "'" + advances + "','" + unchanged + "','" + index_Id_0 + "','" + records_Id + "')";
                SqlCommand myCommand = new SqlCommand(insQuery, con);
                myCommand.ExecuteNonQuery();
                con.Close();
            }
        }
        #endregion

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }
    }
}