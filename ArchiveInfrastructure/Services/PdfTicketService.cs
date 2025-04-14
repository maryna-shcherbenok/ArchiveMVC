using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Identity;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;
using ArchiveDomain.Model;

public class PdfTicketService : IPdfTicketService
{
    private readonly UserManager<User> _userManager;

    public PdfTicketService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public byte[] GenerateTicketPdf(Reservation reservation)
    {
        using var ms = new MemoryStream();
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont("Arial", 20, XFontStyle.Bold);
        var fontLabel = new XFont("Arial", 12, XFontStyle.Bold);
        var fontText = new XFont("Arial", 12, XFontStyle.Regular);
        var fontNote = new XFont("Arial", 10, XFontStyle.Italic);
        var fontSmall = new XFont("Arial", 10, XFontStyle.Regular);
        var fontRulesTitle = new XFont("Arial", 13, XFontStyle.BoldItalic);

        double margin = 30;
        double contentWidth = page.Width - 2 * margin;

        var logoPath = Path.Combine("wwwroot", "images", "logo.png");
        double logoHeight = 0;
        if (File.Exists(logoPath))
        {
            using var logoStream = File.OpenRead(logoPath);
            var logoImage = XImage.FromStream(() => logoStream);

            double maxLogoWidth = 100;
            double ratio = logoImage.PixelHeight / (double)logoImage.PixelWidth;
            logoHeight = maxLogoWidth * ratio;

            gfx.DrawImage(logoImage, margin, margin, maxLogoWidth, logoHeight);
        }

        gfx.DrawString("КВИТОК НА БРОНЮВАННЯ", fontTitle, XBrushes.Maroon,
            new XRect(0, margin + logoHeight + 10, page.Width, 30), XStringFormats.TopCenter);

        double boxY = margin + logoHeight + 50;
        gfx.DrawRoundedRectangle(new XPen(XColors.DarkSlateGray, 1.5), margin, boxY, contentWidth, 150, 10, 10);

        double col1X = margin + 10;
        double col2X = margin + 170;
        double rowY = boxY + 25;
        double rowHeight = 25;

        string fullName = reservation.UserId;
        var user = _userManager.FindByIdAsync(reservation.UserId).Result;
        if (user != null && !string.IsNullOrEmpty(user.FullName))
            fullName = user.FullName;

        var documentInstance = reservation.ReservationDocuments.FirstOrDefault()?.DocumentInstance;
        var documentTitle = documentInstance?.Document?.Title ?? "Невідомий документ";
        var inventoryNumber = documentInstance?.InventoryNumber.ToString() ?? "—";

        gfx.DrawString("Користувач:", fontLabel, XBrushes.Black, new XPoint(col1X, rowY));
        gfx.DrawString(fullName, fontText, XBrushes.DimGray, new XPoint(col2X, rowY));
        rowY += rowHeight;

        gfx.DrawString("Документ:", fontLabel, XBrushes.Black, new XPoint(col1X, rowY));
        gfx.DrawString(documentTitle, fontText, XBrushes.Black, new XPoint(col2X, rowY));
        rowY += rowHeight;

        gfx.DrawString("• Початок бронювання:", fontLabel, XBrushes.Black, new XPoint(col1X, rowY));
        gfx.DrawString(reservation.ReservationStartDateTime.ToString("dd.MM.yyyy HH:mm"), fontText, XBrushes.Black, new XPoint(col2X, rowY));
        rowY += rowHeight;

        gfx.DrawString("• Завершення:", fontLabel, XBrushes.Black, new XPoint(col1X, rowY));
        gfx.DrawString(reservation.ReservationEndDateTime?.ToString("dd.MM.yyyy HH:mm") ?? "—", fontText, XBrushes.Black, new XPoint(col2X, rowY));

        var qrText = $"Inventory:{inventoryNumber};UserId:{reservation.UserId}";
        var qrImage = GenerateQrCodeAsXImage(qrText);
        gfx.DrawImage(qrImage, page.Width - margin - 100, boxY + 10, 90, 90);

        double noteY = boxY + 170;
        gfx.DrawRoundedRectangle(new XPen(XColors.Gray, 0.5), margin, noteY, contentWidth, 100, 10, 10);

        double textY = noteY + 20;
        gfx.DrawString("• Адреса архіву:", fontLabel, XBrushes.Black, new XPoint(margin + 10, textY));
        textY += 15;
        gfx.DrawString("вул. Історична, 12, м. Київ, Україна", fontText, XBrushes.Black, new XPoint(margin + 30, textY));
        textY += 25;
        gfx.DrawString("• Примітка:", fontLabel, XBrushes.Black, new XPoint(margin + 10, textY));
        textY += 15;
        gfx.DrawString("Приходьте не пізніше 15 хв. після початку бронювання.", fontSmall, XBrushes.Black, new XPoint(margin + 30, textY));

        var rulesY = noteY + 130;
        gfx.DrawString("Правила відвідування архіву:", fontRulesTitle, XBrushes.DimGray, new XPoint(margin, rulesY));
        rulesY += 28;

        string[] rules = new[]
        {
            "1. Вхід лише за попереднім бронюванням.",
            "2. Предʼявіть цей квиток при вході.",
            "3. Бронювання дійсне лише вказаний час.",
            "4. Заборонено фотографування без дозволу.",
            "5. Заборонено їсти та пити поруч з документами.",
            "6. Ви несете відповідальність за матеріали.",
            "7. Повідомте заздалегідь про скасування.",
            "8. Дотримуйтесь тиші в читальній залі."
        };

        foreach (var rule in rules)
        {
            gfx.DrawString(rule, fontSmall, XBrushes.DimGray, new XPoint(margin + 20, rulesY));
            rulesY += 24;
        }

        gfx.DrawString("Предʼявіть цей квиток працівнику архіву при вході.", fontNote, XBrushes.Gray,
            new XRect(margin, page.Height - 40, contentWidth, 20), XStringFormats.Center);

        doc.Save(ms);
        return ms.ToArray();
    }

    private XImage GenerateQrCodeAsXImage(string text)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var pngQr = new PngByteQRCode(qrCodeData);
        var qrBytes = pngQr.GetGraphic(20);
        return XImage.FromStream(() => new MemoryStream(qrBytes));
    }
}
