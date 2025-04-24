using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    /// <summary>
    /// Service zum Versenden von E-Mails
    /// </summary>
    public class EmailService
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly bool _isTestMode;

        /// <summary>
        /// Initialisiert eine neue Instanz des EmailService
        /// </summary>
        public EmailService(string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, string senderEmail, string senderName, bool isTestMode = false)
        {
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
            _senderEmail = senderEmail;
            _senderName = senderName;
            _isTestMode = isTestMode;

            Debug.WriteLine($"EmailService initialisiert mit Server: {smtpServer}, Port: {smtpPort}, Benutzer: {smtpUsername}");
        }

        /// <summary>
        /// Sendet eine Registrierungsbestätigungs-E-Mail
        /// </summary>
        /// <param name="recipientEmail">E-Mail-Adresse des Empfängers</param>
        /// <param name="recipientName">Name des Empfängers</param>
        /// <param name="username">Benutzername</param>
        /// <returns>True, wenn die E-Mail erfolgreich gesendet wurde, sonst False</returns>
        public async Task<bool> SendRegistrationConfirmationAsync(string recipientEmail, string recipientName, string username)
        {
            try
            {
                // E-Mail-Template mit HTML und CSS
                string emailBody = CreateRegistrationEmailHtml(recipientName, username);

                // Immer den E-Mail-Inhalt in der Debug-Konsole anzeigen (zu Testzwecken)
                Debug.WriteLine($"Registrierungsbestätigung an {recipientEmail} ({recipientName}) gesendet:");
                Debug.WriteLine("Betreff: Willkommen bei der Virtuellen Börsenplattform");
                Debug.WriteLine("HTML-E-Mail mit ansprechendem Layout wird versendet.");

                // Wenn im Testmodus, keine tatsächliche E-Mail senden
                if (_isTestMode)
                {
                    Debug.WriteLine("Test-Modus aktiv: Keine E-Mail gesendet.");
                    await Task.Delay(500); // Kurze Verzögerung simulieren
                    return true;
                }

                // Tatsächliche E-Mail senden
                try
                {
                    var client = new SmtpClient(_smtpServer, _smtpPort)
                    {
                        Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_senderEmail, _senderName),
                        Subject = "Willkommen bei der Virtuellen Börsenplattform",
                        Body = emailBody,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(new MailAddress(recipientEmail, recipientName));

                    Debug.WriteLine("SMTP-Verbindung wird hergestellt...");
                    await client.SendMailAsync(mailMessage);
                    Debug.WriteLine("E-Mail erfolgreich gesendet!");

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Senden der E-Mail über SMTP: {ex.Message}");
                    Debug.WriteLine($"Stacktrace: {ex.StackTrace}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Allgemeiner Fehler beim Senden der E-Mail: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Erstellt das HTML-Template für die Registrierungsbestätigungs-E-Mail
        /// </summary>
        private string CreateRegistrationEmailHtml(string recipientName, string username)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Willkommen bei der Virtuellen Börsenplattform</title>
    <style>
        body {
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 10px rgba(0,0,0,0.1);
        }
        .header {
            background-color: #2c3e50;
            color: white;
            padding: 25px;
            text-align: center;
        }
        .header h1 {
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }
        .content {
            padding: 30px;
        }
        .welcome-message {
            font-size: 18px;
            margin-bottom: 25px;
            color: #2c3e50;
        }
        .account-info {
            background-color: #f1f8e9;
            border-radius: 6px;
            padding: 20px;
            margin-bottom: 25px;
        }
        .account-info h3 {
            margin-top: 0;
            color: #2c3e50;
            font-size: 16px;
        }
        .account-info p {
            margin: 8px 0;
        }
        .cta-button {
            display: inline-block;
            background-color: #2c3e50;
            color: white;
            text-decoration: none;
            padding: 12px 24px;
            border-radius: 4px;
            font-weight: 600;
            margin: 15px 0;
        }
        .footer {
            background-color: #f5f5f5;
            padding: 20px;
            text-align: center;
            font-size: 14px;
            color: #777;
        }
        .highlight {
            font-weight: bold;
            color: #33691e;
        }
        .info-box {
            background-color: #e8f5e9;
            border-left: 4px solid #4caf50;
            padding: 12px 20px;
            margin: 15px 0;
        }
        .chart-icon {
            text-align: center;
            margin: 15px 0;
            font-size: 48px;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Virtuelle Börsenplattform</h1>
        </div>
        <div class='content'>
            <p class='welcome-message'>Hallo " + recipientName + @",</p>
            
            <p>Herzlich willkommen bei der Virtuellen Börsenplattform! Ihre Registrierung war erfolgreich.</p>
            
            <div class='chart-icon'>📈</div>
            
            <div class='account-info'>
                <h3>Ihre Zugangsdaten</h3>
                <p><strong>Benutzername:</strong> " + username + @"</p>
                <p><strong>Startguthaben:</strong> <span class='highlight'>15.000,00 €</span></p>
            </div>
            
            <p>Sie können sich ab sofort mit Ihren Zugangsdaten einloggen und mit dem Handel beginnen.</p>
            
            <div class='info-box'>
                <p><strong>Tipp:</strong> Verschaffen Sie sich zunächst einen Überblick über die aktuellen Markttrends, bevor Sie Ihre ersten Investments tätigen.</p>
            </div>
            
            <p>Mit unserer Plattform können Sie:</p>
            <ul>
                <li>Aktien risikofrei kaufen und verkaufen</li>
                <li>Ihr Portfolio verwalten und analysieren</li>
                <li>Markttrends beobachten</li>
                <li>Ihre Handelsstrategien testen</li>
            </ul>
            
            <p>Wir wünschen Ihnen viel Erfolg und gute Gewinne!</p>
            
            <p>Mit freundlichen Grüßen,<br>
            Ihr Team der Virtuellen Börsenplattform</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Virtuelle Börsenplattform | Entwickelt von Jannis Ruhland</p>
            <p>Diese E-Mail wurde automatisch generiert. Bitte antworten Sie nicht darauf.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}