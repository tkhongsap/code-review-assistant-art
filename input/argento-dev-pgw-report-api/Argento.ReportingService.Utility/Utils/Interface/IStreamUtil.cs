using System.IO;
using System.Text;

namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IStreamUtil
    {
        byte[] ToByteArray(Stream stream);
        string ToString(Stream stream, Encoding encoding);
    }
}