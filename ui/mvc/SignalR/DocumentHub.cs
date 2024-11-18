namespace PhiDeidPortal.Ui.SignalR
{
    using Microsoft.AspNetCore.SignalR;

    public class DocumentHub : Hub
    {
        public async Task NotifyDocumentStatusChanged(string documentId, string status)
        {
            await Clients.All.SendAsync("DocumentStatusChanged", documentId, status);
        }
    }
}