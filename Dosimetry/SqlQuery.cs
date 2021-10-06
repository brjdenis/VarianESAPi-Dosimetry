using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.IO;

namespace Dosimetry
{
    public class SqlQuery
    {
        public string DatabasePath;


        public SqlQuery(string DatabasePath)
        {
            this.DatabasePath = DatabasePath;
        }


        public SQLiteConnection OpenConnection(bool close)
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + this.DatabasePath + "; Version=3; FailIfMissing=True;");

            sqlite_conn.Open();
            if (close)
            {
                sqlite_conn.Close();
            }

            return sqlite_conn;
        }

        public bool TestConnection()
        {
            try
            {
                OpenConnection(close: true);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool CanAddTable(string PatientID, string TableName)
        {
            bool result = false;
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "SELECT count(*) FROM DosimetrySpecial WHERE PatientID=@PatientID AND TableName=@TableName;";
                    sqlite_cmd.Parameters.AddWithValue("@PatientID", PatientID);
                    sqlite_cmd.Parameters.AddWithValue("@TableName", TableName);
                    int count = Convert.ToInt32(sqlite_cmd.ExecuteScalar());

                    if (count <= 0)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public List<List<string>> GetTableNames(string PatientID)
        {
            List<List<string>> result = new List<List<string>>() { };
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "SELECT TableName, DateTime FROM DosimetrySpecial WHERE PatientID=@PatientID ORDER BY datetime(DateTime) DESC;";
                    sqlite_cmd.Parameters.AddWithValue("@PatientID", PatientID);

                    using (SQLiteDataReader rdr = sqlite_cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            List<string> temp = new List<string>() { };
                            temp.Add(rdr.GetString(0));
                            temp.Add(rdr.GetString(1));
                            result.Add(temp);
                        }
                    }
                }
            }
            return result;
        }


        public Dictionary<string, string> GetTableData(string PatientID, string TableName)
        {
            Dictionary<string, string> result = new Dictionary<string, string>() { };
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "SELECT DataGridOrgans, DataGridPTV1, DataGridPTV2, Normalization "
                        + "FROM DosimetrySpecial WHERE PatientID=@PatientID AND TableName=@TableName";
                    sqlite_cmd.Parameters.AddWithValue("@PatientID", PatientID);
                    sqlite_cmd.Parameters.AddWithValue("@TableName", TableName);
                    using (SQLiteDataReader rdr = sqlite_cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            result["DataGridOrgans"] = rdr.GetString(0);
                            result["DataGridPTV1"] = rdr.GetString(1);
                            result["DataGridPTV2"] = rdr.GetString(2);
                            result["Normalization"] = rdr.GetString(3);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        public List<List<string>> GetPatientIDs(string searchString)
        {
            List<List<string>> results = new List<List<string>>() { };
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "SELECT PatientID, TableName, DateTime, LastSaver, Normalization"
                        + " FROM DosimetrySpecial WHERE PatientID LIKE @search;";
                    sqlite_cmd.Parameters.AddWithValue("@search", searchString + "%");
                    using (SQLiteDataReader rdr = sqlite_cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            List<string> temp = new List<string>() { };
                            temp.Add(rdr.GetString(0));
                            temp.Add(rdr.GetString(1));
                            temp.Add(rdr.GetString(2));
                            temp.Add(rdr.GetString(3));
                            temp.Add(rdr.GetString(4));
                            results.Add(temp);
                        }
                    }
                }
            }
            return results;
        }


        public void AddNewTable(string PatientID, string TableName, string dateTime, string LastSaver,
            string DataGridOrgans, string DataGridPTV1, string DataGridPTV2, string Normalization)
        {
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "INSERT INTO DosimetrySpecial "
                    + "(PatientID, TableName, DateTime, LastSaver, DataGridOrgans, DataGridPTV1, DataGridPTV2, Normalization) VALUES "
                    + "(@PatientID, @TableName, @DateTime, @LastSaver, @DataGridOrgans, @DataGridPTV1, @DataGridPTV2, @Normalization); ";
                    sqlite_cmd.Parameters.AddWithValue("@PatientID", PatientID);
                    sqlite_cmd.Parameters.AddWithValue("@TableName", TableName);
                    sqlite_cmd.Parameters.AddWithValue("@DateTime", dateTime);
                    sqlite_cmd.Parameters.AddWithValue("@LastSaver", LastSaver);
                    sqlite_cmd.Parameters.AddWithValue("@DataGridOrgans", DataGridOrgans);
                    sqlite_cmd.Parameters.AddWithValue("@DataGridPTV1", DataGridPTV1);
                    sqlite_cmd.Parameters.AddWithValue("@DataGridPTV2", DataGridPTV2);
                    sqlite_cmd.Parameters.AddWithValue("@Normalization", Normalization);
                    sqlite_cmd.Prepare();
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
        }


        public void UpdateTable(string PatientID, string TableName, string dateTime, string LastSaver,
            string DataGridOrgans, string DataGridPTV1, string DataGridPTV2, string Normalization)
        {
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "UPDATE DosimetrySpecial SET DateTime = @DateTime, "
                        + "LastSaver = @LastSaver, "
                        + "DataGridOrgans = @DataGridOrgans, "
                        + "DataGridPTV1 = @DataGridPTV1, "
                        + "DataGridPTV2 = @DataGridPTV2, "
                        + "Normalization = @Normalization "
                        + "WHERE PatientID = @PatientID AND TableName = @TableName";
                    sqlite_cmd.Parameters.AddWithValue("@DateTime", dateTime);
                    sqlite_cmd.Parameters.AddWithValue("@LastSaver", LastSaver);
                    sqlite_cmd.Parameters.AddWithValue("@DataGridOrgans", DataGridOrgans);
                    sqlite_cmd.Parameters.AddWithValue("@DataGridPTV1", DataGridPTV1);
                    sqlite_cmd.Parameters.AddWithValue("@DataGridPTV2", DataGridPTV2);
                    sqlite_cmd.Parameters.AddWithValue("@Normalization", Normalization);
                    sqlite_cmd.Parameters.AddWithValue("@PatientID", PatientID);
                    sqlite_cmd.Parameters.AddWithValue("@TableName", TableName);
                    sqlite_cmd.Prepare();
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
        }

        public void ChangeID(string PatientIDold, string PatientIDNew)
        {
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "UPDATE DosimetrySpecial SET PatientID = @PatientIDNew "
                        + "WHERE PatientID = @PatientIDold";
                    sqlite_cmd.Parameters.AddWithValue("@PatientIDNew", PatientIDNew);
                    sqlite_cmd.Parameters.AddWithValue("@PatientIDold", PatientIDold);
                    sqlite_cmd.Prepare();
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTable(string PatientID, string TableName)
        {
            using (SQLiteConnection conn = OpenConnection(close: false))
            {
                using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "DELETE FROM DosimetrySpecial "
                        + "WHERE PatientID = @PatientID AND TableName = @TableName";
                    sqlite_cmd.Parameters.AddWithValue("@PatientID", PatientID);
                    sqlite_cmd.Parameters.AddWithValue("@TableName", TableName);
                    sqlite_cmd.Prepare();
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
