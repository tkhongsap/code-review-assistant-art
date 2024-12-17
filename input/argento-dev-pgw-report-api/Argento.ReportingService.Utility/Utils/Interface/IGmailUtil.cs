namespace Argento.ReportingService.Utility.Utils.Interface
{
    public interface IGmailUtil
    {
        void Send(string sendTo, string subject, string message);
    }
}