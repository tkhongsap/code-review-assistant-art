namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IFileUtil
    {
        bool DirectoryExists(string folderPath);
        bool FileExists(string filePath);
        void CopyFile(string filePath, string destinationPath);
        void CreateDirectory(string filepath);
        void WriteAllBytes(string filePath, byte[] bytes);
        void WriteAllText(string filePath, string text);
        void DeleteFile(string filePath);
        void DeleteDirectory(string directoryPath);
        string GetFileChecksum(string filePath);
        string ReadAllText(string filePath);


    }
}