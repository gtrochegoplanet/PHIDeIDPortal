namespace PhiDeidPortal.Ui.SignalR
{
    using Microsoft.AspNetCore.SignalR;

    public class DocumentHub : Hub
    {
        public void DocumentStatusChanged(string documentId, string status)
        {
            Clients.All.SendAsync("DocumentStatusChanged", documentId, status);            
        }
    }
}