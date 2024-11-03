using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KonturConsole
{
    public class ProcessRecord
    {
        [Name("Категория процесса")]
        public string Category { get; set; }

        [Name("Код процесса")]
        public string ProcessCode { get; set; }

        [Name("Наименование процесса")]
        public string ProcessName { get; set; }

        [Name("Подразделение-владелец процесса")]
        public string Department { get; set; }
    }
}
