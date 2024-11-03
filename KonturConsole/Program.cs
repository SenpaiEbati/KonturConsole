using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using Npgsql;
using System.Globalization;
using System.Threading.Tasks;
using System.Formats.Asn1;
using System.Text;

namespace KonturConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // Расширяем набор кодировок, где присутствует Windows-1251.До этого установив System.Text.Encoding.CodePages в NuGet
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // Путь к CSV файлу
            string filecsv = "C:/KontyrFiles/testdata.csv";
            // Подключение к базе данных PostgreSQL
            string conndb = "Host=localhost;Username=postgres;Password=****;Database=Konturdb";

            // Создание списков 
            var categories = new List<Category>();
            var departments = new List<Department>();
            var processdefs = new List<ProcessDefinition>();
            var processes = new List<Process>();

            using (var reader = new StreamReader(filecsv, Encoding.GetEncoding("Windows-1251")))
            using (var csv = new CsvReader(reader, 
                new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    Encoding = Encoding.GetEncoding("Windows-1251"),
                    HeaderValidated = null,
                    MissingFieldFound = null
                }))
            {
                // Чтение CSV-файла
                var records = csv.GetRecords<ProcessRecord>();

                // Уникальные значения для нормализации
                int categoryID = 1, departmentID = 1, procdefID = 1, processID = 1;
                var categoryDict = new Dictionary<string, int>();
                var departmentDict = new Dictionary<string, int>();
                var processdefDict = new Dictionary<(string, string), int>();

                foreach (var record in records)
                {
                    // Переписывание в список категорий
                    if (!categoryDict.ContainsKey(record.Category))
                    {
                        categoryDict[record.Category] = categoryID++;
                        categories.Add(new Category
                        {
                            ID = categoryDict[record.Category],
                            CategoryName = record.Category
                        });
                    }

                    // Переписывание в список подразделений
                    if (!string.IsNullOrEmpty(record.Department) && !departmentDict.ContainsKey(record.Department))
                    {
                        departmentDict[record.Department] = departmentID++;
                        departments.Add(new Department
                        {
                            ID = departmentDict[record.Department],
                            DepartmentName = record.Department
                        });
                    }

                    // Переписывание в список названий процессов
                    var processKey = (record.ProcessCode, record.ProcessName);
                    if (!processdefDict.ContainsKey(processKey))
                    {
                        processdefDict[processKey] = procdefID++;
                        processdefs.Add(new ProcessDefinition
                        {
                            ID = processdefDict[processKey],
                            ProcessCode = record.ProcessCode,
                            ProcessName = record.ProcessName
                        });
                    }

                    // Сводим все ID выше в единый список исходя из 3 нормальной формы 
                    processes.Add(new Process
                    {
                        ID = processID++,
                        CategoryID = categoryDict[record.Category],
                        DepartmentID = string.IsNullOrEmpty(record.Department) ? (int?)null : departmentDict[record.Department],
                        ProcessDefinitionID = processdefDict[processKey]
                    });
                }
            }

            // Подключаемся к базе данных для ввода в бд данных, хранящихся в списках. 
            using (var conn = new NpgsqlConnection(conndb))
            {
                conn.Open();

                // Вставка данных в таблицу категорий
                using (var cmd = new NpgsqlCommand("INSERT INTO process_categories (id, category_name) VALUES (@id, @category_name)", conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("id", DbType.Int32));
                    cmd.Parameters.Add(new NpgsqlParameter("category_name", DbType.String));
                    foreach (var category in categories)
                    {
                        cmd.Parameters["id"].Value = category.ID;
                        cmd.Parameters["category_name"].Value = category.CategoryName;
                        cmd.ExecuteNonQuery();
                    }
                }

                // Вставка данных в таблицу подразделений
                using (var cmd = new NpgsqlCommand("INSERT INTO departments (id, department_name) VALUES (@id, @department_name)", conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("id", DbType.Int32));
                    cmd.Parameters.Add(new NpgsqlParameter("department_name", DbType.String));
                    foreach (var department in departments)
                    {
                        cmd.Parameters["id"].Value = department.ID;
                        cmd.Parameters["department_name"].Value = department.DepartmentName;
                        cmd.ExecuteNonQuery();
                    }
                }

                // Вставка данных в таблицу определений процессов
                using (var cmd = new NpgsqlCommand("INSERT INTO process_definitions (id, process_code, process_name) VALUES (@id, @process_code, @process_name)", conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("id", DbType.Int32));
                    cmd.Parameters.Add(new NpgsqlParameter("process_code", DbType.String));
                    cmd.Parameters.Add(new NpgsqlParameter("process_name", DbType.String));
                    foreach (var processdef in processdefs)
                    {
                        cmd.Parameters["id"].Value = processdef.ID;
                        cmd.Parameters["process_code"].Value = processdef.ProcessCode;
                        cmd.Parameters["process_name"].Value = processdef.ProcessName;
                        cmd.ExecuteNonQuery();
                    }
                }

                // Вставка данных в общюю таблицу процессов
                using (var cmd = new NpgsqlCommand("INSERT INTO processes (id, category_id, department_id, process_definition_id) VALUES (@id, @category_id, @department_id, @process_definition_id)", conn))
                {
                    cmd.Parameters.Add(new NpgsqlParameter("id", DbType.Int32));
                    cmd.Parameters.Add(new NpgsqlParameter("category_id", DbType.Int32));
                    cmd.Parameters.Add(new NpgsqlParameter("department_id", DbType.Int32));
                    cmd.Parameters.Add(new NpgsqlParameter("process_definition_id", DbType.Int32));
                    foreach (var process in processes)
                    {
                        cmd.Parameters["id"].Value = process.ID;
                        cmd.Parameters["category_id"].Value = process.CategoryID;
                        cmd.Parameters["department_id"].Value = process.DepartmentID.HasValue ? (object)process.DepartmentID.Value : DBNull.Value;
                        cmd.Parameters["process_definition_id"].Value = process.ProcessDefinitionID;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            // Вывод в консоль сообщения об окончании работы кода
            Console.WriteLine("Данные успешно загружены в PostgreSQL.");
        }
    }
}

