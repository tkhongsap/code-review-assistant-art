namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IImageEmailUtil
    {
        void SendEmailImage(string sendTo, string subject, string htmlBody);
    }
}