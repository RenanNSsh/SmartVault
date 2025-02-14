using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace SmartVault.Program
{
    public class FileHandler
    {
        private readonly IConfiguration? _configuration;
        private readonly SQLiteConnection _connection;

        public FileHandler()
        {
            _configuration = GetDefaultConfiguration();
            _connection = GetDefaultConnection();
        }
        public FileHandler(SQLiteConnection connection)
        {
            _configuration = GetDefaultConfiguration();
            _connection = connection;
        }
        public FileHandler(SQLiteConnection connection, IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = connection;
        }

        private IConfiguration GetDefaultConfiguration(string jsonFile = "appsettings.json")
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(jsonFile).Build();
        }

        private SQLiteConnection GetDefaultConnection()
        {
            var defaultConnection = _configuration?["ConnectionStrings:DefaultConnection"] ?? "";
            string applicationRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? "";   
            var dbLocation = Path.Combine(applicationRoot, "testdb.sqlite");
            if (!File.Exists(dbLocation))
            {
                throw new FileNotFoundException("Database not found");
            }
            return new SQLiteConnection(string.Format(defaultConnection, dbLocation));
        }

        public void WriteEveryThirdFileToFile(string accountId, string outputFile, string contentCondition = "Smith Property")
        {
            var query = $"Select FilePath from Document where accountId = @AccountId;";
            var documentData = _connection.Query<string>(query, new {accountId}).ToList();
            try
            {
                var resultContent = ReadFileContents(documentData);
                WriteFile(resultContent, outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public long GetAllFileSizes()
        {

            var documentData = _connection.Query<string>("Select FilePath from Document;");
            long totalSize = 0;

            foreach (var document in documentData)
            {
                FileInfo fileInfo = new FileInfo(document);

                long fileSizeInBytes = fileInfo.Length;
                totalSize += fileSizeInBytes;
            }

            Console.WriteLine($"Total Size: {totalSize} bytes");
            return totalSize;
        }

        private static string ReadFileContents(List<string> filePaths)
        {
            var result = "";
            for (var i = 2; i < filePaths.Count(); i += 3)
            {
                using StreamReader reader = new(filePaths[i]);
                string content = reader.ReadToEnd();

                if (content.Contains("Smith Property"))
                {
                    result += content;
                }
            }
            return result;
        }

        private static void WriteFile(string fileText, string outputFile)
        {
            using StreamWriter writer = new(outputFile);

            writer.WriteLine(fileText);
            writer.WriteLine();
            Console.WriteLine($"Consolidated file created: {outputFile}");
        }
    }
}
