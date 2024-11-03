using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KonturConsole
{
    public class Process
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public int? DepartmentID { get; set; }
        public int ProcessDefinitionID { get; set; }
    }
}
