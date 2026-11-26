using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Hängt an jeder Tür im Gangplan.
/// Klick auf die Tür (Button -> ShowPopup) öffnet ein Popup und lädt über den
/// HtlTeacherService, welche Lehrer:innen im Raum sitzen und wie die Telefonnummer ist.
public class DoorPopupController : MonoBehaviour
{
    // Wird im Inspector gesetzt: das Popup-Fenster, das zu dieser Tür gehört
    public GameObject popup;

    // Raumnummer, z.B. "B310". Bleibt sie leer, wird sie aus dem Objektnamen ("Tür B310") gelesen.
    public string roomNumber;

    // Breite des Popups in Pixeln
    public float popupWidth = 380f;

    // Das Popup, das gerade offen ist (es soll immer nur eines offen sein).
    // "static" = alle Türen teilen sich diese eine Variable.
    static DoorPopupController currentlyOpen;

    // Domain Reload ist im Projekt aus -> static-Variable bei jedem Play-Start zurücksetzen
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        currentlyOpen = null;
    }

    RectTransform popupRect;
    RectTransform canvasRect;
    Text popupText;
    bool isLoading = false;


    void Awake()
    {
        // Raumnummer aus dem Namen holen, falls keine eingetragen ist
        if (string.IsNullOrEmpty(roomNumber))
        {
            roomNumber = HtlTeacherService.NormalizeRoom(name);
            if (roomNumber == null)
                roomNumber = name;
        }

        // Das Popup ist fix in der Szene angelegt (direkt unter dem Canvas, Anker in der Mitte,
        // inaktiv). Sein Button ruft beim Klick HidePopup() auf, der Text ist im Inspector
        // eingestellt (Rich Text, oben links, Umbruch). Hier werden nur die Referenzen geholt.
        popupText = popup.GetComponentInChildren<Text>(true);
        popupRect = popup.GetComponent<RectTransform>();
        canvasRect = GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
    }


    // Wird vom Button der Tür aufgerufen.
    public void ShowPopup()
    {
        // Ist das Popup dieser Tür schon offen? Dann schließt der zweite Klick es wieder.
        if (currentlyOpen == this)
        {
            HidePopup();
            return;
        }

        // Ein anderes Popup ist offen -> zuerst schließen
        if (currentlyOpen != null)
            currentlyOpen.HidePopup();

        currentlyOpen = this;
        popup.SetActive(true);
        popupRect.SetAsLastSibling();   // ganz nach vorne holen
        SetText("<b>Raum " + roomNumber + "</b>\n\nLade Daten von htl-salzburg.ac.at ...");

        // Daten laden (aber nicht doppelt, falls schon geladen wird)
        if (!isLoading)
        {
            isLoading = true;
            StartCoroutine(HtlTeacherService.LoadTeachersInRoom(roomNumber, ShowTeachers, ShowError));
        }
    }

    public void HidePopup()
    {
        popup.SetActive(false);
        if (currentlyOpen == this)
            currentlyOpen = null;
    }


    // Wird aufgerufen, wenn die Lehrer:innen fertig geladen sind.
    void ShowTeachers(List<TeacherInfo> teachers)
    {
        isLoading = false;
        string text = "<b>Raum " + roomNumber + "</b>\n";

        if (teachers.Count == 0)
        {
            text += "\nKeine Lehrer:innen in diesem Raum gefunden.";
            SetText(text);
            return;
        }

        // Alle verschiedenen Durchwahlen im Raum sammeln (ohne doppelte)
        List<string> extensions = new List<string>();
        foreach (TeacherInfo teacher in teachers)
        {
            if (teacher.PhoneExtension != null && !extensions.Contains(teacher.PhoneExtension))
                extensions.Add(teacher.PhoneExtension);
        }

        // Zeile mit den Telefonnummern
        text += "Tel: ";
        if (extensions.Count == 0)
        {
            text += "unbekannt";
        }
        else
        {
            for (int i = 0; i < extensions.Count; i++)
            {
                if (i > 0)
                    text += ", ";
                text += HtlTeacherService.FormatPhone(extensions[i]);
            }
        }

        // Liste der Lehrer:innen
        text += "\n\n<b>Lehrer:innen:</b>";
        foreach (TeacherInfo teacher in teachers)
        {
            text += "\n• " + teacher.Name;

            // Gibt es im Raum mehrere Durchwahlen, steht bei jeder Person ihre eigene dabei
            if (extensions.Count > 1 && teacher.PhoneExtension != null)
                text += "  (DW " + teacher.PhoneExtension + ")";
        }

        SetText(text);
    }

    // Wird aufgerufen, wenn beim Laden etwas schiefgeht.
    void ShowError(string message)
    {
        isLoading = false;
        SetText("<b>Raum " + roomNumber + "</b>\n\n<color=#B00000>" + message + "</color>");
    }


    // Setzt den Text und passt Größe und Position des Popups an.
    void SetText(string text)
    {
        popupText.text = text;

        // Zuerst die Breite setzen, damit Unity weiß, wo der Text umbricht ...
        popupRect.sizeDelta = new Vector2(popupWidth, popupRect.sizeDelta.y);

        // ... dann die Höhe = Höhe des Textes + Abstand oben und unten (10 + 10, siehe Awake)
        float textHeight = popupText.preferredHeight;
        popupRect.sizeDelta = new Vector2(popupWidth, textHeight + 20);

        PlaceNextToDoor();
    }

    // Setzt das Popup über die Tür. Ist oben kein Platz, kommt es unter die Tür.
    // Außerdem wird es so verschoben, dass es nie aus dem Bild ragt.
    void PlaceNextToDoor()
    {
        float gap = 10f;   // Abstand zwischen Tür und Popup

        RectTransform door = GetComponent<RectTransform>();
        Rect canvas = canvasRect.rect;
        float halfWidth = popupRect.rect.width / 2;
        float halfHeight = popupRect.rect.height / 2;
        float doorHalfHeight = door.rect.height / 2;

        // Position der Tür im Koordinatensystem des Canvas
        Vector2 doorPos = canvasRect.InverseTransformPoint(door.position);

        // Versuch 1: über der Tür
        float x = doorPos.x;
        float y = doorPos.y + doorHalfHeight + gap + halfHeight;

        // Ragt es oben hinaus? Dann unter die Tür setzen.
        if (y + halfHeight > canvas.yMax)
            y = doorPos.y - doorHalfHeight - gap - halfHeight;

        // Im Bild halten (Clamp = Wert auf einen Bereich begrenzen)
        x = Mathf.Clamp(x, canvas.xMin + halfWidth, canvas.xMax - halfWidth);
        y = Mathf.Clamp(y, canvas.yMin + halfHeight, canvas.yMax - halfHeight);

        popupRect.localPosition = new Vector3(x, y, 0);
    }
}
