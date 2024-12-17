using Argento.ReportingService.Utility.Utils.Interface;
using Arcadia.Extensions.DependencyInjection.Attributes;
using System.IO.Compression;

namespace Argento.ReportingService.Utility.Utils
{
    /// <summary>
    /// ZipFile Service
    /// Author : Peeradis S.
    /// Date 18/5/2018
    ///
    /// User for Zip Fie
    /// Example : CreateZipFile(C:\\ArcadiaDocument,C:\\ArcadiaDocument.zip);
    /// </summary>
    [RegisterType(typeof(IZipFileUtil))]
    public class ZipFileUtil : IZipFileUtil
    {
        public void CreateZipFile(string fromPath, string destinationPath)
        {
            ZipFile.CreateFromDirectory(fromPath, destinationPath);
        }

        public void UnZipFile(string fromPath, string destinationPath)
        {
            ZipFile.ExtractToDirectory(fromPath, destinationPath);
        }
    }
}