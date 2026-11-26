using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

// Holt die Lehrer:innen eines Raums von der HTL-Website.
//
// Ablauf:
//  1. Die Übersichtsseite "lehrerinnen.html" laden. Dort steht pro Lehrer:in nur der Name
//     und ein Link zur Detailseite. Der Raum steckt am Ende des Links, z.B.
//     "/lehrerinnen-details/feichtinger-daniel-prof-mag-b-313.html"  ->  Raum B313
//  2. Für jede Lehrperson im gesuchten Raum die Detailseite laden
//     und dort Name, Raum und Telefon-Durchwahl auslesen.
public static class HtlTeacherService
{
    const string BaseUrl = "https://www.htl-salzburg.ac.at";
    const string ListUrl = BaseUrl + "/lehrerinnen.html";
    const string PhonePrefix = "+43 662 453610-";

    // ---------------------------------------------------------------------
    // Regex 1: ein Eintrag auf der Übersichtsseite
    //
    // Der HTML-Code sieht so aus:
    //   <a class="value" href="/lehrerinnen-details/mueller-...-b-313.html"><span class="text">Müller Hans</span>
    //
    //   (?<link>/lehrerinnen-details/[^"]+)  -> merkt sich den Link als "link".
    //                                           [^"]+ = alle Zeichen bis zum nächsten "
    //   (?<name>[^<]+)                       -> merkt sich den Namen als "name".
    //                                           [^<]+ = alle Zeichen bis zum nächsten <
    //
    // In C# muss man in einem @"..." String ein " doppelt schreiben, deshalb steht da "".
    // ---------------------------------------------------------------------
    static Regex listEntryRegex = new Regex(
        @"<a class=""value"" href=""(?<link>/lehrerinnen-details/[^""]+)""><span class=""text"">(?<name>[^<]+)</span>");

    // ---------------------------------------------------------------------
    // Regex 2: eine Raumnummer finden, z.B. in "Tür B310", "b 313", "-b-313.html"
    //
    //   (?<building>[A-Za-z])  -> ein Buchstabe = Gebäude/Gang (B, C, D ...)
    //   [\s-]*                 -> danach beliebig viele Leerzeichen oder Bindestriche
    //   (?<number>\d{3})       -> genau drei Ziffern = Raumnummer (\d = Ziffer)
    // ---------------------------------------------------------------------
    static Regex roomRegex = new Regex(@"(?<building>[A-Za-z])[\s-]*(?<number>\d{3})");

    // Die Übersichtsseite (wird nur einmal geladen und dann gemerkt)
    static string listHtml = null;
    static bool listIsLoading = false;

    // Schon geladene Räume merken: Raum -> Lehrer:innen
    static Dictionary<string, List<TeacherInfo>> cache = new Dictionary<string, List<TeacherInfo>>();

    // Im Projekt ist "Enter Play Mode Options -> Domain Reload" ausgeschaltet. Dann behalten
    // static-Variablen ihren Wert vom letzten Play-Durchlauf. Damit jede Play-Session frisch
    // startet (Übersicht + Cache leer), wird hier beim Start alles zurückgesetzt.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        listHtml = null;
        listIsLoading = false;
        cache = new Dictionary<string, List<TeacherInfo>>();
    }

    // Log-Zeile nach einem Request: URL, HTTP-Status und Ergebnis (Success, ConnectionError, ...)
    static string DescribeRequest(UnityWebRequest request)
    {
        string text = request.url + " -> HTTP " + request.responseCode + " (" + request.result + ")";
        if (request.result != UnityWebRequest.Result.Success)
            text += " Fehler: " + request.error;
        return text;
    }


    // Macht aus einem Text eine einheitliche Raumnummer.
    // "Tür b310" -> "B310",  "B 313" -> "B313",  kein Raum gefunden -> null
    public static string NormalizeRoom(string text)
    {
        if (text == null)
            return null;

        Match match = roomRegex.Match(text);
        if (!match.Success)
            return null;

        return match.Groups["building"].Value.ToUpper() + match.Groups["number"].Value;
    }

    // Durchwahl "229" -> "+43 662 453610-229"
    public static string FormatPhone(string extension)
    {
        return PhonePrefix + extension;
    }


    // Coroutine: sucht alle Lehrer:innen in einem Raum.
    // Wenn sie fertig ist, wird onSuccess (mit der Liste) oder onError (mit einer Fehlermeldung) aufgerufen.
    public static IEnumerator LoadTeachersInRoom(string room, Action<List<TeacherInfo>> onSuccess, Action<string> onError)
    {
        room = NormalizeRoom(room);
        if (room == null)
        {
            onError("Ungültige Raumnummer.");
            yield break;
        }

        // Raum schon einmal geladen? Dann das gemerkte Ergebnis verwenden.
        if (cache.ContainsKey(room))
        {
            Debug.Log("[Cache] Raum " + room + " ist schon geladen -> kein neuer Request (" + cache[room].Count + " Lehrer:innen)");
            onSuccess(cache[room]);
            yield break;
        }

        // ----- Schritt 1: Übersichtsseite laden (nur beim allerersten Mal) -----

        // Lädt gerade eine andere Tür die Übersicht? Dann warten, bis sie fertig ist.
        while (listIsLoading)
            yield return null;

        if (listHtml == null)
        {
            listIsLoading = true;
            UnityWebRequest listRequest = UnityWebRequest.Get(ListUrl);
            Debug.Log("[Request-Start] Übersicht: " + ListUrl);
            yield return listRequest.SendWebRequest();
            listIsLoading = false;

            if (listRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[Request-Ende] Übersicht: " + DescribeRequest(listRequest));
                onError("Lehrerliste konnte nicht geladen werden:\n" + listRequest.error);
                listRequest.Dispose();
                yield break;
            }

            listHtml = listRequest.downloadHandler.text;
            Debug.Log("[Request-Ende] Übersicht: " + DescribeRequest(listRequest) +
                      " -> " + listEntryRegex.Matches(listHtml).Count + " Lehrer:innen gefunden");
            listRequest.Dispose();
        }
        else
        {
            Debug.Log("[Cache] Übersicht ist schon geladen -> kein neuer Request für " + ListUrl);
        }

        // Alle Einträge der Übersicht durchgehen und die Lehrer:innen des gesuchten Raums sammeln
        List<TeacherInfo> teachers = new List<TeacherInfo>();
        foreach (Match match in listEntryRegex.Matches(listHtml))
        {
            string link = match.Groups["link"].Value;
            string name = match.Groups["name"].Value;

            // Raum steht am Ende des Links: ".../name-b-313.html" -> "B313"
            string roomInLink = NormalizeRoom(GetLinkEnding(link));
            if (roomInLink != room)
                continue;

            TeacherInfo teacher = new TeacherInfo();
            teacher.Name = WebUtility.HtmlDecode(name).Trim();   // z.B. "&uuml;" -> "ü"
            teacher.Room = roomInLink;
            teacher.DetailUrl = BaseUrl + link;
            teachers.Add(teacher);
        }

        // ----- Schritt 2: Detailseiten laden -----

        // Alle Anfragen gleichzeitig starten (geht schneller als eine nach der anderen)
        List<UnityWebRequest> requests = new List<UnityWebRequest>();
        foreach (TeacherInfo teacher in teachers)
        {
            UnityWebRequest request = UnityWebRequest.Get(teacher.DetailUrl);
            Debug.Log("[Request-Start] Detailseite (Raum " + room + "): " + teacher.DetailUrl);
            request.SendWebRequest();
            requests.Add(request);
        }

        // Warten, bis jede Anfrage fertig ist
        foreach (UnityWebRequest request in requests)
        {
            while (!request.isDone)
                yield return null;
        }

        // Ergebnisse auswerten. requests[i] gehört zu teachers[i].
        List<TeacherInfo> result = new List<TeacherInfo>();
        for (int i = 0; i < teachers.Count; i++)
        {
            TeacherInfo teacher = teachers[i];
            UnityWebRequest request = requests[i];

            if (request.result == UnityWebRequest.Result.Success)
            {
                string html = request.downloadHandler.text;

                // Die Detailseite ist aktueller als der Link.
                // Steht dort ein anderer Raum, gehört die Person nicht (mehr) in diesen Raum.
                string roomOnPage = NormalizeRoom(ReadField(html, "Raum"));
                if (roomOnPage != null && roomOnPage != room)
                {
                    Debug.Log("[Request-Ende] Detailseite: " + DescribeRequest(request) +
                              " -> Raum laut Detailseite " + roomOnPage + ", wird für " + room + " ignoriert");
                    request.Dispose();
                    continue;
                }

                string fullName = ReadField(html, "Lehrername");
                if (fullName != null)
                    teacher.Name = fullName;

                teacher.PhoneExtension = ReadField(html, "Telefon");
                Debug.Log("[Request-Ende] Detailseite: " + DescribeRequest(request) +
                          " -> " + teacher.Name + ", Raum " + (roomOnPage ?? "?") +
                          ", Tel-DW " + (teacher.PhoneExtension ?? "-"));
            }
            else
            {
                Debug.LogWarning("[Request-Ende] Detailseite: " + DescribeRequest(request));
            }
            // Falls die Detailseite nicht geladen werden konnte, bleibt die Person
            // trotzdem in der Liste (nur ohne Telefonnummer).

            result.Add(teacher);
            request.Dispose();
        }

        cache[room] = result;
        onSuccess(result);
    }


    /// <summary>
    /// Gibt den letzten Teil eines Links zurück, damit nur dort nach dem Raum gesucht wird.
    /// "/lehrerinnen-details/feichtinger-daniel-prof-mag-b-313.html" -> "b-313.html"
    /// </summary>
    static string GetLinkEnding(string link)
    {
        // Regex: ein Buchstabe, Bindestrich, drei Ziffern, ".html" ganz am Ende ($ = Ende des Textes)
        Match match = Regex.Match(link, @"[a-zA-Z]-\d{3}\.html$");
        if (match.Success)
            return match.Value;
        return null;
    }

    // Liest ein Feld aus einer Detailseite aus. Der HTML-Code sieht z.B. so aus:
    //
    //   &lt;div class="field Telefon"&gt;
    //     &lt;div class="label"&gt;Tel-DW:&lt;/div&gt;
    //     &lt;div class="value"&gt; &lt;span class="text"&gt;229&lt;/span&gt;
    //
    // ReadField(html, "Telefon") gibt dann "229" zurück.
    // Ist das Feld leer oder "-", kommt null zurück.
    static string ReadField(string html, string fieldName)
    {
        // Regex-Erklärung:
        //   \s*                 -> beliebig viele Leerzeichen / Zeilenumbrüche
        //   [^<]*               -> beliebiger Text bis zum nächsten <  (hier: das Label, z.B. "Tel-DW:")
        //   (?<value>[^<]*)     -> der gesuchte Wert, wird als "value" gemerkt
        string pattern =
            @"<div class=""field " + fieldName + @""">\s*" +
            @"<div class=""label"">[^<]*</div>\s*" +
            @"<div class=""value"">\s*<span class=""text"">(?<value>[^<]*)</span>";

        Match match = Regex.Match(html, pattern);
        if (!match.Success)
            return null;

        string value = WebUtility.HtmlDecode(match.Groups["value"].Value).Trim();
        if (value == "" || value == "-")
            return null;

        return value;
    }
}
