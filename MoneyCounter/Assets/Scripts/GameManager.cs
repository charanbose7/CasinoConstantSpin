using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject[] reels; // Assign Reel 1, 2, 3, 4, 5 in the Inspector
    [SerializeField] private float scrollDuration = 2f; // Duration of the scroll
    [SerializeField] private int increment = 2; // Increment value
    [SerializeField] private GameObject spritePrefab; // Assign the sprite prefab in the Inspector
    [SerializeField] private RectTransform textRectTransform; // The RectTransform of the TextMeshProUGUI

    private int[] reelValues = new int[5]; // To keep track of each reel's current value
    private int targetNumber; // Max value
    private int minNumber; // Min value
    private Coroutine scrollCoroutine; // To keep track of the current scrolling coroutine
    private RectTransform[][] reelTransforms; // Cached references to child RectTransforms
    private TextMeshProUGUI[][] reelTexts; // Cached references to TextMeshProUGUI components

    [SerializeField] TextMeshProUGUI text;
    [SerializeField] GameObject numberHolder;

    private void Start()
    {
        CacheReelComponents();
        InitializeReels();
        targetNumber = GetCurrentNumber(); // Start with the current number as target
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetRandomTargetNumber();
            SetReelsToValue(minNumber);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }

            scrollCoroutine = StartCoroutine(ScrollReelsToTarget(targetNumber));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            PlaceSpritesOnLastThreeDigits();
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

    private int GetCurrentNumber()
    {
        int currentNumber = 0;
        foreach (int reelValue in reelValues)
        {
            currentNumber = currentNumber * 10 + reelValue;
        }
        return currentNumber;
    }

    private void SetReelsToValue(int value)
    {
        string valueStr = value.ToString().PadLeft(reels.Length, '0');
        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            reelValues[reelIndex] = int.Parse(valueStr[reelIndex].ToString());
            UpdateReel(reelIndex, reelValues[reelIndex]);
        }
    }

    private void SetRandomTargetNumber()
    {
        minNumber = Random.Range(0, (int)Mathf.Pow(10, reels.Length) - 1);
        targetNumber = minNumber + increment;

        Debug.Log("Min Number: " + minNumber);
        Debug.Log("Target Number: " + targetNumber);
    }

    private IEnumerator ScrollReelsToTarget(int target)
    {
        int currentNumber = GetCurrentNumber();
        while (currentNumber != target)
        {
            yield return StartCoroutine(ScrollReelsTogether(reels.Length - 1));
            currentNumber = GetCurrentNumber();
        }
    }

    private IEnumerator ScrollReelsTogether(int startReelIndex)
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

        float scrollDistance = reelTransforms[0][0].rect.height;

        // Create a sequence to combine all the tweens
        Sequence sequence = DOTween.Sequence();

        // Dictionary to track the new values for each reel
        Dictionary<int, int> newReelValues = new Dictionary<int, int>();

        // Animate each reel
        foreach (int reelIndex in reelsToScroll)
        {
            int reelValue = reelValues[reelIndex];
            int newValue = (reelValue + 1) % 10;
            newReelValues[reelIndex] = newValue;

            for (int childIndex = 0; childIndex < reelTransforms[reelIndex].Length; childIndex++)
            {
                RectTransform child = reelTransforms[reelIndex][childIndex];
                float startY = child.localPosition.y;
                float endY = startY + scrollDistance;

                // Create a tween to animate the child position
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

                // Add the tween to the sequence
                sequence.Join(tween);
            }
        }

        // Play the sequence and wait for it to complete
        sequence.Play();
        yield return sequence.WaitForCompletion();

        // Final update of reel values
        foreach (var reelIndex in newReelValues.Keys)
        {
            reelValues[reelIndex] = newReelValues[reelIndex];
            UpdateReel(reelIndex, reelValues[reelIndex]);
        }
    }

    private void PlaceSpritesOnLastThreeDigits()
    {
        // Get the number of children to determine how many sprites to place
        int childCount = numberHolder.transform.childCount;
        string textValue = text.text;

        List<int> numericCharIndices = new List<int>();

        // Find indices of all numeric characters in the text
        for (int i = 0; i < textValue.Length; i++)
        {
            if (char.IsDigit(textValue[i]))
            {
                numericCharIndices.Add(i);
            }
        }
        var y = GetCharacterPosition(text, 0, out Vector2 yPos);
        // Ensure there are enough digits
        if (numericCharIndices.Count < childCount)
        {
            Debug.LogWarning("Not enough numeric characters in text to match child count.");
            return;
        }

        // Process the last 'childCount' numeric characters
        List<Vector3> charPositions = new List<Vector3>();
        List<Vector2> charSizes = new List<Vector2>();

        for (int i = numericCharIndices.Count - childCount; i < numericCharIndices.Count; i++)
        {
            int charIndex = numericCharIndices[i];
            Vector3 charPosition = GetCharacterPosition(text, charIndex, out Vector2 charSize);
            charPositions.Add(charPosition);
            charSizes.Add(charSize);
        }

        // Make the last 'childCount' numeric characters invisible
        for (int i = numericCharIndices.Count - childCount; i < numericCharIndices.Count; i++)
        {
            int charIndex = numericCharIndices[i];
            TMP_CharacterInfo charInfo = text.textInfo.characterInfo[charIndex];
            int meshIndex = charInfo.materialReferenceIndex;
            text.textInfo.meshInfo[meshIndex].colors32[charInfo.vertexIndex + 0] = new Color32(0, 0, 0, 0);
            text.textInfo.meshInfo[meshIndex].colors32[charInfo.vertexIndex + 1] = new Color32(0, 0, 0, 0);
            text.textInfo.meshInfo[meshIndex].colors32[charInfo.vertexIndex + 2] = new Color32(0, 0, 0, 0);
            text.textInfo.meshInfo[meshIndex].colors32[charInfo.vertexIndex + 3] = new Color32(0, 0, 0, 0);
        }

        // Apply changes to the text mesh
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

        // Place sprites on top of the invisible text and adjust their properties
        for (int i = 0; i < charPositions.Count; i++)
        {
            // Get the current child of numberHolder
            GameObject child = numberHolder.transform.GetChild(i).gameObject;
            RectTransform childRect = child.GetComponent<RectTransform>();

            var height = (charSizes[i].y * childCount);
            // Set the child dimensions
            childRect.sizeDelta = new Vector2(charSizes[i].x, height);

            // Set the child position
            childRect.anchoredPosition = new Vector2(charPositions[i].x, (charSizes[i].y) / 10);//yPos.y - (charSizes[i].y)/2



            // Optional: Match the text size of the child object (if it has a TextMeshPro component)
            TextMeshProUGUI childText = child.GetComponentInChildren<TextMeshProUGUI>();
            if (childText != null)
            {
                childText.fontSize = text.fontSize; // Match the font size of the main text
            }
        }
    }

    private Vector3 GetCharacterPosition(TextMeshProUGUI tmp, int charIndex, out Vector2 charSize)
    {
        // Force the text to update its information
        tmp.ForceMeshUpdate();

        // Get the character info
        TMP_CharacterInfo charInfo = tmp.textInfo.characterInfo[charIndex];

        // Only proceed if the character is visible
        if (!charInfo.isVisible)
        {
            charSize = Vector2.zero;
            return Vector3.zero;
        }

        // Get the bottom left and top right corners of the character in local space
        Vector3 worldBottomLeft = charInfo.bottomLeft;
        Vector3 worldTopRight = charInfo.topRight;

        // Calculate the width and height of the character
        float width = worldTopRight.x - worldBottomLeft.x;
        float height = worldTopRight.y - worldBottomLeft.y;
        charSize = new Vector2(width, height);

        // Calculate the center of the character
        Vector3 charCenter = (worldBottomLeft + worldTopRight) / 2;

        // Convert the world space position to local space relative to the text RectTransform
        Vector3 localPosition = textRectTransform.InverseTransformPoint(tmp.transform.TransformPoint(charCenter));

        return new Vector3(localPosition.x, localPosition.y, 0f); // Returning a 2D anchored position
    }
}
