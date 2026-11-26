# 09-21-Lehrerplan

Gangplan der HTL Salzburg (3. Stock, Gänge B, C, D) in Unity. Die Lehrerzimmer sind eingezeichnet;
ein Klick auf eine Tür zeigt, welche Lehrer:innen im Raum sitzen und unter welcher Telefonnummer
der Raum erreichbar ist. Die Daten kommen live von https://www.htl-salzburg.ac.at/lehrerinnen.html.

## Starten

Szene `Assets/MyGame/Scenes/Gangplan.unity` öffnen → Play → auf eine Tür klicken.
Nochmal auf die Tür oder auf das Popup klicken schließt es.

## Scripts (`Assets/MyGame/Scripts`)

| Datei | Aufgabe |
|---|---|
| `DoorPopupController.cs` | Hängt an jeder Tür. Liest die Raumnummer aus dem Objektnamen (`Tür B310`), startet das Laden und zeigt das Ergebnis im Popup an. |
| `HtlTeacherService.cs` | WebRequests + Regex. Lädt die Lehrerliste und die Detailseiten. |
| `TeacherInfo.cs` | Datenklasse (Name, Raum, Durchwahl, Link). |

## Ablauf WebRequest + Regex

1. `UnityWebRequest.Get("…/lehrerinnen.html")` – die Übersicht enthält nur Name + Link.
   Regex: `<a class="value" href="(?<link>/lehrerinnen-details/[^"]+)"><span class="text">(?<name>[^<]+)</span>`
2. Der Raum steht am Ende des Links (`…-b-313.html`).
   Regex: `[a-zA-Z]-\d{3}\.html$` schneidet das Ende (`b-313.html`) ab, danach macht
   `(?<building>[A-Za-z])[\s-]*(?<number>\d{3})` daraus `B313` → nur Lehrer:innen des geklickten Raums bleiben übrig.
3. Deren Detailseiten werden parallel geladen; Raum und Durchwahl werden ausgelesen:
   `<div class="field Telefon">\s*<div class="label">[^<]*</div>\s*<div class="value">\s*<span class="text">(?<value>[^<]*)</span>`
4. Telefonnummer = `+43 662 453610-` + Durchwahl (Tel-DW).

Die Übersichtsseite wird nur einmal geladen, Ergebnisse pro Raum werden zwischengespeichert.

## Logging & Tests

Jeder Request wird in der Unity Console geloggt: `[Request-Start]` direkt vor dem Aufruf,
`[Request-Ende]` mit URL, HTTP-Status, Result und Ergebnis, `[Cache]` wenn kein Request nötig ist.
Weil *Domain Reload* beim Play-Start ausgeschaltet ist, werden die `static`-Daten bei jedem Play-Start
zurückgesetzt – jede Play-Session startet also frisch.

Testergebnisse: [TESTPROTOKOLL.md](TESTPROTOKOLL.md)
