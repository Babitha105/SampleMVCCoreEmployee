using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleMVCCoreEmployee.Models
{
    public class VEmployeeModel
    {       
        public int empid { get; set; }
        public string empname { get; set; }
        public string designation { get; set; }
        public decimal salary { get; set; }
        public DateTime joiningdate { get; set; }
    }
}
