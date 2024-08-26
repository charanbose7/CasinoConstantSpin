using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Unity.VisualScripting.Metadata;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject[] reels; // Assign Reel 1, 2, 3, 4, 5 in the Inspector
    [SerializeField] private float scrollDuration = 2f; // Duration of the scroll
    [SerializeField] private float increment = 2.00f; // Increment value as float
    [SerializeField] private GameObject spritePrefab; // Assign the sprite prefab in the Inspector
    [SerializeField] private RectTransform textRectTransform; // The RectTransform of the TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject numberHolder;

    private int[] reelValues = new int[5]; // To keep track of each reel's current value
    private float targetNumber; // Max value
    private float minNumber; // Min value
    private Coroutine scrollCoroutine; // To keep track of the current scrolling coroutine
    private RectTransform[][] reelTransforms; // Cached references to child RectTransforms
    private TextMeshProUGUI[][] reelTexts; // Cached references to TextMeshProUGUI components

    private void Start()
    {
        ValidateIncrement();
        CacheReelComponents();
        InitializeReels();
        targetNumber = GetCurrentNumber(); // Start with the current number as target
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetRandomTargetNumber();
            SetReelsToValue(minNumber);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            StartScrollingReels();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            PlaceSpritesOnLastThreeDigits();
        }
    }

    private void ValidateIncrement()
    {
        float roundedIncrement = Mathf.Round(increment * 100f) / 100f;
        if (Mathf.Abs(increment - roundedIncrement) > Mathf.Epsilon)
        {
            Debug.LogError("Increment value must have only two decimal places!");
        }
    }

    private void CacheReelComponents()
    {
        reelTransforms = new RectTransform[reels.Length][];
        reelTexts = new TextMeshProUGUI[reels.Length][];

        for (int i = 0; i < reels.Length; i++)
        {
            int childCount = reels[i].transform.childCount;
            reelTransforms[i] = new RectTransform[childCount];
            reelTexts[i] = new TextMeshProUGUI[childCount];

            for (int j = 0; j < childCount; j++)
            {
                reelTransforms[i][j] = reels[i].transform.GetChild(j).GetComponent<RectTransform>();
                reelTexts[i][j] = reelTransforms[i][j].GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void InitializeReels()
    {
        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            reelValues[reelIndex] = Random.Range(0, 10);
            UpdateReel(reelIndex, reelValues[reelIndex]);
        }
    }

    private void UpdateReel(int reelIndex, int startValue)
    {
        for (int childIndex = 0; childIndex < reelTransforms[reelIndex].Length; childIndex++)
        {
            reelTexts[reelIndex][childIndex].text = ((startValue + childIndex) % 10).ToString();
        }
    }

    private float GetCurrentNumber()
    {
        string currentNumberStr = "";
        for (int i = 0; i < reelValues.Length; i++)
        {
            currentNumberStr += reelValues[i].ToString();
            if (i == reelValues.Length - 2) // Place the decimal point before the last two digits
            {
                currentNumberStr += ".";
            }
        }
        return float.Parse(currentNumberStr);
    }

    private void SetReelsToValue(float value)
    {
        string valueStr = value.ToString("F2").Replace(".", "").PadLeft(reels.Length, '0');

        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            reelValues[reelIndex] = int.Parse(valueStr[reelIndex].ToString());
            UpdateReel(reelIndex, reelValues[reelIndex]);
        }

        Debug.Log($"Final Reel Values: {string.Join(", ", reelValues)}");
    }

    private void SetRandomTargetNumber()
    {
        minNumber = Random.Range(0f, Mathf.Pow(10, reels.Length - 2) - 1f);
        minNumber = Mathf.Round(minNumber * 100f) / 100f;
        targetNumber = minNumber + increment;

        Debug.Log($"Min Number: {minNumber}");
        Debug.Log($"Target Number: {targetNumber}");
    }

    private void StartScrollingReels()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }
        scrollCoroutine = StartCoroutine(ScrollReelsToTarget(targetNumber));
    }

    private IEnumerator ScrollReelsToTarget(float target)
    {
        float currentNumber = GetCurrentNumber();
        while (Mathf.Abs(currentNumber - target) > Mathf.Epsilon)
        {
            yield return StartCoroutine(ScrollReelsTogether(reels.Length - 1));
            currentNumber = GetCurrentNumber();
        }
    }

    private IEnumerator ScrollReelsTogether(int startReelIndex)
    {
        List<int> reelsToScroll = GetReelsToScroll(startReelIndex);
        float scrollDistance = reelTransforms[0][0].rect.height;
        Sequence sequence = DOTween.Sequence();
        Dictionary<int, int> newReelValues = new Dictionary<int, int>();

        foreach (int reelIndex in reelsToScroll)
        {
            int newValue = (reelValues[reelIndex] + 1) % 10;
            newReelValues[reelIndex] = newValue;

            Tween tween = ScrollReel(reelIndex, scrollDistance);
            sequence.Join(tween);
        }

        sequence.Play();
        yield return sequence.WaitForCompletion();

        UpdateReelValues(newReelValues);
    }

    private List<int> GetReelsToScroll(int startReelIndex)
    {
        List<int> reelsToScroll = new List<int>();

        for (int reelIndex = startReelIndex; reelIndex >= 0; reelIndex--)
        {
            reelsToScroll.Add(reelIndex);
            if (reelValues[reelIndex] != 9)
            {
                break;
            }
        }

        return reelsToScroll;
    }

    private Tween ScrollReel(int reelIndex, float scrollDistance)
    {
        Sequence sequence = DOTween.Sequence();

        foreach (RectTransform child in reelTransforms[reelIndex])
        {
            float startY = child.localPosition.y;
            float endY = startY + scrollDistance;

            Tween tween = child.DOLocalMoveY(endY, scrollDuration)
                .SetEase(Ease.Linear)
                .OnUpdate(() =>
                {
                    if (child.localPosition.y >= endY)
                    {
                        child.SetAsLastSibling();
                        child.localPosition -= Vector3.up * scrollDistance;
                    }
                });

            sequence.Join(tween);
        }

        return sequence;
    }

    private void UpdateReelValues(Dictionary<int, int> newReelValues)
    {
        foreach (var reelIndex in newReelValues.Keys)
        {
            reelValues[reelIndex] = newReelValues[reelIndex];
            UpdateReel(reelIndex, reelValues[reelIndex]);
        }
    }

    private void PlaceSpritesOnLastThreeDigits()
    {
        int childCount = numberHolder.transform.childCount;
        string textValue = text.text;

        List<int> numericCharIndices = GetNumericCharIndices(textValue);

        if (numericCharIndices.Count < childCount)
        {
            Debug.LogWarning("Not enough numeric characters in text to match child count.");
            return;
        }

        List<Vector3> charPositions = new List<Vector3>();
        List<Vector2> charSizes = new List<Vector2>();

        for (int i = numericCharIndices.Count - childCount; i < numericCharIndices.Count; i++)
        {
            int charIndex = numericCharIndices[i];
            Vector3 charPosition = GetCharacterPosition(text, charIndex, out Vector2 charSize);
            charPositions.Add(charPosition);
            charSizes.Add(charSize);
        }

        MakeCharactersInvisible(numericCharIndices, childCount);

        PlaceReelsOnCharacters(charPositions, charSizes);
    }

    private List<int> GetNumericCharIndices(string textValue)
    {
        List<int> numericCharIndices = new List<int>();

        for (int i = 0; i < textValue.Length; i++)
        {
            if (char.IsDigit(textValue[i]))
            {
                numericCharIndices.Add(i);
            }
        }

        return numericCharIndices;
    }

    private void MakeCharactersInvisible(List<int> numericCharIndices, int childCount)
    {
        text.ForceMeshUpdate(); // Ensure the text mesh is up-to-date

        // Iterate over the indices of the characters to make invisible
        for (int i = numericCharIndices.Count - childCount; i < numericCharIndices.Count; i++)
        {
            int charIndex = numericCharIndices[i];
            TMP_CharacterInfo charInfo = text.textInfo.characterInfo[charIndex];
            int meshIndex = charInfo.materialReferenceIndex;

            // Make sure the character's color is set to fully transparent
            Color32[] vertexColors = text.textInfo.meshInfo[meshIndex].colors32;
            int vertexIndex = charInfo.vertexIndex;

            vertexColors[vertexIndex + 0] = new Color32(0, 0, 0, 0); // Bottom-left vertex
            vertexColors[vertexIndex + 1] = new Color32(0, 0, 0, 0); // Bottom-right vertex
            vertexColors[vertexIndex + 2] = new Color32(0, 0, 0, 0); // Top-right vertex
            vertexColors[vertexIndex + 3] = new Color32(0, 0, 0, 0); // Top-left vertex
        }

        // Apply changes to the text mesh
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }

    private void PlaceReelsOnCharacters(List<Vector3> charPositions, List<Vector2> charSizes)
    {
        for (int i = 0; i < numberHolder.transform.childCount; i++)
        {
            RectTransform reelRect = numberHolder.transform.GetChild(i).GetComponent<RectTransform>();

            Vector3 position = charPositions[i];
            Vector2 size = charSizes[i];
            reelRect.position = position;
            reelRect.sizeDelta = size;

            /*var height = (charSizes[i].y * numberHolder.transform.childCount);
            // Set the child dimensions
            reelRect.sizeDelta = new Vector2(charSizes[i].x, height);

            // Set the child position
            reelRect.anchoredPosition = new Vector2(charPositions[i].x, (charSizes[i].y) / 10);//yPos.y - (charSizes[i].y)/2
            */
        }
    }

    private Vector3 GetCharacterPosition(TextMeshProUGUI textComponent, int charIndex, out Vector2 charSize)
    {
        TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[charIndex];
        charSize = charInfo.ascender - charInfo.descender > 0 ? new Vector2(charInfo.xAdvance, charInfo.ascender - charInfo.descender) : Vector2.zero;
        return textComponent.transform.TransformPoint((charInfo.bottomLeft + charInfo.topRight) / 2);
    }
}
