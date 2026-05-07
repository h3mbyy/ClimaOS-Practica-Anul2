using System;
using ClimaOS.Data; // Importăm folderul tău Data

namespace ClimaOS
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Încercare de conectare la MySQL...");

            DatabaseHelper dbHelper = new DatabaseHelper();

            try
            {
                using (var connection = dbHelper.GetConnection())
                {
                    connection.Open();
                    Console.WriteLine("Conexiunea la baza de date a reușit!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare la conectare: " + ex.Message);
            }
        }
    }
}