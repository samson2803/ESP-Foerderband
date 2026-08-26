/*
  Foerderband-Steuerung mit ESP8266 + Webinterface
  -------------------------------------------------
  Steuert ein Foerderband (z.B. als Poop-Chute fuer Bambu Lab Drucker) per
  Lichtschranke und WLAN-Webinterface.

  Funktionen:
  - Lichtschranke loest per Flankenerkennung konfigurierbare Umdrehungen aus
  - Webinterface: Status, Geschwindigkeit, Umdrehungen, Walzendurchmesser,
    manueller Start, Firmware-Update (OTA per Browser)
  - JSON-API unter /api/... fuer Desktop-App und Skripte:
      /api/status  Zustand + Fortschritt
      /api/run     Strecke fahren (?cm= ?mm= ?rev= ?steps=)
      /api/stop    laufenden Auftrag abbrechen
      /api/config  Einstellungen lesen (ohne Parameter) / schreiben (mit)
  - WLAN-Zugangsdaten und Motor-Einstellungen persistent im EEPROM
  - Captive-Portal Access Point ("Foerderband-Setup") bei fehlendem WLAN
  - OTA-Update per Arduino IDE / arduino-cli (foerderband.local) und Browser

  Erstinbetriebnahme:
    Mit WLAN "Foerderband-Setup" (Passwort: foerderband) verbinden,
    http://192.168.4.1 aufrufen und WLAN-Daten eingeben.

  Hardware:
    NodeMCU ESP8266, DRV8825 (1/32 Mikroschritt)
    STEP=D2(GPIO4), DIR=D1(GPIO5), ENA=D3(GPIO0), Sensor=D6(GPIO12)

  GitHub: https://github.com/...
*/

#define FIRMWARE_VERSION "1.3.1"

#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>
#include <DNSServer.h>
#include <EEPROM.h>
#include <ArduinoOTA.h>

// ---- Pin-Definitionen ----
#define STEP_PIN 4
#define DIR_PIN  5
#define ENA_PIN  0
const int sensorPin = 12; // D6 auf NodeMCU

// ---- Motor-Grundlagen ----
const int stepsPerRevolution = 2011; // kalibriert: Walze 33.5mm, DRV8825 1/32

// ---- Einstellbare Parameter (per Webinterface aenderbar) ----
float stepDelayUs       = 800;
int   umdrehungenSoll   = 10;
float walzendurchmesser = 33.5;

// ---- Laufzeit-Status ----
bool isRunning = false;
long stepsRemaining = 0;
long stepsTotal     = 0;   // Gesamtschritte des laufenden Auftrags (fuer Fortschritt)
int  lastSensorState = LOW;

// Obergrenze je Fahrauftrag - schuetzt vor Tippfehlern wie "3000cm" ueber die API.
const long MAX_RUN_STEPS = 2000000L;

// ---- WLAN-Konfiguration ----
#define EEPROM_SIZE        128
#define EEPROM_MAGIC       0x42
#define SSID_MAX_LEN       32
#define PASS_MAX_LEN       64
#define MOTOR_EEPROM_MAGIC 0x43
#define MOTOR_EEPROM_ADDR  97

char storedSsid[SSID_MAX_LEN + 1] = "";
char storedPass[PASS_MAX_LEN + 1] = "";

bool apMode = false;
const char* apSsid     = "Foerderband-Setup";
const char* apPassword = "foerderband";

ESP8266WebServer server(80);
DNSServer dnsServer;
const byte DNS_PORT = 53;

// ---------- EEPROM ----------
void loadWifiCredentials() {
  EEPROM.begin(EEPROM_SIZE);
  if (EEPROM.read(0) == EEPROM_MAGIC) {
    for (int i = 0; i < SSID_MAX_LEN; i++) storedSsid[i] = EEPROM.read(1 + i);
    storedSsid[SSID_MAX_LEN] = '\0';
    for (int i = 0; i < PASS_MAX_LEN; i++) storedPass[i] = EEPROM.read(1 + SSID_MAX_LEN + i);
    storedPass[PASS_MAX_LEN] = '\0';
  }
}

void saveWifiCredentials(const String& ssid, const String& pass) {
  EEPROM.write(0, EEPROM_MAGIC);
  for (int i = 0; i < SSID_MAX_LEN; i++) EEPROM.write(1 + i, i < (int)ssid.length() ? ssid[i] : 0);
  for (int i = 0; i < PASS_MAX_LEN; i++) EEPROM.write(1 + SSID_MAX_LEN + i, i < (int)pass.length() ? pass[i] : 0);
  EEPROM.commit();
}

void clearWifiCredentials() {
  EEPROM.write(0, 0xFF);
  EEPROM.commit();
}

void loadMotorSettings() {
  if (EEPROM.read(MOTOR_EEPROM_ADDR) == MOTOR_EEPROM_MAGIC) {
    EEPROM.get(MOTOR_EEPROM_ADDR + 1, stepDelayUs);
    EEPROM.get(MOTOR_EEPROM_ADDR + 5, umdrehungenSoll);
    EEPROM.get(MOTOR_EEPROM_ADDR + 9, walzendurchmesser);
    Serial.printf("Motor-Einstellungen: delay=%.0fus, umdr=%d, walze=%.1fmm\n",
                  stepDelayUs, umdrehungenSoll, walzendurchmesser);
  }
}

void saveMotorSettings() {
  EEPROM.write(MOTOR_EEPROM_ADDR, MOTOR_EEPROM_MAGIC);
  EEPROM.put(MOTOR_EEPROM_ADDR + 1, stepDelayUs);
  EEPROM.put(MOTOR_EEPROM_ADDR + 5, umdrehungenSoll);
  EEPROM.put(MOTOR_EEPROM_ADDR + 9, walzendurchmesser);
  EEPROM.commit();
}

// ---------- CSS / gemeinsame Styles ----------
String commonStyles() {
  String s = "<style>";
  s += "body{font-family:sans-serif;max-width:440px;margin:30px auto;padding:0 15px;}";
  s += "h2{color:#333;margin-bottom:4px;} .version{color:#aaa;font-size:12px;margin-bottom:15px;}";
  s += ".status{padding:10px;border-radius:6px;margin-bottom:15px;}";
  s += ".running{background:#d4f8d4;} .idle{background:#eee;}";
  s += "label{display:block;margin-top:10px;font-size:14px;}";
  s += "input[type=number],input[type=file]{width:100%;padding:6px;box-sizing:border-box;}";
  s += "button{margin-top:12px;padding:10px;width:100%;background:#2a7de1;color:#fff;border:none;border-radius:6px;font-size:15px;cursor:pointer;}";
  s += "button.sec{background:#555;}";
  s += ".info{margin-top:10px;padding:8px;background:#e8f0fe;border-radius:6px;font-size:14px;}";
  s += ".tabs{display:flex;border-bottom:2px solid #2a7de1;margin-bottom:15px;}";
  s += ".tab{padding:9px 18px;cursor:pointer;border:none;background:none;font-size:15px;color:#555;border-bottom:3px solid transparent;margin-bottom:-2px;}";
  s += ".tab.active{color:#2a7de1;border-bottom-color:#2a7de1;font-weight:bold;}";
  s += ".pane{display:none;} .pane.active{display:block;}";
  s += ".upd-ok{padding:12px;background:#d4f8d4;border-radius:6px;margin-top:10px;}";
  s += ".upd-err{padding:12px;background:#fdd;border-radius:6px;margin-top:10px;}";
  s += "a.reset{display:block;text-align:center;margin-top:15px;color:#aaa;font-size:13px;}";
  s += "progress{width:100%;margin-top:10px;display:none;}";
  s += "</style>";
  return s;
}

String tabScript() {
  String s = "<script>";
  s += "function tab(id){";
  s += "document.querySelectorAll('.pane').forEach(e=>e.classList.remove('active'));";
  s += "document.querySelectorAll('.tab').forEach(e=>e.classList.remove('active'));";
  s += "document.getElementById(id).classList.add('active');";
  s += "document.getElementById('t_'+id).classList.add('active');}";
  s += "</script>";
  return s;
}

// ---------- Hauptseite ----------
void handleRoot() {
  String html = "<!DOCTYPE html><html><head><meta charset='utf-8'>";
  html += "<meta name='viewport' content='width=device-width, initial-scale=1'>";
  html += "<title>Foerderband Steuerung</title>";
  html += commonStyles();
  html += "</head><body>";
  html += "<h2>Foerderband Steuerung</h2>";
  html += "<div class='version'>Firmware v" FIRMWARE_VERSION "</div>";

  // Status
  html += "<div class='status " + String(isRunning ? "running" : "idle") + "'>";
  html += "Status: <b>" + String(isRunning ? "laeuft" : "steht") + "</b> &nbsp;|&nbsp; ";
  html += "Lichtschranke: <b>" + String(digitalRead(sensorPin) == HIGH ? "ausgeloest" : "frei") + "</b>";
  html += "</div>";

  // Tabs
  html += "<div class='tabs'>";
  html += "<button class='tab active' id='t_settings' onclick='tab(\"settings\")'>Einstellungen</button>";
  html += "<button class='tab' id='t_firmware' onclick='tab(\"firmware\")'>Firmware</button>";
  html += "</div>";

  // --- Tab: Einstellungen ---
  html += "<div class='pane active' id='settings'>";
  html += "<form action='/set' method='GET'>";
  html += "<label>Geschwindigkeit (Delay in µs, kleiner = schneller):</label>";
  html += "<input type='number' name='delay' value='" + String((int)stepDelayUs) + "' min='100' max='5000'>";
  html += "<label>Umdrehungen pro Ausloesung:</label>";
  html += "<input type='number' name='umdr' id='umdr' value='" + String(umdrehungenSoll) + "' min='1' max='100' oninput='calc()'>";
  html += "<label>Walzendurchmesser inkl. Band (mm):</label>";
  html += "<input type='number' name='durchm' id='durchm' value='" + String(walzendurchmesser, 1) + "' min='1' max='200' step='0.1' oninput='calc()'>";
  html += "<div class='info' id='info'></div>";
  html += "<button type='submit'>Speichern</button>";
  html += "</form>";
  html += "<form action='/trigger' method='GET'>";
  html += "<button type='submit' class='sec'>Jetzt manuell starten</button>";
  html += "</form>";
  html += "<a class='reset' href='/wifi/reset'>WLAN neu einrichten</a>";
  html += "</div>";

  // --- Tab: Firmware ---
  html += "<div class='pane' id='firmware'>";
  html += "<p>Aktuelle Firmware: <b>v" FIRMWARE_VERSION "</b></p>";

  // Update-Check
  html += "<button type='button' class='sec' id='checkbtn' onclick='checkUpdate()' style='margin-top:0;margin-bottom:6px;'>Auf Updates prüfen</button>";
  html += "<div id='updcheck' style='margin-bottom:12px;'></div>";

  html += "<p>Waehle eine neue <code>.bin</code>-Datei aus und klicke auf Hochladen.<br>";
  html += "Das Geraet startet nach dem Update automatisch neu.</p>";
  html += "<form method='POST' action='/update' enctype='multipart/form-data' id='upd'>";
  html += "<label>Firmware-Datei (.bin):</label>";
  html += "<input type='file' name='firmware' accept='.bin' onchange='document.getElementById(\"updbtn\").disabled=false'>";
  html += "<progress id='prog' max='100' value='0'></progress>";
  html += "<button type='submit' id='updbtn' disabled>Firmware hochladen</button>";
  html += "</form>";
  html += "<script>";

  // Update-Check via GitHub raw (laeuft im Browser, nicht auf dem ESP)
  html += "function checkUpdate(){";
  html += "var btn=document.getElementById('checkbtn');";
  html += "var div=document.getElementById('updcheck');";
  html += "btn.disabled=true;btn.textContent='Prüfe...';";
  html += "fetch('https://raw.githubusercontent.com/samson2803/ESP-Foerderband/main/version.txt')";
  html += ".then(function(r){if(!r.ok)throw new Error('HTTP '+r.status);return r.text();})";
  html += ".then(function(t){";
  html += "var latest=t.trim();";
  html += "var cur='" FIRMWARE_VERSION "';";
  html += "if(latest===cur){";
  html += "div.innerHTML='<div class=\"upd-ok\">&#10003; Firmware ist aktuell (v'+cur+')</div>';";
  html += "}else{";
  html += "div.innerHTML='<div style=\"padding:10px;background:#fff3cd;border-radius:6px;color:#555;\">"
          "&#11014; Neue Version verfügbar: <b>v'+latest+'</b> &nbsp;&mdash;&nbsp;"
          "<a href=\"https://github.com/samson2803/ESP-Foerderband/releases\" target=\"_blank\">Download auf GitHub</a></div>';";
  html += "}";
  html += "btn.disabled=false;btn.textContent='Auf Updates prüfen';";
  html += "})";
  html += ".catch(function(e){";
  html += "div.innerHTML='<div class=\"upd-err\">Fehler beim Abruf: '+e.message+'</div>';";
  html += "btn.disabled=false;btn.textContent='Auf Updates prüfen';";
  html += "});}";

  // Upload-Fortschritt
  html += "document.getElementById('upd').onsubmit=function(e){";
  html += "var f=this.querySelector('input[type=file]').files[0];";
  html += "if(!f)return false;";
  html += "e.preventDefault();";
  html += "var fd=new FormData(this);";
  html += "var xhr=new XMLHttpRequest();";
  html += "xhr.open('POST','/update');";
  html += "var p=document.getElementById('prog');";
  html += "p.style.display='block';";
  html += "xhr.upload.onprogress=function(e){if(e.lengthComputable)p.value=Math.round(e.loaded/e.total*100);};";
  html += "xhr.onload=function(){";
  html += "if(xhr.status===200){";
  html += "document.getElementById('firmware').innerHTML='<div class=\"upd-ok\"><b>Update erfolgreich!</b> Geraet startet neu...<br>Seite laedt in 8 Sekunden neu.</div>';";
  html += "setTimeout(()=>location.href='/',8000);";
  html += "}else{";
  html += "document.getElementById('firmware').innerHTML='<div class=\"upd-err\"><b>Update fehlgeschlagen:</b> '+xhr.responseText+'</div>';";
  html += "}};";
  html += "xhr.send(fd);};";
  html += "</script>";
  html += "</div>";

  // Calc-Script + Tab-Script
  html += "<script>";
  html += "function calc(){";
  html += "var d=parseFloat(document.getElementById('durchm').value)||0;";
  html += "var u=parseInt(document.getElementById('umdr').value)||0;";
  html += "var umfang=(Math.PI*d).toFixed(1);";
  html += "var ges=(Math.PI*d*u).toFixed(1);";
  html += "document.getElementById('info').innerHTML='Weg/Umdrehung: <b>'+umfang+' mm</b> &nbsp;|&nbsp; Gesamtweg: <b>'+ges+' mm</b>';";
  html += "}calc();";
  html += "function tab(id){";
  html += "document.querySelectorAll('.pane').forEach(e=>e.classList.remove('active'));";
  html += "document.querySelectorAll('.tab').forEach(e=>e.classList.remove('active'));";
  html += "document.getElementById(id).classList.add('active');";
  html += "document.getElementById('t_'+id).classList.add('active');}";
  html += "</script>";

  html += "</body></html>";
  server.send(200, "text/html", html);
}

void handleSet() {
  if (server.hasArg("delay"))  stepDelayUs       = server.arg("delay").toFloat();
  if (server.hasArg("umdr"))   umdrehungenSoll   = server.arg("umdr").toInt();
  if (server.hasArg("durchm")) walzendurchmesser = server.arg("durchm").toFloat();
  saveMotorSettings();
  server.sendHeader("Location", "/");
  server.send(303);
}

void handleTrigger() {
  startBelt();
  server.sendHeader("Location", "/");
  server.send(303);
}

void handleWifiReset() {
  clearWifiCredentials();
  server.send(200, "text/html", "<p>WLAN-Daten geloescht. Geraet startet neu im Einrichtungsmodus.</p>");
  delay(1000);
  ESP.restart();
}

// Dieser String muss in jeder gueltigen Foerderband-Firmware enthalten sein.
const char FW_MAGIC[] = "FOERDERBAND_FW_MAGIC";
static const size_t MAGIC_LEN = sizeof(FW_MAGIC) - 1;

static bool    uploadMagicOk = false;
// Die letzten Bytes des vorherigen Blocks - sonst rutscht ein Treffer genau auf
// der Blockgrenze durch.
static uint8_t magicTail[MAGIC_LEN - 1];
static size_t  magicTailLen = 0;

static bool magicInBuffer(const uint8_t* data, size_t len) {
  if (len < MAGIC_LEN) return false;
  for (size_t i = 0; i + MAGIC_LEN <= len; i++) {
    if (memcmp(data + i, FW_MAGIC, MAGIC_LEN) == 0) return true;
  }
  return false;
}

// Sucht den Magic-String im laufenden Upload.
//
// Wichtig: FW_MAGIC liegt als const char[] im .rodata-Abschnitt, also weit
// hinten im Image (bei v1.3.x rund 356 kB tief). Ein Blick auf die ersten Bytes
// findet ihn nie - es muss der ganze Strom durchsucht werden. Genau daran ist
// der Browser-Upload bis v1.3.0 gescheitert.
static void magicScan(const uint8_t* data, size_t len) {
  if (uploadMagicOk || len == 0) return;

  // 1. Treffer, der ueber die Blockgrenze reicht
  if (magicTailLen > 0) {
    uint8_t bridge[2 * (MAGIC_LEN - 1)];
    size_t head = min(len, MAGIC_LEN - 1);
    memcpy(bridge, magicTail, magicTailLen);
    memcpy(bridge + magicTailLen, data, head);
    if (magicInBuffer(bridge, magicTailLen + head)) {
      uploadMagicOk = true;
      return;
    }
  }

  // 2. Treffer innerhalb dieses Blocks
  if (magicInBuffer(data, len)) {
    uploadMagicOk = true;
    return;
  }

  // 3. Ueberhang fuer den naechsten Block merken
  if (len >= MAGIC_LEN - 1) {
    memcpy(magicTail, data + len - (MAGIC_LEN - 1), MAGIC_LEN - 1);
    magicTailLen = MAGIC_LEN - 1;
  } else {
    size_t drop = (magicTailLen + len > MAGIC_LEN - 1)
                  ? (magicTailLen + len) - (MAGIC_LEN - 1)
                  : 0;
    memmove(magicTail, magicTail + drop, magicTailLen - drop);
    magicTailLen -= drop;
    memcpy(magicTail + magicTailLen, data, len);
    magicTailLen += len;
  }
}

void handleFirmwareUpdate() {
  server.sendHeader("Connection", "close");
  if (!uploadMagicOk) {
    server.send(400, "text/plain", "Falsche Firmware: Magic nicht gefunden. Nur Foerderband-Firmware kann per Browser hochgeladen werden.");
  } else if (Update.hasError()) {
    server.send(500, "text/plain", "Update fehlgeschlagen");
  } else {
    server.send(200, "text/plain", "OK");
    delay(500);
    ESP.restart();
  }
}

void handleFirmwareUpload() {
  HTTPUpload& upload = server.upload();

  if (upload.status == UPLOAD_FILE_START) {
    Serial.printf("HTTP OTA: %s\n", upload.filename.c_str());
    uploadMagicOk = false;
    magicTailLen  = 0;
    Update.begin((size_t)0xFFFFFFFF);

  } else if (upload.status == UPLOAD_FILE_WRITE) {
    magicScan(upload.buf, upload.currentSize);
    Update.write(upload.buf, upload.currentSize);

  } else if (upload.status == UPLOAD_FILE_END) {
    if (uploadMagicOk) {
      Update.end(true);
      Serial.printf("HTTP OTA fertig: %u Bytes\n", upload.totalSize);
    } else {
      // Der Magic sitzt am Ende des Images, das Urteil steht also erst jetzt fest.
      // Update.end(false) schreibt nichts fest - die laufende Firmware bleibt.
      Update.end(false);
      Serial.println("HTTP OTA: Firmware-Magic nicht gefunden - verworfen");
    }
  }
}

// ---------- Motorsteuerung ----------

// Bandweg pro Umdrehung = PI * Walzendurchmesser -> daraus die Schritte je Millimeter.
// Unter 1 mm gibt es keinen sinnvollen Walzendurchmesser (das ist auch die untere
// Grenze in /api/config). Die Abfrage faengt zugleich einen kaputten EEPROM-Wert ab:
// bei 0 oder einer denormalen Zahl kaeme sonst inf heraus.
float stepsPerMm() {
  if (!(walzendurchmesser >= 1.0f)) return 0.0f;
  return (float)stepsPerRevolution / (PI * walzendurchmesser);
}

// Startet einen Lauf ueber eine feste Schrittzahl. Liefert false, wenn das Band
// schon laeuft - ein laufender Auftrag wird nie unterbrochen.
bool startBeltSteps(long steps) {
  if (isRunning || steps <= 0) return false;
  stepsTotal     = steps;
  stepsRemaining = steps;
  digitalWrite(DIR_PIN, LOW);
  digitalWrite(ENA_PIN, LOW);
  isRunning = true;
  Serial.printf("- Stepper ON (%ld Schritte)\n", steps);
  return true;
}

// Lauf ueber die gespeicherte Umdrehungszahl (Lichtschranke, /trigger).
void startBelt() {
  startBeltSteps((long)stepsPerRevolution * umdrehungenSoll);
}

// Bricht einen laufenden Auftrag sofort ab und schaltet den Treiber stromlos.
// stepsRemaining bleibt absichtlich stehen: daran erkennt die App hinterher,
// dass abgebrochen wurde und wie weit das Band gekommen ist.
void stopBelt() {
  if (!isRunning) return;
  isRunning = false;
  digitalWrite(ENA_PIN, HIGH);
  Serial.printf("- Stepper OFF (Stop bei %ld von %ld Schritten)\n",
                stepsTotal - stepsRemaining, stepsTotal);
}

// ---------- JSON-API ----------

void sendJson(int code, const String& json) {
  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.send(code, "application/json", json);
}

void sendApiError(int code, const String& msg) {
  sendJson(code, "{\"ok\":false,\"error\":\"" + msg + "\"}");
}

// Vollstaendiger Geraetezustand - Antwort auf /api/status und auf jede Aktion,
// damit die App nach einem Befehl nicht extra nachfragen muss.
String statusJson() {
  float spm  = stepsPerMm();
  long  done = stepsTotal - stepsRemaining;
  if (done < 0) done = 0;

  String j = "{\"ok\":true";
  j += ",\"running\":"         + String(isRunning ? "true" : "false");
  j += ",\"sensor\":"          + String(digitalRead(sensorPin) == HIGH ? "true" : "false");
  j += ",\"steps_total\":"     + String(stepsTotal);
  j += ",\"steps_done\":"      + String(done);
  j += ",\"steps_remaining\":" + String(stepsRemaining);
  j += ",\"mm_total\":"        + String(spm > 0.0f ? stepsTotal / spm : 0.0f, 1);
  j += ",\"mm_done\":"         + String(spm > 0.0f ? done / spm : 0.0f, 1);
  j += ",\"steps_per_mm\":"    + String(spm, 3);
  j += ",\"steps_per_rev\":"   + String(stepsPerRevolution);
  j += ",\"delay_us\":"        + String((int)stepDelayUs);
  j += ",\"umdrehungen\":"     + String(umdrehungenSoll);
  j += ",\"walze_mm\":"        + String(walzendurchmesser, 1);
  j += ",\"version\":\"" FIRMWARE_VERSION "\"";
  j += ",\"ip\":\""            + WiFi.localIP().toString() + "\"";
  j += ",\"rssi\":"            + String(WiFi.RSSI());
  j += "}";
  return j;
}

void handleApiStatus() {
  sendJson(200, statusJson());
}

// /api/run?cm=30 | ?mm=300 | ?rev=2 | ?steps=5732
// Ohne Parameter faehrt die gespeicherte Umdrehungszahl (wie die Lichtschranke).
void handleApiRun() {
  if (isRunning) { sendApiError(409, "Band laeuft bereits"); return; }

  long steps;
  if (server.hasArg("steps")) {
    steps = server.arg("steps").toInt();
  } else if (server.hasArg("mm") || server.hasArg("cm")) {
    float mm = server.hasArg("mm") ? server.arg("mm").toFloat()
                                   : server.arg("cm").toFloat() * 10.0f;
    float spm = stepsPerMm();
    if (spm <= 0.0f) { sendApiError(500, "Walzendurchmesser ungueltig"); return; }
    steps = lround(mm * spm);
  } else if (server.hasArg("rev")) {
    steps = lround(server.arg("rev").toFloat() * stepsPerRevolution);
  } else {
    steps = (long)stepsPerRevolution * umdrehungenSoll;
  }

  if (steps <= 0)            { sendApiError(400, "Strecke muss groesser als 0 sein"); return; }
  if (steps > MAX_RUN_STEPS) { sendApiError(400, "Strecke zu gross"); return; }

  startBeltSteps(steps);
  sendJson(200, statusJson());
}

void handleApiStop() {
  stopBelt();
  sendJson(200, statusJson());
}

// Ohne Parameter lesend, mit Parametern schreibend (dann auch im EEPROM gesichert).
void handleApiConfig() {
  bool changed = false;

  if (server.hasArg("delay")) {
    float v = server.arg("delay").toFloat();
    if (v < 100.0f || v > 5000.0f) { sendApiError(400, "delay muss zwischen 100 und 5000 liegen"); return; }
    stepDelayUs = v; changed = true;
  }
  if (server.hasArg("umdr")) {
    int v = server.arg("umdr").toInt();
    if (v < 1 || v > 100) { sendApiError(400, "umdr muss zwischen 1 und 100 liegen"); return; }
    umdrehungenSoll = v; changed = true;
  }
  if (server.hasArg("durchm")) {
    float v = server.arg("durchm").toFloat();
    if (v < 1.0f || v > 200.0f) { sendApiError(400, "durchm muss zwischen 1 und 200 liegen"); return; }
    walzendurchmesser = v; changed = true;
  }

  if (changed) saveMotorSettings();
  sendJson(200, statusJson());
}

// ---------- WLAN-Einrichtungsseite (AP-Modus) ----------
void handleWifiForm() {
  String html = "<!DOCTYPE html><html><head><meta charset='utf-8'>";
  html += "<meta name='viewport' content='width=device-width, initial-scale=1'>";
  html += "<title>WLAN einrichten</title>";
  html += commonStyles();
  html += "</head><body>";
  html += "<h2>Foerderband: WLAN einrichten</h2>";
  html += "<p>Kein WLAN konfiguriert oder Verbindung fehlgeschlagen. Bitte Zugangsdaten eingeben:</p>";
  html += "<form action='/wifi/save' method='POST'>";
  html += "<label>WLAN-Name (SSID):</label><input type='text' name='ssid' maxlength='32' required>";
  html += "<label>WLAN-Passwort:</label><input type='password' name='pass' maxlength='64'>";
  html += "<button type='submit'>Speichern &amp; verbinden</button>";
  html += "</form>";
  html += "</body></html>";
  server.send(200, "text/html", html);
}

void handleWifiSave() {
  if (!server.hasArg("ssid") || server.arg("ssid").length() == 0) {
    server.sendHeader("Location", "/");
    server.send(303);
    return;
  }
  String ssid = server.arg("ssid");
  String pass = server.hasArg("pass") ? server.arg("pass") : "";
  Serial.printf("WLAN-Daten empfangen: SSID=[%s] Laenge=%d\n", ssid.c_str(), ssid.length());
  saveWifiCredentials(ssid, pass);
  String html = "<!DOCTYPE html><html><head><meta charset='utf-8'></head><body>";
  html += "<p>Gespeichert. Verbinde mit '" + ssid + "'...</p>";
  html += "</body></html>";
  server.send(200, "text/html", html);
  delay(1000);
  ESP.restart();
}

// ---------- WLAN-Verbindung ----------
bool connectToWifi(const char* ssid, const char* pass, unsigned long timeoutMs) {
  Serial.printf("Verbinde mit WLAN: [%s]\n", ssid);
  WiFi.disconnect(true);
  delay(200);
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, pass);
  unsigned long start = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - start < timeoutMs) {
    delay(500);
    Serial.print(".");
  }
  Serial.println();
  Serial.printf("WiFi Status: %d\n", WiFi.status());
  if (WiFi.status() == WL_CONNECTED) {
    Serial.print("Verbunden! IP: ");
    Serial.println(WiFi.localIP());
    return true;
  }
  if (WiFi.status() == WL_NO_SSID_AVAIL) Serial.println("Fehler: SSID nicht gefunden (2,4GHz?)");
  if (WiFi.status() == WL_WRONG_PASSWORD) Serial.println("Fehler: Falsches Passwort");
  if (WiFi.status() == WL_CONNECT_FAILED) Serial.println("Fehler: Verbindung fehlgeschlagen");
  return false;
}

void startApMode() {
  apMode = true;
  WiFi.mode(WIFI_AP);
  WiFi.softAP(apSsid, apPassword);
  IPAddress apIP = WiFi.softAPIP();
  Serial.printf("AP-Modus: WLAN '%s', http://192.168.4.1\n", apSsid);
  dnsServer.start(DNS_PORT, "*", apIP);
  server.on("/", handleWifiForm);
  server.on("/wifi/save", handleWifiSave);
  server.onNotFound([]() {
    server.sendHeader("Location", "/", true);
    server.send(302, "text/plain", "");
  });
}

// ---------- Setup & Loop ----------
void setup() {
  Serial.begin(115200);
  Serial.printf("\nFoerderband Steuerung v%s\n", FIRMWARE_VERSION);
  pinMode(STEP_PIN, OUTPUT);
  pinMode(DIR_PIN, OUTPUT);
  pinMode(ENA_PIN, OUTPUT);
  digitalWrite(ENA_PIN, HIGH);
  pinMode(sensorPin, INPUT);

  loadWifiCredentials();
  loadMotorSettings();

  bool connected = false;
  if (strlen(storedSsid) > 0) {
    connected = connectToWifi(storedSsid, storedPass, 15000);
  } else {
    Serial.println("Kein WLAN gespeichert.");
  }

  if (connected) {
    server.on("/", handleRoot);
    server.on("/set", handleSet);
    server.on("/trigger", handleTrigger);
    server.on("/wifi/reset", handleWifiReset);
    server.on("/update", HTTP_POST, handleFirmwareUpdate, handleFirmwareUpload);

    // JSON-API fuer Desktop-App und Skripte (GET wie POST, damit im Browser testbar)
    server.on("/api/status", handleApiStatus);
    server.on("/api/run",    handleApiRun);
    server.on("/api/stop",   handleApiStop);
    server.on("/api/config", handleApiConfig);

    ArduinoOTA.setHostname("foerderband");
    ArduinoOTA.onStart([]() { Serial.println("ArduinoOTA startet..."); });
    ArduinoOTA.onEnd([]()   { Serial.println("ArduinoOTA fertig."); });
    ArduinoOTA.onError([](ota_error_t e) { Serial.printf("ArduinoOTA Fehler[%u]\n", e); });
    ArduinoOTA.begin();
    Serial.println("OTA bereit (foerderband.local) + HTTP-Update unter /update");
  } else {
    startApMode();
  }

  server.begin();
}

void loop() {
  server.handleClient();
  if (!apMode) ArduinoOTA.handle();
  if (apMode)  dnsServer.processNextRequest();

  int sensorState = digitalRead(sensorPin);
  if (sensorState == HIGH && lastSensorState == LOW) {
    startBelt();
  }
  lastSensorState = sensorState;

  if (isRunning && stepsRemaining > 0) {
    digitalWrite(STEP_PIN, HIGH);
    delayMicroseconds((int)stepDelayUs);
    digitalWrite(STEP_PIN, LOW);
    delayMicroseconds((int)stepDelayUs);
    stepsRemaining--;
  } else if (isRunning && stepsRemaining <= 0) {
    isRunning = false;
    digitalWrite(ENA_PIN, HIGH);
    Serial.println("- Stepper OFF");
  }
}
