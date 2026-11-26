# Testprotokoll – LehrerPlan (Gruppe 09-21)

Testdatum: 23.11.2026 (Test 7: 26.11.2026) · Getesteter Commit: 022e40d + noch nicht committete Logging-Änderungen (siehe unten) – **nach dem Commit des Codes hier den neuen Kurz-Hash eintragen** (`git rev-parse --short HEAD`)

Getestet mit Unity 6000.3.24f1, Szene `Assets/MyGame/Scenes/Gangplan.unity`, Ausgabe in der Unity Console (`Debug.Log`).

| Nr. | Durchführung | Soll | Ist | Status |
|---|---|---|---|---|
| 1 | Frische Play-Session, Tür **B313** geklickt. Angabe im Popup mit der Detailseite derselben Lehrkraft verglichen: `/lehrerinnen-details/feichtinger-daniel-prof-mag-b-313.html` (Tel-DW: 229). | Die Anzeige zeigt +43 662 453610- plus die Durchwahl. | Popup: `Tel: +43 662 453610-229`. Detailseite Feichtinger: Raum B 313, Tel-DW 229. | Bestanden |
| 2 | Räume **B313** und **B310** nacheinander geöffnet und mit der Schulwebsite (`lehrerinnen.html` + Detailseiten) verglichen. | Raumnummer und Personen stimmen für beide Räume. | B313: Feichtinger Daniel, Lachinger Manuela, Schneeweiß Birgit (Tel …-229). B310: Illmer Michael, Kowarik Thomas, Mulic Elma (Tel …-422). Website: dieselben Personen, Raum B 313 bzw. B 310, DW 229 bzw. 422. | Bestanden |
| 3 | Frische Play-Session, Tür **B313**, Lehrkraft Feichtinger. Eintrag für `https://www.htl-salzburg.ac.at/lehrerinnen-details/feichtinger-daniel-prof-mag-b-313.html` in der Console geprüft. | Für diese URL erscheint HTTP 200 (Success). | `[Request-Ende] Detailseite: …/feichtinger-daniel-prof-mag-b-313.html -> HTTP 200 (Success) -> Feichtinger Daniel, Prof. Mag., Raum B313, Tel-DW 229` | Bestanden |
| 4 | Frische Play-Session, erster Türklick (B313). Abruf von `https://www.htl-salzburg.ac.at/lehrerinnen.html` in der Console geprüft. | Für diese URL erscheint HTTP 200 (Success). | `[Request-Start] Übersicht: …/lehrerinnen.html`, danach `[Request-Ende] Übersicht: …/lehrerinnen.html -> HTTP 200 (Success) -> 268 Lehrer:innen gefunden` | Bestanden |
| 5 | Frische Play-Session, zuerst **B313**, dann **B310** geöffnet. Request-Starts beider Arten (Übersicht / Detailseite) in der Console gezählt. | Die Übersicht wird einmal geladen. Für den zweiten Raum starten passende Detailrequests. | Nur 1× `[Request-Start] Übersicht` (bei B313). Bei B310: `[Cache] Übersicht ist schon geladen -> kein neuer Request`, danach 3× `[Request-Start] Detailseite (Raum B310)` für illmer-…-b-310, kowarik-…-b-310, mulic-…-b-310, alle HTTP 200 (Success). | Bestanden |
| 6 | Frische Play-Session, **B313** geöffnet und fertig laden lassen (1 Übersicht + 3 Detailrequests). Popup durch erneuten Klick auf die Tür geschlossen, Console mit *Clear* geleert, Tür **B313** erneut geöffnet. | Dieselben Angaben erscheinen ohne neuen Request. | Popup wieder: `Tel: +43 662 453610-229`, Feichtinger / Lachinger / Schneeweiß. Console enthält nur `[Cache] Raum B313 ist schon geladen -> kein neuer Request (3 Lehrer:innen)`, kein `[Request-Start]`. | Bestanden |
| 7 | Am 26.11.2026 (gleicher Code-Stand): WLAN ausgeschaltet, Unity frisch gestartet, Play, Tür **C312** geklickt. Abruf von `https://www.htl-salzburg.ac.at/lehrerinnen.html` in der Console geprüft. | Das Popup zeigt „Lehrerliste konnte nicht geladen werden“ und den technischen Fehler. | Popup: `Raum C312` – `Lehrerliste konnte nicht geladen werden: Cannot resolve destination host` (rot). Console: `[Request-Ende] Übersicht: https://www.htl-salzburg.ac.at/lehrerinnen.html -> HTTP 0 (ConnectionError) Fehler: Cannot resolve destination host` | Bestanden |

## Für die Tests geänderter Code

Damit die Tests überhaupt prüfbar sind, wurden vor dem Testen zwei Dinge ergänzt (Stand: Commit 022e40d + diese Änderungen):

- **Logging in `HtlTeacherService.cs`**
  - `[Request-Start] Übersicht: <URL>` bzw. `[Request-Start] Detailseite (Raum X): <URL>` – direkt vor `SendWebRequest()`.
  - `[Request-Ende] … <URL> -> HTTP <Status> (<Result>)` plus Ergebnis (Anzahl Lehrer:innen bzw. Name, Raum, Tel-DW). Bei Fehlern als Warnung inkl. `request.error`.
  - `[Cache] …` wenn die Übersicht oder ein Raum schon geladen ist und **kein** Request startet.
- **Reset der `static`-Variablen bei jedem Play-Start** (`[RuntimeInitializeOnLoadMethod]` in `HtlTeacherService` und `DoorPopupController`).
  Im Projekt ist *Enter Play Mode Options → Domain Reload* ausgeschaltet. Ohne Reset blieben Übersicht und Raum-Cache vom letzten Play-Durchlauf erhalten – eine „frische Sitzung“ (Tests 3–5) hätte dann gar keinen Request mehr gestartet.

## Screenshots

- Tests 1, 3, 4 – B313 nach dem ersten Klick: [docs/tests/test1-4_B313.jpg](docs/tests/test1-4_B313.jpg)
- Tests 2, 5 – B310 als zweiter Raum: [docs/tests/test2-5_B310.jpg](docs/tests/test2-5_B310.jpg)
- Test 6 – erneuter Besuch, nur Cache-Meldung: [docs/tests/test6_erneuter-besuch.jpg](docs/tests/test6_erneuter-besuch.jpg)
- Test 7 – ohne Internet, Fehlermeldung im Popup (C312): [docs/tests/test7_offline_C312.png](docs/tests/test7_offline_C312.png)

## Console-Auszug (Tests 1–5, eine frische Play-Session)

```
[Request-Start] Übersicht: https://www.htl-salzburg.ac.at/lehrerinnen.html
[Request-Ende] Übersicht: https://www.htl-salzburg.ac.at/lehrerinnen.html -> HTTP 200 (Success) -> 268 Lehrer:innen gefunden
[Request-Start] Detailseite (Raum B313): https://www.htl-salzburg.ac.at/lehrerinnen-details/feichtinger-daniel-prof-mag-b-313.html
[Request-Start] Detailseite (Raum B313): https://www.htl-salzburg.ac.at/lehrerinnen-details/lachinger-manuela-prof-mag-phil-b-313.html
[Request-Start] Detailseite (Raum B313): https://www.htl-salzburg.ac.at/lehrerinnen-details/schneewei%C3%9F-birgit-prof-mmmag-b-313.html
[Request-Ende] Detailseite: https://www.htl-salzburg.ac.at/lehrerinnen-details/feichtinger-daniel-prof-mag-b-313.html -> HTTP 200 (Success) -> Feichtinger Daniel, Prof. Mag., Raum B313, Tel-DW 229
[Request-Ende] Detailseite: https://www.htl-salzburg.ac.at/lehrerinnen-details/lachinger-manuela-prof-mag-phil-b-313.html -> HTTP 200 (Success) -> Lachinger Manuela, Prof. Mag.phil, Raum B313, Tel-DW 229
[Request-Ende] Detailseite: https://www.htl-salzburg.ac.at/lehrerinnen-details/schneewei%C3%9F-birgit-prof-mmmag-b-313.html -> HTTP 200 (Success) -> Schneeweiß Birgit, Prof. MMMag., Raum B313, Tel-DW 229
[Cache] Übersicht ist schon geladen -> kein neuer Request für https://www.htl-salzburg.ac.at/lehrerinnen.html
[Request-Start] Detailseite (Raum B310): https://www.htl-salzburg.ac.at/lehrerinnen-details/illmer-michael-prof-mag-b-310.html
[Request-Start] Detailseite (Raum B310): https://www.htl-salzburg.ac.at/lehrerinnen-details/kowarik-thomas-prof-dipl-ing-dr-b-310.html
[Request-Start] Detailseite (Raum B310): https://www.htl-salzburg.ac.at/lehrerinnen-details/mulic-elma-prof-med-b-310.html
[Request-Ende] Detailseite: https://www.htl-salzburg.ac.at/lehrerinnen-details/illmer-michael-prof-mag-b-310.html -> HTTP 200 (Success) -> Illmer Michael, Prof. Mag., Raum B310, Tel-DW 422
[Request-Ende] Detailseite: https://www.htl-salzburg.ac.at/lehrerinnen-details/kowarik-thomas-prof-dipl-ing-dr-b-310.html -> HTTP 200 (Success) -> Kowarik Thomas, Prof. Dipl.-Ing. Dr., Raum B310, Tel-DW 422
[Request-Ende] Detailseite: https://www.htl-salzburg.ac.at/lehrerinnen-details/mulic-elma-prof-med-b-310.html -> HTTP 200 (Success) -> Mulic Elma, Prof. MEd, Raum B310, Tel-DW 422
```
