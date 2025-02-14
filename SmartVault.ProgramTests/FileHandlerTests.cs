using Microsoft.Extensions.Configuration;
using System.Data.SQLite;
using Dapper;

namespace SmartVault.Program.Tests
{
    [TestClass]
    public class FileHandlerTests
    {
        private string _testOutputDir;
        private string _testOutputFile;
        private IConfiguration _testConfiguration;
        private SQLiteConnection _testConnection;
        private FileHandler _fileHandler;

        [TestInitialize]
        public void TestInitialize()
        {
            _testOutputDir = Path.Combine(Path.GetTempPath(), "FileHandlerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testOutputDir);

            var _testDbPath = Path.Combine(_testOutputDir, "testdb.sqlite");
            _testOutputFile = Path.Combine(_testOutputDir, "consolidatedFile.txt");

            SQLiteConnection.CreateFile(_testDbPath);
            _testConnection = new SQLiteConnection($"Data Source={_testDbPath};Version=3;");
            _testConnection.Open();

            _testConnection.Execute(
                @"CREATE TABLE Document (
                    Id INTEGER PRIMARY KEY,
                    AccountId TEXT,
                    FilePath TEXT
                )");

            var configDict = new Dictionary<string, string>
            {
                {"ConnectionStrings:DefaultConnection", $"Data Source={_testDbPath};Version=3;"}
            };

            _testConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            _fileHandler = new FileHandler(_testConnection, _testConfiguration);
        }


        [TestMethod]
        public void GetAllFileSizes_ReturnsCorrectTotalSize()
        {
            var testFiles = new List<string>();
            long expectedTotalSize = 0;

            for (int i = 1; i <= 3; i++)
            {
                string filePath = Path.Combine(_testOutputDir, $"testfile{i}.txt");
                string content = new string('A', i * 100); 
                File.WriteAllText(filePath, content);
                testFiles.Add(filePath);
                expectedTotalSize += i * 100;
            }

            foreach (var file in testFiles)
            {
                _testConnection.Execute(
                    "INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = "123", FilePath = file });
            }

            // Act
            long actualSize = _fileHandler.GetAllFileSizes();

            // Assert
            Assert.AreEqual(expectedTotalSize, actualSize);
        }

        [TestMethod]
        public void WriteEveryThirdFileToFile_FiltersCorrectFiles()
        {
            // Arrange 
            var accountId = "456";
            var testFiles = new List<string>();

            for (int i = 1; i <= 6; i++)
            {
                string filePath = Path.Combine(_testOutputDir, $"docfile{i}.txt");
                string content = $"File {i} regular content";
                if (i % 3 == 0)
                {
                    content = $"File {i} with Smith Property content";
                }

                File.WriteAllText(filePath, content);
                testFiles.Add(filePath);
            }

            foreach (var file in testFiles)
            {
                _testConnection.Execute(
                    "INSERT INTO Document (AccountId, FilePath) VALUES (@AccountId, @FilePath)",
                    new { AccountId = accountId, FilePath = file });
            }

            // Act
            _fileHandler.WriteEveryThirdFileToFile(accountId, _testOutputFile, "Smith Property");

            // Assert
            var fileExists = File.Exists(_testOutputFile);
            
            Assert.IsTrue(fileExists);
            string fileContent = File.ReadAllText(_testOutputFile);
            Assert.IsTrue(fileContent.Contains("Smith Property"));
            
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _testConnection.Close();
            _testConnection.Dispose();

            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, true);
            }
        }
    }
}