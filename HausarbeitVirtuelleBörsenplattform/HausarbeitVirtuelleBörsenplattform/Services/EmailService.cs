using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;

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
                string emailBody = GenerateEmailTemplate(recipientName, username);

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
                        Subject = "🚀 Willkommen zu Ihrer Trading-Reise! | Virtuelle Börsenplattform",
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
        /// Verwendet eine einfache Ersetzungsmethode, um die Daten einzufügen
        /// </summary>
        private string GenerateEmailTemplate(string recipientName, string username)
        {
            // HTML-Template aus einer Datei laden oder hier als String definieren
            string template = LoadEmailTemplate();

            // Ersetzungen durchführen - diese Methode ist sicherer als komplexe Interpolation
            template = template.Replace("{{RECIPIENT_NAME}}", recipientName)
                              .Replace("{{USERNAME}}", username);

            return template;
        }

        /// <summary>
        /// Lädt das HTML-Template aus einer Datei oder gibt ein Standard-Template zurück
        /// </summary>
        private string LoadEmailTemplate()
        {
            // In einer echten Anwendung würde man die Vorlage aus einer Datei laden
            // return File.ReadAllText("Templates/RegistrationEmail.html");

            // Hier vereinfachtes Template als String
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Willkommen bei der Virtuellen Börsenplattform</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f8f9fa;
        }
        
        .container {
            max-width: 650px;
            margin: 30px auto;
            background-color: #fff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 24px rgba(0,0,0,0.08);
        }
        
        .header {
            background: linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%);
            color: white;
            padding: 35px 40px;
            text-align: center;
        }
        
        .header h1 {
            margin: 0;
            font-size: 28px;
            font-weight: bold;
        }
        
        .tagline {
            font-size: 16px;
            margin-top: 8px;
        }
        
        .content {
            padding: 40px;
        }
        
        .welcome-message {
            font-size: 20px;
            margin-bottom: 25px;
            font-weight: bold;
        }
        
        .account-info {
            background-color: #f0f7ff;
            border-radius: 12px;
            padding: 25px;
            margin-bottom: 35px;
            border: 1px solid #d0e0ff;
        }
        
        .account-detail {
            display: flex;
            justify-content: space-between;
            margin: 12px 0;
            padding-bottom: 12px;
            border-bottom: 1px dashed #ccc;
        }
        
        .account-label {
            font-weight: bold;
        }
        
        .highlight {
            font-weight: bold;
            color: #16A34A;
            font-size: 18px;
        }
        
        .info-box {
            background-color: #e6f7ef;
            border-left: 4px solid #10B981;
            padding: 20px;
            margin: 25px 0;
            border-radius: 6px;
        }
        
        .warning-box {
            background-color: #fff8e6;
            border-left: 4px solid #F59E0B;
            padding: 20px;
            margin: 25px 0;
            border-radius: 6px;
        }
        
        .feature-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin: 35px 0;
        }
        
        .feature-item {
            background-color: #f8fafc;
            border-radius: 10px;
            padding: 20px;
            border: 1px solid #e2e8f0;
        }
        
        .feature-title {
            font-weight: bold;
            margin-bottom: 10px;
            font-size: 16px;
        }
        
        .divider {
            height: 1px;
            background-color: #e0e0e0;
            margin: 35px 0;
        }
        
        .step {
            display: flex;
            margin-bottom: 20px;
        }
        
        .step-number {
            background-color: #2563EB;
            color: white;
            width: 30px;
            height: 30px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            margin-right: 15px;
        }
        
        .step-title {
            font-weight: bold;
            margin-bottom: 5px;
        }
        
        .cta-button {
            display: inline-block;
            background-color: #2563EB;
            color: white;
            text-decoration: none;
            padding: 16px 32px;
            border-radius: 8px;
            font-weight: bold;
            font-size: 16px;
            margin: 25px 0 15px;
            text-align: center;
        }
        
        .footer {
            background-color: #f1f5f9;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e2e8f0;
        }
        
        .footer-logo {
            font-weight: bold;
            font-size: 18px;
            color: #1E3A8A;
            margin-bottom: 15px;
        }
        
        .footer-text {
            font-size: 14px;
            color: #64748B;
            max-width: 400px;
            margin: 0 auto 15px;
        }
        
        .footer-links {
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e2e8f0;
            display: flex;
            justify-content: center;
            flex-wrap: wrap;
        }
        
        .footer-link {
            color: #3B82F6;
            text-decoration: none;
            margin: 0 15px;
            font-size: 13px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Virtuelle Börsenplattform</h1>
            <p class=""tagline"">Ihr Einstieg in die Welt des risikofreien Aktienhandels</p>
        </div>
        
        <div class=""content"">
            <p class=""welcome-message"">Willkommen, {{RECIPIENT_NAME}}!</p>

            <p>Wir freuen uns, Sie bei der Virtuellen Börsenplattform begrüßen zu dürfen! Ihre Registrierung war erfolgreich, und Sie können ab sofort mit dem virtuellen Handel beginnen – ohne echtes Geld zu riskieren, aber mit echten Marktdaten.</p>
            
            <div class=""account-info"">
                <h3>Ihre Zugangsdaten</h3>

                <div class=""account-detail"">
                    <span class=""account-label"">Benutzername</span>
                    <span>{{USERNAME}}</span>
                </div>

                <div class=""account-detail"">
                    <span class=""account-label"">Startguthaben</span>
                    <span class=""highlight"">15.000,00 €</span>
                </div>

                <div class=""account-detail"">
                    <span class=""account-label"">Status</span>
                    <span style=""color: #10B981;"">Aktiv</span>
                </div>
            </div>

            <div class=""info-box"">
                <p><strong>Bonus:</strong> Wir haben als Willkommensgeschenk bereits <strong>zwei Apple-Aktien</strong> für Sie erworben! Sehen Sie sich Ihr Portfolio an und verfolgen Sie die Entwicklung dieser Aktien als Einstieg.</p>
            </div>
            
            <h3 style=""margin-top: 40px; font-size: 18px;"">Was unsere Plattform Ihnen bietet:</h3>

            <div class=""feature-grid"">
                <div class=""feature-item"">
                    <div class=""feature-title"">Portfolio Management</div>
                    <p>Verwalten Sie Ihr virtuelles Portfolio mit echten Aktien und ETFs. Kaufen und verkaufen Sie zu aktuellen Marktpreisen.</p>
                </div>
                
                <div class=""feature-item"">
                    <div class=""feature-title"">Echtzeit-Daten</div>
                    <p>Nutzen Sie Live-Marktdaten von weltweiten Börsen, um informierte Entscheidungen zu treffen.</p>
                </div>
                
                <div class=""feature-item"">
                    <div class=""feature-title"">Performance-Tracking</div>
                    <p>Verfolgen Sie Ihre Gewinne und Verluste mit detaillierten Diagrammen und analytischen Tools.</p>
                </div>
                
                <div class=""feature-item"">
                    <div class=""feature-title"">Strategie-Entwicklung</div>
                    <p>Testen Sie verschiedene Handelsstrategien, ohne echtes Geld zu riskieren.</p>
                </div>
            </div>
            
            <div class=""divider""></div>

            <h3>So starten Sie:</h3>

            <div>
                <div class=""step"">
                    <div class=""step-number"">1</div>
                    <div>
                        <div class=""step-title"">Anmelden</div>
                        <p>Melden Sie sich mit Ihren neuen Zugangsdaten auf der Plattform an.</p>
                    </div>
                </div>
                
                <div class=""step"">
                    <div class=""step-number"">2</div>
                    <div>
                        <div class=""step-title"">Marktübersicht erkunden</div>
                        <p>Verschaffen Sie sich einen Überblick über aktuelle Kurse und Trends.</p>
                    </div>
                </div>
                
                <div class=""step"">
                    <div class=""step-number"">3</div>
                    <div>
                        <div class=""step-title"">Erste Transaktion tätigen</div>
                        <p>Kaufen Sie Ihre ersten Aktien oder ETFs mit Ihrem virtuellen Guthaben.</p>
                    </div>
                </div>
                
                <div class=""step"">
                    <div class=""step-number"">4</div>
                    <div>
                        <div class=""step-title"">Performance analysieren</div>
                        <p>Verfolgen Sie die Entwicklung Ihres Portfolios und optimieren Sie Ihre Strategie.</p>
                    </div>
                </div>
            </div>
            
            <div class=""warning-box"">
                <p><strong>Tipp für Einsteiger:</strong> Beginnen Sie mit dem Handel von bekannten Aktien und ETFs, die Sie verstehen. Diversifizieren Sie Ihr Portfolio, um Risiken zu streuen. Nutzen Sie unsere Marktdaten, um fundierte Entscheidungen zu treffen.</p>
            </div>
            
            <center>
                <a href=""#"" class=""cta-button"">Jetzt mit dem Handel beginnen</a>
            </center>
            
            <p style=""margin-top: 30px;"">Wir wünschen Ihnen viel Erfolg bei Ihren virtuellen Investments!</p>
            
            <p style=""margin-top: 20px;"">Mit freundlichen Grüßen,<br>
            <strong>Ihr Team der Virtuellen Börsenplattform</strong></p>
        </div>
        
        <div class=""footer"">
            <div class=""footer-logo"">Virtuelle Börsenplattform</div>
            <p class=""footer-text"">Trainieren Sie Ihre Investmentfähigkeiten mit echter Marktdynamik – ohne finanzielles Risiko.</p>
            <p style=""color: #94A3B8; font-size: 13px;"">© 2025 Virtuelle Börsenplattform | Entwickelt von Jannis Ruhland</p>
            
            <div class=""footer-links"">
                <a href=""#"" class=""footer-link"">Nutzungsbedingungen</a>
                <a href=""#"" class=""footer-link"">Datenschutz</a>
                <a href=""#"" class=""footer-link"">Kontakt</a>
            </div>

            <p style=""color: #94A3B8; font-size: 12px; margin-top: 20px;"">Diese E-Mail wurde automatisch generiert. Bitte antworten Sie nicht darauf.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}