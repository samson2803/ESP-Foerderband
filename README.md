# ESP-Foerderband

Förderband-Steuerung mit NodeMCU ESP8266, DRV8825 Schrittmotortreiber und WLAN-Webinterface.

Ursprünglich als automatischer Poop-Chute für den **Bambu Lab A1 Mini** entwickelt:
Der Druckkopf fährt beim Filamentwechsel auf die Y-Achsen-Endposition — eine Lichtschranke erkennt
dies und transportiert den Purge-Abfall automatisch in einen Schacht/Mülleimer.

![Förderband](foerderband.jpg)

---

## Funktionen

- **Automatische Auslösung** per Lichtschranke (Flankenerkennung, kein Dauerbetrieb)
- **Webinterface** zur Konfiguration von Geschwindigkeit, Umdrehungen und Walzendurchmesser
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
