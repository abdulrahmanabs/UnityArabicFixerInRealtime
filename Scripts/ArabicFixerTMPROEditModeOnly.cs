using TMPro;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[ExecuteInEditMode]
#endif

public class ArabicFixerTMPROEditModeOnly : MonoBehaviour
{
    public string fixedText;
    public bool ShowTashkeel;
    public bool UseHinduNumbers;
    public bool isFixed = false;
    TextMeshProUGUI tmpTextComponent;

    private string OldText; // For Refresh on TextChange
    private int OldFontSize; // For Refresh on Font Size Change
    private RectTransform rectTransform;  // For Refresh on resize
    private Vector2 OldDeltaSize; // For Refresh on resize
    private bool OldEnabled = false; // For Refresh on enabled change // when text ui is not active then arabic text will not trigered when the control get active
    private List<RectTransform> OldRectTransformParents = new List<RectTransform>(); // For Refresh on parent resizing
    private Vector2 OldScreenRect = new Vector2(Screen.width, Screen.height); // For Refresh on screen resizing
    bool hasExecuteInEditMode = false;

    private string prevText = "";
    bool isInitilized;
    void Awake()
    {
        // Get the attributes of the ScriptAttributeChecker class using reflection
        object[] attributes = GetType().GetCustomAttributes(typeof(ExecuteInEditMode), true);

        // Check if the ExecuteInEditMode attribute is present
        foreach (object attribute in attributes)
        {
            if (attribute is ExecuteInEditMode)
            {
                hasExecuteInEditMode = true;
                break;
            }
        }

        GetRectTransformParents(OldRectTransformParents);
        isInitilized = false;
        tmpTextComponent = GetComponent<TextMeshProUGUI>();
        prevText = tmpTextComponent.text;
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        fixedText = tmpTextComponent.text;
        isInitilized = true;
    }

    private void GetRectTransformParents(List<RectTransform> rectTransforms)
    {
        rectTransforms.Clear();
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
        {
            GameObject goP = parent.gameObject;
            RectTransform rect = goP.GetComponent<RectTransform>();
            if (rect) rectTransforms.Add(rect);
        }
    }

    private bool CheckRectTransformParentsIfChanged()
    {
        bool hasChanged = false;
        for (int i = 0; i < OldRectTransformParents.Count; i++)
        {
            hasChanged |= OldRectTransformParents[i].hasChanged;
            OldRectTransformParents[i].hasChanged = false;
        }
        return hasChanged;
    }

#if UNITY_EDITOR
    // This method is called whenever the text of the TMP component changes in the editor
    void OnValidate()
    {
        if (hasExecuteInEditMode)
        {
            // The text has changed, so fix it for UI
            FixTextForUI();
        }
    }
#endif

    void Update()
    {
        if (isFixed) return;
        #if UNITY_EDITOR
        if (EditorApplication.isPlaying) return;
        #endif
        if (!isInitilized)
            return;

        // if No Need to Refresh
        if (OldText == fixedText &&
            OldFontSize == tmpTextComponent.fontSize &&
            OldDeltaSize == rectTransform.sizeDelta &&
            OldEnabled == tmpTextComponent.enabled &&
            (OldScreenRect.x == Screen.width && OldScreenRect.y == Screen.height &&
            !CheckRectTransformParentsIfChanged()))
            return;

        FixTextForUI();
        OldText = fixedText;
        OldFontSize = (int)tmpTextComponent.fontSize;
        OldDeltaSize = rectTransform.sizeDelta;
        OldEnabled = tmpTextComponent.enabled;
        OldScreenRect.x = Screen.width;
        OldScreenRect.y = Screen.height;
    }

    public void FixTextForUI()
    {
        if (!string.IsNullOrEmpty(fixedText))
        {
            isFixed = true;
            string rtlText = ArabicSupport.Fix(fixedText, ShowTashkeel, UseHinduNumbers);
            rtlText = rtlText.Replace("\r", ""); // the Arabix fixer Return \r\n for everyy \n .. need to be removed

            string finalText = "";
            string[] rtlParagraph = rtlText.Split('\n');

            tmpTextComponent.text = "";
            for (int lineIndex = 0; lineIndex < rtlParagraph.Length; lineIndex++)
            {
                string[] words = rtlParagraph[lineIndex].Split(' ');
                System.Array.Reverse(words);
                tmpTextComponent.text = string.Join(" ", words);
                Canvas.ForceUpdateCanvases();
                for (int i = 0; i < tmpTextComponent.textInfo.lineCount; i++)
                {
                    int startIndex = tmpTextComponent.textInfo.lineInfo[i].firstCharacterIndex;
                    int endIndex = (i == tmpTextComponent.textInfo.lineCount - 1) ? tmpTextComponent.text.Length
                        : tmpTextComponent.textInfo.lineInfo[i + 1].firstCharacterIndex;
                    int length = endIndex - startIndex;
                    string[] lineWords = tmpTextComponent.text.Substring(startIndex, length).Split(' ');
                    System.Array.Reverse(lineWords);
                    finalText = finalText + string.Join(" ", lineWords).Trim() + "\n";
                }
            }
            tmpTextComponent.text = finalText.TrimEnd('\n');
        }
        prevText = tmpTextComponent.text;
    }
}
