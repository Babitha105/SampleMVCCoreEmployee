using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using Microsoft.Extensions.Configuration;
using SampleMVCCoreEmployee.Models;
using System.IO;
using System.Reflection;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace SampleMVCCoreEmployee.Controllers
{
    public class VEmployeeController : Controller
    {
        // GET: VEmployeeController
        public ActionResult Index()
        {
            return View();
        }

        private readonly IConfiguration _configuration;

        private readonly IMemoryCache memoryCache;
        public VEmployeeController(IConfiguration configuration,IMemoryCache memoryCache)
        {
            this._configuration = configuration;
            this.memoryCache = memoryCache;
        }
        public ActionResult VEmployeeDetail()
        {
            ModelState.Clear();
           

            DataTable sqlDt = new DataTable();
            var emplList = new List<VEmployeeModel>();
            using (SqlConnection sqlConnection = new SqlConnection(_configuration.GetConnectionString("SQLAuth")))
            {
                sqlConnection.Open();
                SqlCommand sqlCmd = new SqlCommand("EmplDetails", sqlConnection);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter sqlDa = new SqlDataAdapter(sqlCmd);
                sqlCmd.Parameters.AddWithValue("Action", 1);
                sqlDa.Fill(sqlDt);
                emplList = ConvertDataTable<VEmployeeModel>(sqlDt);
            }

            //DateTime currentTime;
            bool isExist = memoryCache.TryGetValue("CacheTime", out emplList);

            if (!isExist)
            {
                //currentTime = DateTime.Now;
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(30));

                memoryCache.Set("CacheTime", emplList, cacheEntryOptions);
            }

            return View("VEmployeeDetail", emplList);
        }
        private static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }
        private static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name.ToLower().Equals(column.ColumnName.ToLower()))
                    {
                        object value = dr[pro.Name];
                        if (value == DBNull.Value)
                            value = null;
                        pro.SetValue(obj, value, null);
                    }
                    else
                        continue;
                }
            }
            return obj;
        }
        [Route("VEmployee/VEmployeeAddEdit")]
        public ActionResult VEmployeeAddEdit()
        {
            VEmployeeModel empModel = new VEmployeeModel();
            return View("VEmployeeAddEdit", empModel);
        }

        [HttpGet]
        public ActionResult EditEmployee(int id)
        {
            DataTable sqlDt = new DataTable();
            List<VEmployeeModel> emplList = new List<VEmployeeModel>();
            VEmployeeModel empModel = new VEmployeeModel();
            using (SqlConnection sqlConnection = new SqlConnection(_configuration.GetConnectionString("SQLAuth")))
            {
                sqlConnection.Open();
                SqlCommand sqlCmd = new SqlCommand("EmplDetails", sqlConnection);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter sqlDa = new SqlDataAdapter(sqlCmd);
                sqlCmd.Parameters.AddWithValue("empid", id);
                sqlCmd.Parameters.AddWithValue("Action", 5);
                sqlDa.Fill(sqlDt);
            }

            if (sqlDt.Rows.Count == 1)
            {
                empModel.empid = Convert.ToInt32(sqlDt.Rows[0]["empid"].ToString());
                empModel.empname = sqlDt.Rows[0]["empname"].ToString();
                empModel.designation = sqlDt.Rows[0]["designation"].ToString();
                empModel.salary = Convert.ToDecimal(sqlDt.Rows[0]["salary"].ToString());
                emplList.Add(empModel);
            }
            return View("VEmployeeAddEdit", empModel);
        }

        [HttpPost]
        [Route("VEmployee/VEmployeeAddEdit")]
        public ActionResult VEmployeeAddEdit(VEmployeeModel empModel)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection sqlConnection = new SqlConnection(_configuration.GetConnectionString("SQLAuth")))
                {
                    sqlConnection.Open();
                    SqlCommand sqlCmd = new SqlCommand("EmplDetails", sqlConnection);
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.Parameters.AddWithValue("empid", empModel.empid);
                    sqlCmd.Parameters.AddWithValue("empname", empModel.empname);
                    sqlCmd.Parameters.AddWithValue("designation", empModel.designation);
                    sqlCmd.Parameters.AddWithValue("salary", empModel.salary);
                    //sqlCmd.Parameters.AddWithValue("joiningdate", DateTime.Now);
                    if (empModel.empid == 0)
                    { sqlCmd.Parameters.AddWithValue("Action", 2); }
                    else
                    {

                        sqlCmd.Parameters.AddWithValue("Action", 3);
                    }

                    sqlCmd.Parameters.AddWithValue("Active", 1);
                    sqlCmd.ExecuteNonQuery();

                }

                return RedirectToAction(nameof(VEmployeeDetail));
            }
            return View("VEmployeeAddEdit", empModel);

            //TextWriter txtWriter = new StreamWriter("D:\\Project\\EmployeeText.txt");
            //txtWriter.Write(vEmployee.empname);
            //txtWriter.WriteLine();
            //txtWriter.Write(vEmployee.designation);
            //txtWriter.WriteLine();
            //txtWriter.Write(vEmployee.salary);
            //txtWriter.Close();

        }
        public ActionResult DeleteEmployee(int id)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_configuration.GetConnectionString("SQLAuth")))
            {
                sqlConnection.Open();
                SqlCommand sqlCmd = new SqlCommand("EmplDetails", sqlConnection);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.AddWithValue("empid", id);
                sqlCmd.Parameters.AddWithValue("Action", 4);
                sqlCmd.ExecuteNonQuery();
            }
            return RedirectToAction(nameof(VEmployeeDetail));
        }

        [HttpGet]

        [Route("VEmployee/EmpViewAPI")]
        public ActionResult GetEmplAPI()
        {
            IEnumerable<VEmployeeModel> emplList = null;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://localhost:44348/api/");
                //HTTP GET
                var responseTask = client.GetAsync("Employees");
                responseTask.Wait();
                    
                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsAsync<IList<VEmployeeModel>>();
                    readTask.Wait();

                    emplList = readTask.Result;
                }
                else //web api sent error response 
                {
                    //log response status here..

                    emplList = Enumerable.Empty<VEmployeeModel>();

                    ModelState.AddModelError(string.Empty, "Server error. Please contact administrator.");
                }
            }
            return View("EmpViewAPI",emplList);

        }



        //// GET: VEmployeeController/Details/5
        //public ActionResult Details(int id)
        //{
        //    return View();
        //}

        //// GET: VEmployeeController/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: VEmployeeController/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: VEmployeeController/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: VEmployeeController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: VEmployeeController/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: VEmployeeController/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Delete(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}
