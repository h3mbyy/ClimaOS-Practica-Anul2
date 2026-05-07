using System;
using MySql.Data.MySqlClient; 

namespace ClimaOS.Data
{
    public class DatabaseHelper
    {
        
        private readonly string connectionString = "Server=localhost;Database=ClimaOS_DB;Uid=root;Pwd=godea1234;";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}