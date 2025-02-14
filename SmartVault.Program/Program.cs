
using System;
using System.IO;

namespace SmartVault.Program
{
    partial class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("Please provide accountId as first argument\r\n");
            }

            var accoundId = args[0];

            var fileHandler = new FileHandler();


            string applicationRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? "";
            var singleFileResult = Path.Combine(applicationRoot, "singleFileResult.txt");

            fileHandler.WriteEveryThirdFileToFile(accoundId, singleFileResult);
            fileHandler.GetAllFileSizes();
        }
    }
}