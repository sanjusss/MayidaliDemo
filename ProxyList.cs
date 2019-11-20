using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MayidaliDemo
{
    public static class ProxyList
    {
        private static readonly SQLiteConnection _conn;

        static ProxyList()
        {
            _conn = CreateConnection();
            InitTable();
        }

        public static void AddProxyInfo(string ip, string city, string isp)
        {
            var info = GetInfo(ip);
            info.city = city;
            info.isp = isp;
            ++info.count;
            SaveInfo(info);
        }

        private static ProxyInfo GetInfo(string ip)
        {
            ProxyInfo info = new ProxyInfo()
            {
                ip = ip
            };
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT * FROM proxylist WHERE ip = '{ip}'";
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info.city = Convert.ToString(reader["city"]);
                        info.isp = Convert.ToString(reader["isp"]);
                        info.count = Convert.ToInt32(reader["count"]);
                    }
                    else
                    {
                        info.count = 0;
                    }
                }
            }

            return info;
        }

        private static void SaveInfo(ProxyInfo info)
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "REPLACE INTO proxylist (ip, city, isp, count) VALUES (@ip, @city, @isp, @count)";
                cmd.Parameters.AddWithValue("ip", info.ip);
                cmd.Parameters.AddWithValue("city", info.city);
                cmd.Parameters.AddWithValue("isp", info.isp);
                cmd.Parameters.AddWithValue("count", info.count);
                cmd.ExecuteNonQuery();
            }
        }

        private static SQLiteConnection CreateConnection()
        {
            string dbPath = "Data Source =" + AppDomain.CurrentDomain.BaseDirectory + "/data.db";
            var conn = new SQLiteConnection(dbPath);
            conn.Open();
            return conn;
        }

        private static void InitTable()
        {
            string sql = "CREATE TABLE IF NOT EXISTS \"proxylist\" (\n"
                         + "  \"ip\" text NOT NULL COLLATE BINARY,\n"
                         + "  \"city\" text NOT NULL,\n"
                         + "  \"isp\" text NOT NULL,\n"
                         + "  \"count\" integer NOT NULL DEFAULT 0,\n"
                         + "  PRIMARY KEY (\"ip\")\n"
                         + ");";
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
