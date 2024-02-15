using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using MySqlConnector;

namespace last_seen
{
    public class MySQLDatabase
    {
        private MySqlConnection connection;
        public MySqlCommand comm;
        private string server;
        private string database;
        private string username;
        private string password;

        public MySQLDatabase(string server, string database, string username, string password)
        {
            this.server = server;
            this.database = database;
            this.username = username;
            this.password = password;
        }

        public void OpenConnection()
        {
            string connectionString = $"Server={server};Database={database};Uid={username};Pwd={password};Convert Zero Datetime=true;";
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }

        private void CloseConnection()
        {
            connection.Close();
        }

        public void ExecuteNonQuery(string query)
        {
            try
            {

                comm = new MySqlCommand(query, connection);
                comm.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
        }

        public MySqlDataReader ExecuteQuery(string query)
        {
            try
            {

                MySqlCommand command = new MySqlCommand(query, connection);
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }
    }
    public class Startup
    {   
        public void ConfigureServices(IServiceCollection services)
        {

        }    
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {                                  
                    WebRequest request = WebRequest.Create("https://andrei123.okdesk.ru/api/v1/employees/list?api_token=b0c3624630cc03f9b77f4d6f1ab2a5f13e18b0a9&page[direction]=forward");
                    WebResponse respons = request.GetResponse();
                    string line = " ";
                    using (StreamReader stream = new StreamReader(respons.GetResponseStream()))
                    {
                        if ((line = stream.ReadLine()) != null)
                        {
                            string server = "localhost";
                            string database = "test";
                            string username = "root";
                            string password = "";
                            MySQLDatabase databas = new MySQLDatabase(server, database, username, password);
                            databas.OpenConnection();
                           
                            string insertQuery = "INSERT INTO employes_last_seen (id, FIO,last_seen) VALUES ";
                         
                            dynamic jsonDe = JsonConvert.DeserializeObject(line);
                            foreach (var a in jsonDe)
                            {
                                string datetime = a.last_seen.ToString("yyyy-MM-dd");
                                datetime += " ";
                                datetime += a.last_seen.ToString("hh:mm:ss");
                                string FIO = a.last_name + " " + a.first_name + " " + a.patronymic;
                                insertQuery += $"('{a.id}','{FIO}','{datetime}'),";
                            }
                            insertQuery = insertQuery.Remove(insertQuery.Length - 1);
                            databas.ExecuteNonQuery(insertQuery);

                            string selectQuery = "SELECT * FROM employes_last_seen";
                            databas.OpenConnection();
                            MySqlDataReader reader = databas.ExecuteQuery(selectQuery);

                            var response = context.Response;
                            var stringBuilder = new System.Text.StringBuilder("<table>");
                          
                            response.ContentType = "text/html; charset=utf-8";
                            stringBuilder.Append("<style> table {width: 600px; margin: auto;text-align: center;border - collapse: collapse; } td { border: 2px solid #333;} </style>");
                            stringBuilder.Append("<table><tr><td>id сотрудника</td><td>ФИО сотрудника</td><td>Последнее время посещения</td></tr>");
                            while (reader.Read())
                            {
                                
                                int id = reader.GetInt32("id");
                                string fio = reader.GetString("FIO");
                                 DateTime date = reader.GetDateTime("last_seen");  
                                
                                stringBuilder.Append($"<tr><td>{id}</td><td>{fio}</td><td>{date}</td></tr>");
                            }
                            stringBuilder.Append("</table>");
                            await response.WriteAsync(stringBuilder.ToString());
                            reader.Close();
                        }
                    }                  
                });
            });
        }
    }
}