using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace CreateDatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("database.db"))
            {
                Console.WriteLine("File 'database.db' already exists.");
            }
            else
            {
                try
                {
                    SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=database.db; Version=3; FailIfMissing=False;");
                    sqlite_conn.Open();
                    SQLiteCommand sqlite_cmd;
                    sqlite_cmd = sqlite_conn.CreateCommand();

                    string CreateTable =
                        "CREATE TABLE DosimetrySpecial (" +
                        "PatientID TEXT NOT NULL, " +
                        "TableName TEXT NOT NULL, " +
                        "DateTime TEXT NOT NULL, " +
                        "LastSaver TEXT NOT NULL, " +
                        "DataGridOrgans TEXT, " +
                        "DataGridPTV1 TEXT, " +
                        "DataGridPTV2 TEXT, " +
                        "Normalization TEXT, " +
                        "UNIQUE(PatientID, TableName))";

                    sqlite_cmd.CommandText = CreateTable;
                    sqlite_cmd.ExecuteNonQuery();
                    sqlite_conn.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
