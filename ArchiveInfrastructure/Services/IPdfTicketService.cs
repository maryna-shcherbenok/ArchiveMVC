// Services/IPdfTicketService.cs
using ArchiveDomain.Model;

public interface IPdfTicketService
{
    byte[] GenerateTicketPdf(Reservation reservation);
}