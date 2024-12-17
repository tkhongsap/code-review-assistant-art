namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IZipFileUtil
    {
        void CreateZipFile(string fromPath, string destinationPath);
        void UnZipFile(string fromPath, string destinationPath);
    }
}