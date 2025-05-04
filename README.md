# Virtuelle Börsenplattform

## Projektbeschreibung
Diese Anwendung ist eine virtuelle Börsenplattform, entwickelt als WPF-Anwendung in C# mit Entity Framework Core. Sie ermöglicht Benutzern, mit einem virtuellen Startkapital Aktien und ETFs zu kaufen und zu verkaufen, ihre Portfolios zu verwalten und die Entwicklung ihrer Investments in Echtzeit zu verfolgen. Die Plattform integriert Kursdaten über die Twelve Data API und bietet verschiedene Ansichten und Funktionen für einen realistischen Börsenhandel ohne finanzielle Risiken.

## Hauptfunktionen

### Benutzer-Management
- **Registrierung**: Neue Benutzer können sich mit E-Mail, Benutzername und Passwort registrieren
- **Login**: Bestehende Benutzer können sich einloggen und ihr Konto verwalten
- **Startguthaben**: Jeder neue Benutzer erhält 15.000€ virtuelles Startkapital
- **Bonus**: 2 Apple-Aktien werden als Willkommensgeschenk automatisch ins Portfolio hinzugefügt

### Aktienhandel
- **Marktübersicht**: Anzeige aller verfügbaren Aktien und ETFs mit Echtzeitkursen und Änderungsindikatoren
- **Aktiensuche**: Filtern und Suchen nach Aktien nach Namen, Symbol oder Kategorie
- **Kauf & Verkauf**: Intuitive UI zum Kauf und Verkauf von Aktien mit Preis- und Kostenvorschau
- **Limit-Funktion**: Benutzer können nur entsprechend ihres verfügbaren Guthabens handeln

### Portfolio-Management
- **Portfolio-Übersicht**: Anzeige aller gehaltenen Positionen mit aktuellen Werten
- **Performance-Tracking**: Farbliche Hervorhebung von Gewinnen und Verlusten
- **Gewinn/Verlust-Berechnung**: Anzeige absoluter und prozentualer Wertveränderungen
- **Automatische Aktualisierung**: Regelmäßige Aktualisierung der Kursdaten kann varieren von API Limits 

### Watchlist-Funktion
- **Beobachtungsliste**: Benutzer können interessante Aktien in einer Watchlist speichern
- **Performance-Tracking**: Anzeige der Kursveränderungen seit Hinzufügen zur Watchlist

<!-- ### Benutzererfahrung 'funktioniert noch nicht'
- **Dark/Light-Mode**: Umschaltbare Themen für optimale Lesbarkeit
- **Responsive Design**: Anpassungsfähige UI für verschiedene Bildschirmgrößen
- **Echtzeitaktualisierung**: Live-Updates der UI bei Datenänderungen
- **Statusanzeigen**: Informationen zur Marktöffnung und API-Status -->

## Technische Details

### Projekt-Architektur
Die Anwendung folgt dem MVVM-Pattern (Model-View-ViewModel) mit klarer Trennung von:
- **Models**: Datenmodelle für Entitäten wie Benutzer, Aktien und Portfolioeinträge
- **Views**: XAML-basierte Benutzeroberflächen 
- **ViewModels**: Logische Schicht, die Models mit Views verbindet
- **Services**: Dienstklassen für Datenbankzugriff, API-Kommunikation und Authentifizierung

### Verwendete Frameworks und Bibliotheken
- **.NET 6**: Basis-Framework für die Anwendung
- **WPF**: Windows Presentation Foundation für die UI
- **Entity Framework Core**: ORM für den Datenbankzugriff
- **CommunityToolkit.Mvvm**: Hilfsbibliothek für MVVM-Implementierung
- **Newtonsoft.Json**: JSON-Verarbeitung für API-Antworten
- **BCrypt.Net**: Sichere Passworthashing-Bibliothek

### Externe Dienste
- **Twelve Data API**: Quelle für Echtzeit-Aktienkurse 
- **Rate-Limiter**: Implementierung zur Einhaltung der API-Limits von Twelve Data

### Helfer-Klassen
- **DarkAndLightMode**: Verwaltet die Farbthemen der Anwendung
- **DataUIExtensions**: Enthält Methoden zum Aktualisieren der UI
- **WatermarkHelper**: Implementiert Platzhaltertexte in Textfeldern
- **RateLimiter**: Überwacht API-Anfragen, um Rate-Limits einzuhalten

### Datenmodell

#### Benutzer
- Enthält Authentifizierungsdaten (Benutzername, E-Mail, Passwort-Hash)
- Speichert persönliche Informationen (Vor- und Nachname)
- Verwaltet den virtuellen Kontostand für den Aktienhandel

#### Aktie
- Basisdatenmodell für handelbare Wertpapiere
- Enthält Symbol, Name, aktuellen Preis und Kursänderungen
- Wird durch Spezialisierung (ETF) erweitert für verschiedene Anlageprodukte

#### PortfolioEintrag
- Verbindet Benutzer mit ihren gehaltenen Aktien
- Speichert Anzahl, Einstandspreis und aktuellen Kurs
- Berechnet Wert, Gewinn/Verlust absolut und prozentual

#### WatchlistEintrag
- Speichert beobachtete Aktien pro Benutzer
- Verfolgt die Kursänderung seit dem Hinzufügen zur Watchlist

#### Kategorisierung
- **AktienBranche**: Kategorisiert Aktien nach Branchen wie Technologie, Finanzen, Gesundheit
- **AnlageTyp**: Unterscheidet zwischen Einzelaktien, ETFs, Fonds, Anleihen und Zertifikaten
- **ETFTyp**: Klassifiziert ETFs nach Anlagetyp wie Aktien, Anleihen, Rohstoffe

### Datenbankdesign
- **SQL Server**: Als Datenbank-Backend 
- **Code-First Migrations**: Evolution der Datenbankstruktur über Migrations-Klassen
- **Beziehungen**: Korrekte Implementierung von Fremdschlüsselbeziehungen
- **Indizes**: Optimierung für schnelle Abfragen
- **Seed-Daten**: Vorpopulierte Daten für Benutzer und Aktien

### Erste Schritte
1. Registriere einen neuen Benutzer oder verwende die Demo-Zugangsdaten (Benutzername: "demo", Passwort: "demo")
2. Erkunde die Marktübersicht und Filtermöglichkeiten
3. Füge Aktien zu deiner Watchlist hinzu oder kaufe direkt Aktien mit deinem Startguthaben
4. Verfolge die Performance deiner Investitionen im Portfolio-Bereich

## Implementierungsdetails

### Authentifizierung
- Passwörter werden mit BCrypt sicher gehasht gespeichert
- Validierung für Benutzernamen und E-Mail-Adressen
- Unterstützung für Registrierung neuer Benutzer
- Automatische E-Mail-Benachrichtigung bei Registrierung 

### API-Kommunikation
- **TwelveDataService**: Verbindet zur Twelve Data API für Aktienkurse
- **RateLimiter**: Stellt sicher, dass API-Limits eingehalten werden
- **Cache-Mechanismen**: Minimiert die Anzahl der API-Aufrufe
- **Fehlerbehandlung**: Robuste Fehlerbehandlung bei API-Ausfällen

### User Interface
- **DataGrid-Erweiterungen**: Automatisches Aktualisieren von Datengrids
- **Converter**: Umfangreiche Sammlung von Convertern für die UI-Datenbindung

### Datenbank-Operationen
- **DatabaseService**: Zentraler Service für Datenbankoperationen
- **Kontexterneuerung**: Automatische Erneuerung des Datenbankkontexts für ThreadSafety
- **Transaktionen**: Sicherstellung der Datenintegrität durch Transaktionen
- **Migrationen**: Versionierung des Datenbankschemas

## Komponentenübersicht

### Helpers
- **DarkAndLightMode**: Themeverwaltung (dunkel/hell)
- **DataUIExtensions**: UI-Aktualisierungsmethoden
- **FilterChangedEventArgs**: Event-Argumente für Aktienfilter
- **InverseViewMatchConverter**: UI-Converter für Ansichtsvergleiche
- **RateLimiter**: API-Ratenbegrenzung
- **WatermarkHelper**: Textfeld-Platzhalterimplementierung

### Services
- **TwelveDataService**: Kommunikation mit der Twelve Data API
- **DatabaseService**: Zentrale Datenbankoperationen
- **AuthenticationService**: Benutzerauthentifizierung und -verwaltung
- **EmailService**: Versendet E-Mails (Registrierungsbestätigungen)
- **AktienFilterService**: Filterfunktionen für Aktien und ETFs

### Models
- **Aktie**: Basisklasse für Aktien
- **AktienKategorie/AnlageTyp**: Enums und Helper für Kategorisierungen
- **Benutzer**: Benutzermodell
- **ETF**: Spezialisierte Aktienklasse für ETFs
- **MarktStatus**: Hält den aktuellen Status des Marktes
- **PortfolioEintrag/WatchlistEintrag**: Verknüpfungsmodelle

### Klassen- und Komponentenreferenz
- **Namespace HausarbeitVirtuelleBörsenplattform.Helpers**: Hilfsklassen
- **Namespace HausarbeitVirtuelleBörsenplattform.Models**: Datenmodelle
- **Namespace HausarbeitVirtuelleBörsenplattform.Services**: Dienste
- **Namespace HausarbeitVirtuelleBörsenplattform.Converters**: UI-Converter
- **Namespace HausarbeitVirtuelleBörsenplattform.Migrations**: Datenbankmigrationen

### Datenbankschema
Die Datenbank besteht aus vier Haupttabellen:
- **Benutzer**: Benutzerkonten (BenutzerID, Benutzername, Email, PasswortHash, Kontostand...)
- **Aktien**: Verfügbare Aktien (AktienID, AktienSymbol, AktienName, AktuellerPreis...)
- **PortfolioEintraege**: Aktienbesitz (BenutzerID, AktienID, Anzahl, EinstandsPreis...)
- **WatchlistEintraege**: Beobachtete Aktien (BenutzerID, AktienID, HinzugefuegtAm, KursBeimHinzufuegen...)

---

## Autor

Dieses Projekt wurde im Rahmen der Hausarbeit der HBFSWI erstellt von Jannis Ruhland.
