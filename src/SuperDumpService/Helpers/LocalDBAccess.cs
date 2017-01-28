using System;
using System.IO;
using System.Data.SqlClient;

namespace SuperDumpService.Helpers {
	public static class LocalDBAccess {
		public static SqlConnection GetLocalDB(string dbName, bool deleteIfExists = false) {
			try {
				// todo: need to think about cleanup
				PathHelper.PrepareDirectories();
				string outputFolder = PathHelper.GetHangfireDBDir();
				string mdfFilename = dbName + ".mdf";
				string dbFileName = Path.Combine(outputFolder, mdfFilename);
				string logFileName = Path.Combine(outputFolder, String.Format("{0}_log.ldf", dbName));
				// Create Data Directory If It Doesn't Already Exist.
				if (!Directory.Exists(outputFolder)) {
					Directory.CreateDirectory(outputFolder);
				}

				// If the file exists, and we want to delete old data, remove it here and create a new database.
				if (File.Exists(dbFileName) && deleteIfExists) {
					if (File.Exists(logFileName)) File.Delete(logFileName);
					File.Delete(dbFileName);
					CreateDatabase(dbName, dbFileName);
				}
				// If the database does not already exist, create it.
				else if (!File.Exists(dbFileName)) {
					CreateDatabase(dbName, dbFileName);
				}

				// Open newly created, or old database.
				string connectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFileName={1};Initial Catalog={0};Integrated Security=True;", dbName, dbFileName);
				SqlConnection connection = new SqlConnection(connectionString);
				connection.Open();
				return connection;
			} catch {
				throw;
			}
		}

		public static bool CreateDatabase(string dbName, string dbFileName) {
			try {
				string connectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True");
				using (var connection = new SqlConnection(connectionString)) {
					connection.Open();
					SqlCommand cmd = connection.CreateCommand();

					DetachDatabase(dbName);

					cmd.CommandText = String.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", dbName, dbFileName);
					cmd.ExecuteNonQuery();
				}

				if (File.Exists(dbFileName)) {
					return true;
				} else {
					return false;
				}
			} catch (Exception ex) {
				Console.WriteLine("cannot create DB, check if LocalDB is installed!");
				Console.WriteLine(ex.Message);
				return false;
			}
		}

		public static bool DetachDatabase(string dbName) {
			try {
				string connectionString = String.Format(@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True");
				using (var connection = new SqlConnection(connectionString)) {
					connection.Open();
					SqlCommand cmd = connection.CreateCommand();
					cmd.CommandText = String.Format("exec sp_detach_db '{0}'", dbName);
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
