using Argento.ReportingService.Utility.Utils.Interface;
using Arcadia.Extensions.DependencyInjection.Attributes;
using System;
using System.IO;

namespace Argento.ReportingService.Utility.Utils
{
    [RegisterType(typeof(IFileUtil))]
    public class FileUtil : IFileUtil
    {
        public bool DirectoryExists(string folderPath)
        {
            return Directory.Exists(folderPath);
        }

        public bool FileExists(string filePath)
        {

            return File.Exists(filePath);
        }

        public string ReadAllText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public void CreateDirectory(string filepath)
        {
            Directory.CreateDirectory(filepath);
        }

        public void WriteAllBytes(string filePath, byte[] bytes)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            string directoryPath = Path.GetDirectoryName(filePath);

            if (directoryPath == null)
            {
                throw new ArgumentException($"Unable to get directory path from {filePath}");
            }

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            File.WriteAllBytes(filePath, bytes);
        }

        public void WriteAllText(string filePath, string text)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string directoryPath = Path.GetDirectoryName(filePath);

            if (directoryPath == null)
            {
                throw new ArgumentException($"Unable to get directory path from {filePath}");
            }

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

            File.WriteAllText(filePath, text);
        }

        public void CopyFile(string filePath, string destinationPath)
        {
            File.Copy(filePath, destinationPath, true);
        }

        public void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        public void DeleteDirectory(string directoryPath)
        {
            string[] subDirectoryPaths = Directory.GetDirectories(directoryPath);
            foreach (string subDirectoryPath in subDirectoryPaths)
            {
                DeleteDirectory(subDirectoryPath);
            }

            string[] filePaths = Directory.GetFiles(directoryPath);
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }
            Directory.Delete(directoryPath);
        }

        public string GetFileChecksum(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            byte[] encryptedBytes = AsymmetricEncryptionUtil.EncryptSHA1(fileBytes);
            string fileChecksum = Convert.ToBase64String(encryptedBytes);
            return fileChecksum;
        }

    }
}