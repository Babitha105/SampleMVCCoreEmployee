using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using SampleMVCCoreEmployee.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SampleMVCCoreEmployee.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpAPIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public EmpAPIController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        [HttpGet]
        public async Task<List<VEmployeeModel>> GetEmployee()
        {
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
            return await Task.FromResult(emplList);
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

        //// GET: api/<EmpAPIController>
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET api/<EmpAPIController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<EmpAPIController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<EmpAPIController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<EmpAPIController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
