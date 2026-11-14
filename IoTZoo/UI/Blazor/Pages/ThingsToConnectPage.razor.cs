// --------------------------------------------------------------------------------------------------------------------
//      ____    ______   _____
//     /  _/___/_  __/  /__  / ____  ____
//     / // __ \/ /       / / / __ \/ __ \
//   _/ // /_/ / /       / /_/ /_/ / /_/ /
//  /___/\____/_/       /____|____/\____/   P L A Y G R O U N D
// --------------------------------------------------------------------------------------------------------------------
// Connect «Things» with microcontrollers in a simple way.
// --------------------------------------------------------------------------------------------------------------------
// (c) 2025 - 2026 Holger Freudenreich under the MIT license
// --------------------------------------------------------------------------------------------------------------------

using Domain.Pocos;

namespace IotZoo.Pages;

public class ThingsToConnectPageBase : PageBase
{
   protected string MailSenderExample => """
      {
        "email": {
          "id": "mail-2026-001",
          "priority": "high",
          "createdAt": "2026-11-14T10:30:00+01:00",
          "sender": { "name": "Max Mustermann", "address": "max@example.com" },
          "recipients": {
            "to": [{ "name": "Peter", "address": "peter@example.com" }],
            "cc": [{ "name": "Support", "address": "support@example.com" }],
            "bcc": [{ "name": "Archive", "address": "archive@example.com" }]
          },
          "content": { "subject": "Status", "format": "html", "text": "Forecast: Read('iotzoo/picea/weather_forecast')", "html": "<p>Forecast: Read('iotzoo/picea/weather_forecast')</p>" },
          "attachments": [{ "filename": "status.pdf", "mimeType": "application/pdf", "path": "/documents/status.pdf" }],
          "smtp": { "host": "mail.example.com", "port": 587, "encryption": "STARTTLS", "authentication": { "username": "max@example.com", "passwordRef": "SMTP_PASSWORD" } },
          "options": { "requestReadReceipt": false, "requestDeliveryReceipt": true, "allowHtml": true, "saveToSentFolder": true }
        }
      }
      """;

   protected override void OnInitialized()
   {
      DataTransferService.CurrentScreen = ScreenMode.ThingsToConnect;
   }
}
