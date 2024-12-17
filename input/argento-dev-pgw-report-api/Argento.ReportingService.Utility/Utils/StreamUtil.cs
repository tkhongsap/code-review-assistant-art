using Argento.ReportingService.Utility.Extensions;
using Argento.ReportingService.Utility.Utils.Interface;
using Arcadia.Extensions.DependencyInjection.Attributes;
using System;
using System.IO;
using System.Text;

namespace Argento.ReportingService.Utility.Utils
{
    [RegisterType(typeof(IStreamUtil))]
    public class StreamUtil : IStreamUtil
    {
        public byte[] ToByteArray(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            stream.Position = 0;

            int bufferSize = 1024 * 10; // kb
            var buffer = new byte[bufferSize];
            byte[] array = null;
            using (var ms = new MemoryStream())
            using (var bs = new BufferedStream(ms))
            {
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    bs.Write(buffer, 0, bytesRead);
                }
                bs.Flush();
                array = ms.ToArray();
            }
            stream.Position = 0;

            return array;
        }

        public string ToString(Stream stream, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            int bufferSize = 256;
            var buffer = new char[bufferSize];
            var sb = new StringBuilder();
            var streamReader = new StreamReader(stream, encoding);
            int charsRead;
            while ((charsRead = streamReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(buffer.SubArray(0, charsRead));
            }

            stream.Position = 0;

            return sb.ToString();
        }
    }
}