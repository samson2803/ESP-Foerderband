# ESP-Foerderband

Förderband-Steuerung mit NodeMCU ESP8266, DRV8825 Schrittmotortreiber und WLAN-Webinterface.

Ursprünglich als automatischer Poop-Chute für den **Bambu Lab A1 Mini** entwickelt:
Der Druckkopf fährt beim Filamentwechsel auf die X-Achsen-Endposition — eine Lichtschranke erkennt
dies und transportiert den Purge-Abfall automatisch in einen Schacht/Mülleimer.

![Förderband](foerderband.jpg)

---

## Funktionen

- **Automatische Auslösung** per Lichtschranke (Flankenerkennung, kein Dauerbetrieb)
- **Webinterface** zur Konfiguration von Geschwindigkeit, Umdrehungen und Walzendurchmesser
- **JSON-API** zum Fahren nach Strecke, Abbrechen und Statusabfrage
- **Desktop-Steuerung** für Windows (VB.NET) — „fahre jetzt 30 cm" per Knopfdruck
- **Wegberechnung** im Browser (Weg/Umdrehung und Gesamtweg live berechnet)
- **Firmware-Update per Browser** (kein PC-Tool nötig, Fortschrittsanzeige)
- **OTA-Update** per Arduino IDE / arduino-cli (`foerderband.local`)
- **Einstellungen persistent** im EEPROM (überleben Stromausfall)
- **Captive-Portal** für einfache WLAN-Erstkonfiguration

---

## Hardware

| Komponente | Details |
|---|---|
| Mikrocontroller | NodeMCU ESP8266 |
| Schrittmotortreiber | DRV8825 |
| Mikrostepping | Low / High / High (1/32) |
| Sensor | Lichtschranke (einstellbar) |

### Pin-Belegung

| Funktion | GPIO | NodeMCU-Pin |
|---|---|---|
| STEP | GPIO4 | D2 |
| DIR | GPIO5 | D1 |
| ENA | GPIO0 | D3 |
| Sensor | GPIO12 | D6 |

> **Wichtig:** Den Sensor **nicht** an D8 (GPIO15) anschließen — GPIO15 ist ein Strapping-Pin
> und muss beim Booten LOW sein. Ein ausgelöster Sensor an D8 verhindert den Bootloader-Zugang.

---

## Installation

### Voraussetzungen

- Arduino IDE 2.x oder arduino-cli
- ESP8266 Board-Paket (`esp8266:esp8266`, Version 3.x)
- Keine zusätzlichen Bibliotheken nötig (alles im Board-Paket enthalten)

### Board-Einstellungen

| Einstellung | Wert |
|---|---|
| Board | Generic ESP8266 Module |
| Flash Size | 4MB |
| Upload Speed | 115200 |
| Reset Method | dtr (nodemcu) |

### Flashen

```bash
arduino-cli compile --fqbn esp8266:esp8266:generic foerderband_web
arduino-cli upload --fqbn esp8266:esp8266:generic --port COM3 foerderband_web
```

---

## Erstinbetriebnahme

1. Sketch flashen
2. Mit WLAN **"Foerderband-Setup"** verbinden (Passwort: `foerderband`)
3. Browser öffnet automatisch die Einrichtungsseite (oder manuell `http://192.168.4.1`)
4. WLAN-Zugangsdaten eingeben → ESP verbindet sich und startet neu
5. IP-Adresse aus dem seriellen Monitor ablesen (115200 Baud) und im Browser aufrufen

---

## Kalibrierung

Den Wert `stepsPerRevolution` im Sketch an den eigenen Aufbau anpassen:

```
stepsPerRevolution = (gemessener Bandweg in mm / Anzahl Soll-Umdrehungen) / (π × Walzendurchmesser) × steps_pro_echte_umdrehung
```

Einfacher: Im Webinterface Walzendurchmesser eintragen und den angezeigten Gesamtweg mit dem
tatsächlichen Bandweg vergleichen. `stepsPerRevolution` entsprechend anpassen.

---

## Fernsteuerung

### JSON-API (ab Firmware v1.3.0)

Alle Endpoints vertragen einfache GET-Aufrufe und antworten mit dem vollständigen Gerätestatus —
nach einem Befehl muss also nicht extra nachgefragt werden.

| Endpoint | Zweck |
|---|---|
| `GET /api/status` | Zustand, Fortschritt und Einstellungen |
| `GET /api/run?cm=30` | Strecke fahren — auch `mm=`, `rev=`, `steps=`; ohne Parameter die gespeicherte Umdrehungszahl |
| `GET /api/stop` | Laufenden Auftrag abbrechen, Treiber stromlos |
| `GET /api/config` | Ohne Parameter lesend, mit Parametern schreibend (`delay`, `umdr`, `durchm`) |

```bash
curl "http://foerderband.local/api/run?cm=30"
```

Ein laufender Auftrag wird nie unterbrochen — ein zweites `/api/run` liefert `409`. Nach einem
`/api/stop` bleibt `steps_remaining` stehen; daran ist erkennbar, dass abgebrochen wurde und wie
weit das Band gekommen ist.

**Strecke → Schritte:** Eine Umdrehung transportiert `π × Walzendurchmesser` mm Band. Bei 33,5 mm
sind das 105,2 mm je Umdrehung, also `2011 / 105,2 ≈ 19,11` Schritte je Millimeter — 30 cm
entsprechen 5.732 Schritten.

### Desktop-App (Windows)

Im Ordner [`desktop/`](desktop/) liegt eine VB.NET-WinForms-Steuerung: Strecke in cm oder mm
eingeben, Schnellwahl für 10/20/30/50 cm, Not-Stop und ein Fortschrittsbalken, der während der
Fahrt alle 300 ms nachgeführt wird. Die Motoreinstellungen sind aus der App heraus änderbar.

Öffnen mit `desktop\FoerderbandControl.sln` (Visual Studio) oder bauen per MSBuild:

```bash
msbuild desktop\FoerderbandControl.sln -p:Configuration=Release
```

Die App zielt standardmäßig auf `foerderband.local` und merkt sich den zuletzt benutzten Host.

---

## Firmware-Update

### Per Browser (empfohlen)
1. Webinterface öffnen → Tab **"Firmware"**
2. Kompilierte `.bin`-Datei auswählen
3. **"Firmware hochladen"** klicken → automatischer Neustart

### Per OTA (Arduino IDE / arduino-cli)
```bash
arduino-cli upload --fqbn esp8266:esp8266:generic --port foerderband.local foerderband_web
```

---

## Lizenz

MIT License — siehe [LICENSE](LICENSE)
