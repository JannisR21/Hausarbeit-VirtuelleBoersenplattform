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
        /// Sendet eine E-Mail zur Passwort-Zurücksetzung
        /// </summary>
        /// <param name="recipientEmail">E-Mail-Adresse des Empfängers</param>
        /// <param name="recipientName">Name des Empfängers</param>
        /// <param name="username">Benutzername</param>
        /// <param name="newPassword">Das neue Passwort</param>
        /// <returns>True, wenn die E-Mail erfolgreich gesendet wurde, sonst False</returns>
        public async Task<bool> SendPasswordResetEmailAsync(string recipientEmail, string recipientName, string username, string newPassword)
        {
            try
            {
                // E-Mail-Template mit HTML und CSS
                string emailBody = GeneratePasswordResetEmailTemplate(recipientName, username, newPassword);

                // Immer den E-Mail-Inhalt in der Debug-Konsole anzeigen (zu Testzwecken)
                Debug.WriteLine($"Passwort-Reset-E-Mail an {recipientEmail} ({recipientName}) gesendet:");
                Debug.WriteLine("Betreff: Ihr neues Passwort | Virtuelle Börsenplattform");
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
                        Subject = "🔐 Ihr neues Passwort | Virtuelle Börsenplattform",
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
        /// Generiert das HTML-Template für die Passwort-Reset-E-Mail
        /// </summary>
        private string GeneratePasswordResetEmailTemplate(string recipientName, string username, string newPassword)
        {
            // HTML-Template als einzelnen String zurückgeben
            string htmlHeader = "<html><head><meta charset=\"utf-8\"><title>Passwort zurücksetzen</title></head><body style=\"font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa;\">";
            string htmlContent = "<div style=\"max-width: 650px; margin: 30px auto; background-color: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08);\">" +
                "<div style=\"background: linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%); color: white; padding: 35px 40px; text-align: center;\">" +
                "<h1 style=\"margin: 0; font-size: 28px; font-weight: bold;\">Virtuelle Börsenplattform</h1>" +
                "<p style=\"font-size: 16px; margin-top: 8px;\">Passwort zurücksetzen</p>" +
                "</div>" +
                "<div style=\"padding: 40px;\">" +
                "<h2 style=\"font-size: 22px; margin-bottom: 25px; font-weight: bold; color: #1E3A8A;\">Ihr Passwort wurde zurückgesetzt</h2>" +
                "<p>Hallo " + recipientName + ",</p>" +
                "<p>Sie haben kürzlich Ihr Passwort zurückgesetzt. Hier sind Ihre neuen Zugangsdaten:</p>" +
                "<div style=\"background-color: #f0f7ff; border-radius: 12px; padding: 25px; margin-bottom: 35px; border: 1px solid #d0e0ff;\">" +
                "<div style=\"display: flex; justify-content: space-between; margin: 12px 0; padding-bottom: 12px; border-bottom: 1px dashed #ccc;\">" +
                "<span style=\"font-weight: bold;\">Benutzername</span>" +
                "<span>" + username + "</span>" +
                "</div>" +
                "<div style=\"display: flex; justify-content: space-between; margin: 12px 0; padding-bottom: 12px; border-bottom: 1px dashed #ccc;\">" +
                "<span style=\"font-weight: bold;\">Neues temporäres Passwort</span>" +
                "<span>Siehe unten</span>" +
                "</div>" +
                "</div>" +
                "<p>Ihr neues temporäres Passwort lautet:</p>" +
                "<div style=\"background-color: #fff8e6; border: 1px solid #F59E0B; border-radius: 8px; padding: 15px; text-align: center; margin: 20px 0;\">" +
                "<div style=\"font-family: monospace; font-size: 20px; font-weight: bold; color: #1E3A8A; letter-spacing: 2px; display: inline-block; padding: 8px 20px; background-color: #f1f5f9; border-radius: 6px; border: 1px dashed #3B82F6;\">" + newPassword + "</div>" +
                "</div>" +
                "<div style=\"background-color: #fff8e6; border-left: 4px solid #F59E0B; padding: 20px; margin: 25px 0; border-radius: 6px;\">" +
                "<p><strong>Wichtiger Sicherheitshinweis:</strong> Bitte ändern Sie dieses Passwort sofort nach der Anmeldung. Das temporäre Passwort ist nur für den einmaligen Zugang gedacht.</p>" +
                "</div>" +
                "<div style=\"background-color: #e6f7ef; border-left: 4px solid #10B981; padding: 20px; margin: 25px 0; border-radius: 6px;\">" +
                "<p><strong>So ändern Sie Ihr Passwort:</strong></p>" +
                "<ol>" +
                "<li>Melden Sie sich mit Ihrem Benutzernamen und dem neuen temporären Passwort an</li>" +
                "<li>Klicken Sie oben rechts auf das Einstellungen-Symbol</li>" +
                "<li>Wählen Sie \"Passwort ändern\"</li>" +
                "<li>Geben Sie das temporäre Passwort und Ihr neues, sicheres Passwort ein</li>" +
                "</ol>" +
                "</div>" +
                "<p>Falls Sie diesen Passwort-Reset nicht angefordert haben, kontaktieren Sie bitte umgehend unseren Support.</p>" +
                "<p>Mit freundlichen Grüßen,<br>" +
                "<strong>Ihr Team der Virtuellen Börsenplattform</strong></p>" +
                "</div>" +
                "<div style=\"background-color: #f1f5f9; padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;\">" +
                "<div style=\"font-weight: bold; font-size: 18px; color: #1E3A8A; margin-bottom: 15px;\">Virtuelle Börsenplattform</div>" +
                "<p style=\"font-size: 14px; color: #64748B; max-width: 400px; margin: 0 auto 15px;\">© 2025 Virtuelle Börsenplattform | Entwickelt von Jannis Ruhland</p>" +
                "<p style=\"color: #94A3B8; font-size: 12px; margin-top: 20px;\">Diese E-Mail wurde automatisch generiert. Bitte antworten Sie nicht darauf.</p>" +
                "</div>" +
                "</div>";
            string htmlFooter = "</body></html>";
            
            return htmlHeader + htmlContent + htmlFooter;
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

            // HTML-Template als einzelnen String zurückgeben
            string htmlHeader = "<html><head><meta charset=\"utf-8\"><title>Willkommen bei der Virtuellen Börsenplattform</title></head><body style=\"font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa;\">";
            
            string htmlContent = "<div style=\"max-width: 650px; margin: 30px auto; background-color: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08);\">" +
                "<div style=\"background: linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%); color: white; padding: 35px 40px; text-align: center;\">" +
                "<h1 style=\"margin: 0; font-size: 28px; font-weight: bold;\">Virtuelle Börsenplattform</h1>" +
                "<p style=\"font-size: 16px; margin-top: 8px;\">Ihr Einstieg in die Welt des risikofreien Aktienhandels</p>" +
                "</div>" +
                
                "<div style=\"padding: 40px;\">" +
                "<p style=\"font-size: 20px; margin-bottom: 25px; font-weight: bold;\">Willkommen, {{RECIPIENT_NAME}}!</p>" +
                
                "<p>Wir freuen uns, Sie bei der Virtuellen Börsenplattform begrüßen zu dürfen! Ihre Registrierung war erfolgreich, und Sie können ab sofort mit dem virtuellen Handel beginnen – ohne echtes Geld zu riskieren, aber mit echten Marktdaten.</p>" +
                
                "<div style=\"background-color: #f0f7ff; border-radius: 12px; padding: 25px; margin-bottom: 35px; border: 1px solid #d0e0ff;\">" +
                "<h3>Ihre Zugangsdaten</h3>" +
                
                "<div style=\"display: flex; justify-content: space-between; margin: 12px 0; padding-bottom: 12px; border-bottom: 1px dashed #ccc;\">" +
                "<span style=\"font-weight: bold;\">Benutzername</span>" +
                "<span>{{USERNAME}}</span>" +
                "</div>" +
                
                "<div style=\"display: flex; justify-content: space-between; margin: 12px 0; padding-bottom: 12px; border-bottom: 1px dashed #ccc;\">" +
                "<span style=\"font-weight: bold;\">Startguthaben</span>" +
                "<span style=\"font-weight: bold; color: #16A34A; font-size: 18px;\">15.000,00 €</span>" +
                "</div>" +
                
                "<div style=\"display: flex; justify-content: space-between; margin: 12px 0; padding-bottom: 12px; border-bottom: 1px dashed #ccc;\">" +
                "<span style=\"font-weight: bold;\">Status</span>" +
                "<span style=\"color: #10B981;\">Aktiv</span>" +
                "</div>" +
                "</div>" +
                
                "<div style=\"background-color: #e6f7ef; border-left: 4px solid #10B981; padding: 20px; margin: 25px 0; border-radius: 6px;\">" +
                "<p><strong>Bonus:</strong> Wir haben als Willkommensgeschenk bereits <strong>zwei Apple-Aktien</strong> für Sie erworben! Sehen Sie sich Ihr Portfolio an und verfolgen Sie die Entwicklung dieser Aktien als Einstieg.</p>" +
                "</div>" +
                
                "<h3 style=\"margin-top: 40px; font-size: 18px;\">Was unsere Plattform Ihnen bietet:</h3>" +
                
                "<div style=\"margin: 35px 0;\">" +
                "<div style=\"background-color: #f8fafc; border-radius: 10px; padding: 20px; border: 1px solid #e2e8f0; margin-bottom: 15px;\">" +
                "<div style=\"font-weight: bold; margin-bottom: 10px; font-size: 16px;\">Portfolio Management</div>" +
                "<p>Verwalten Sie Ihr virtuelles Portfolio mit echten Aktien und ETFs. Kaufen und verkaufen Sie zu aktuellen Marktpreisen.</p>" +
                "</div>" +
                
                "<div style=\"background-color: #f8fafc; border-radius: 10px; padding: 20px; border: 1px solid #e2e8f0; margin-bottom: 15px;\">" +
                "<div style=\"font-weight: bold; margin-bottom: 10px; font-size: 16px;\">Echtzeit-Daten</div>" +
                "<p>Nutzen Sie Live-Marktdaten von weltweiten Börsen, um informierte Entscheidungen zu treffen.</p>" +
                "</div>" +
                
                "<div style=\"background-color: #f8fafc; border-radius: 10px; padding: 20px; border: 1px solid #e2e8f0; margin-bottom: 15px;\">" +
                "<div style=\"font-weight: bold; margin-bottom: 10px; font-size: 16px;\">Performance-Tracking</div>" +
                "<p>Verfolgen Sie Ihre Gewinne und Verluste mit detaillierten Diagrammen und analytischen Tools.</p>" +
                "</div>" +
                
                "<div style=\"background-color: #f8fafc; border-radius: 10px; padding: 20px; border: 1px solid #e2e8f0; margin-bottom: 15px;\">" +
                "<div style=\"font-weight: bold; margin-bottom: 10px; font-size: 16px;\">Strategie-Entwicklung</div>" +
                "<p>Testen Sie verschiedene Handelsstrategien, ohne echtes Geld zu riskieren.</p>" +
                "</div>" +
                "</div>" +
                
                "<div style=\"height: 1px; background-color: #e0e0e0; margin: 35px 0;\"></div>" +
                
                "<h3>So starten Sie:</h3>" +
                
                "<div>" +
                "<p>1. <strong>Anmelden</strong> - Melden Sie sich mit Ihren neuen Zugangsdaten auf der Plattform an.</p>" +
                "<p>2. <strong>Marktübersicht erkunden</strong> - Verschaffen Sie sich einen Überblick über aktuelle Kurse und Trends.</p>" +
                "<p>3. <strong>Erste Transaktion tätigen</strong> - Kaufen Sie Ihre ersten Aktien oder ETFs mit Ihrem virtuellen Guthaben.</p>" +
                "<p>4. <strong>Performance analysieren</strong> - Verfolgen Sie die Entwicklung Ihres Portfolios und optimieren Sie Ihre Strategie.</p>" +
                "</div>" +
                
                "<div style=\"background-color: #fff8e6; border-left: 4px solid #F59E0B; padding: 20px; margin: 25px 0; border-radius: 6px;\">" +
                "<p><strong>Tipp für Einsteiger:</strong> Beginnen Sie mit dem Handel von bekannten Aktien und ETFs, die Sie verstehen. Diversifizieren Sie Ihr Portfolio, um Risiken zu streuen. Nutzen Sie unsere Marktdaten, um fundierte Entscheidungen zu treffen.</p>" +
                "</div>" +
                
                "<p>Wir wünschen Ihnen viel Erfolg bei Ihren virtuellen Investments!</p>" +
                
                "<p style=\"margin-top: 20px;\">Mit freundlichen Grüßen,<br>" +
                "<strong>Ihr Team der Virtuellen Börsenplattform</strong></p>" +
                "</div>" +
                
                "<div style=\"background-color: #f1f5f9; padding: 30px; text-align: center; border-top: 1px solid #e2e8f0;\">" +
                "<div style=\"font-weight: bold; font-size: 18px; color: #1E3A8A; margin-bottom: 15px;\">Virtuelle Börsenplattform</div>" +
                "<p style=\"font-size: 14px; color: #64748B; max-width: 400px; margin: 0 auto 15px;\">Trainieren Sie Ihre Investmentfähigkeiten mit echter Marktdynamik – ohne finanzielles Risiko.</p>" +
                "<p style=\"color: #94A3B8; font-size: 13px;\">© 2025 Virtuelle Börsenplattform | Entwickelt von Jannis Ruhland</p>" +
                
                "<div style=\"margin-top: 20px; padding-top: 20px; border-top: 1px solid #e2e8f0;\">" +
                "<a href=\"#\" style=\"color: #3B82F6; text-decoration: none; margin: 0 15px; font-size: 13px;\">Nutzungsbedingungen</a>" +
                "<a href=\"#\" style=\"color: #3B82F6; text-decoration: none; margin: 0 15px; font-size: 13px;\">Datenschutz</a>" +
                "<a href=\"#\" style=\"color: #3B82F6; text-decoration: none; margin: 0 15px; font-size: 13px;\">Kontakt</a>" +
                "</div>" +
                
                "<p style=\"color: #94A3B8; font-size: 12px; margin-top: 20px;\">Diese E-Mail wurde automatisch generiert. Bitte antworten Sie nicht darauf.</p>" +
                "</div>" +
                "</div>";

            string htmlFooter = "</body></html>";
            
            string template = htmlHeader + htmlContent + htmlFooter;

            return template;
        }
    }
}