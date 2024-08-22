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

    private void Start()
    {
        InitializeReels();
        targetNumber = GetCurrentNumber(); // Start with the current number as target
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Generate a random min value and calculate the target max value
            minNumber = Random.Range(0, (int)Mathf.Pow(10, reels.Length) - 1);
            targetNumber = minNumber + increment;

            Debug.Log("Min Number: " + minNumber);
            Debug.Log("Target Number: " + targetNumber);

            // Set the reels to the min value first
            SetReelsToValue(minNumber);

            // If a scroll is already in progress, stop it and start a new one
            /*if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
            }*/

            // Start scrolling to the new target value
        }

        if (Input.GetKeyDown(KeyCode.A))
        {

            scrollCoroutine = StartCoroutine(ScrollReelsToTarget(targetNumber));
        }
    }

    private void InitializeReels()
    {
        for (int reelIndex = 0; reelIndex < reels.Length; reelIndex++)
        {
            reelValues[reelIndex] = Random.Range(0, 10);
            UpdateReel(reels[reelIndex], reelValues[reelIndex]);
        }
    }

    private void UpdateReel(GameObject reel, int startValue)
    {
        for (int childIndex = 0; childIndex < reel.transform.childCount; childIndex++)
        {
            TextMeshProUGUI text = reel.transform.GetChild(childIndex).GetComponent<TextMeshProUGUI>();
            text.text = ((startValue + childIndex) % 10).ToString();
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
            UpdateReel(reels[reelIndex], reelValues[reelIndex]);
        }
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

        float scrollDistance = reels[0].transform.GetChild(0).GetComponent<RectTransform>().rect.height;

        // Create a sequence to combine all the tweens
        Sequence sequence = DOTween.Sequence();

        // Dictionary to track the new values for each reel
        Dictionary<int, int> newReelValues = new Dictionary<int, int>();

        // Animate each reel
        foreach (int reelIndex in reelsToScroll)
        {
            Transform reelTransform = reels[reelIndex].transform;

            // Track the new value for this reel
            int reelValue = reelValues[reelIndex];
            int newValue = (reelValue + 1) % 10;
            newReelValues[reelIndex] = newValue;

            for (int childIndex = 0; childIndex < reelTransform.childCount; childIndex++)
            {
                Transform child = reelTransform.GetChild(childIndex);
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
            UpdateReel(reels[reelIndex], reelValues[reelIndex]);
        }
    }
}
