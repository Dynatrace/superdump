using System;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SuperDumpService.Helpers {
	public static class LocalDBAccess {
		public static SqlConnection GetLocalDB(IConfigurationRoot configuration, string dbName, PathHelper pathHelper, bool deleteIfExists = false) {
			try {
				// todo: need to think about cleanup
				pathHelper.PrepareDirectories();
				string outputFolder = pathHelper.GetHangfireDBDir();
				string mdfFilename = dbName + ".mdf";
				string dbFileName = Path.GetFullPath(new Uri(Path.Combine(pathHelper.GetWorkingDir(), Path.Combine(outputFolder, mdfFilename))).LocalPath);

				// Create Data Directory If It Doesn't Already Exist.
				if (!Directory.Exists(outputFolder)) {
					Directory.CreateDirectory(outputFolder);
				}

				// If the file exists, and we want to delete old data, remove it here and create a new database.
				if (File.Exists(dbFileName) && deleteIfExists) {
					DropDatabase(configuration, dbName);
					CreateDatabase(configuration, dbName, dbFileName);
				} else if (!File.Exists(dbFileName)) {
					// If the database does not already exist, create it.
					CreateDatabase(configuration, dbName, dbFileName);
				}

				// Open newly created, or old database.
				string connectionString = configuration.GetConnectionString("HangfireDB");
				SqlConnection connection = new SqlConnection(connectionString);
				connection.Open();
				return connection;
			} catch {
				throw;
			}
		}

		private static void DropDatabase(IConfigurationRoot configuration, string dbName) {
			try {
				using (var tmpConn = new SqlConnection(configuration.GetConnectionString("MasterDB"))) {
					tmpConn.Open();
					var tmpDropCommand = tmpConn.CreateCommand();
					tmpDropCommand.CommandText = $"DROP DATABASE {dbName}";
				}
			} catch (SqlException) { }
		}

		public static bool CreateDatabase(IConfigurationRoot configuration, string dbName, string dbFileName) {
			try {
				string connectionString = configuration.GetConnectionString("MasterDB");
				using (var connection = new SqlConnection(connectionString)) {
					connection.Open();
					SqlCommand cmd = connection.CreateCommand();

					DetachDatabase(configuration, dbName);

					cmd.CommandText = $"CREATE DATABASE {dbName} ON (NAME = '{dbName}', FILENAME = '{dbFileName}')";
					cmd.ExecuteNonQuery();
				}
				return true;
			} catch (Exception ex) {
				Console.WriteLine("cannot create DB, check if LocalDB is installed!");
				Console.WriteLine(ex.Message);
				return false;
			}
		}
		
		public static bool DetachDatabase(IConfigurationRoot configuration, string dbName) {
			try {
				string connectionString = configuration.GetConnectionString("MasterDB");
				using (var connection = new SqlConnection(connectionString)) {
					connection.Open();
					SqlCommand cmd = connection.CreateCommand();
					cmd.CommandText = $"exec sp_detach_db '{dbName}'";
					cmd.ExecuteNonQuery();

					return true;
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				return false;
			}
		}
	}
}
