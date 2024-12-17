namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IEmailUtil
    {
        void Send(string sendTo, string subject, string message);
    }
}