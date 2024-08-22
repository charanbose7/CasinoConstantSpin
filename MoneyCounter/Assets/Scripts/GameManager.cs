using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject[] reels; // Assign Reel 1, 2, 3, 4, 5 in the Inspector
    [SerializeField] private float scrollDuration = 2f; // Duration of the scroll
    [SerializeField] private int increment = 2; // Increment value

    private int[] reelValues = new int[5]; // To keep track of each reel's current value
    private int targetNumber; // Max value
    private int minNumber; // Min value
    private Coroutine scrollCoroutine; // To keep track of the current scrolling coroutine

    private RectTransform[][] reelTransforms; // Cached references to child RectTransforms
    private TextMeshProUGUI[][] reelTexts; // Cached references to TextMeshProUGUI components

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
}
