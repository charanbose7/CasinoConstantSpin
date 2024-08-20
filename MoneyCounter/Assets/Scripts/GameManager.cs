using System.Collections;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject[] reels; // Assign Reel 1, 2, 3, 4, 5 in the Inspector
    public float scrollDuration = 2f; // Duration of the scroll
    private int[] reelValues = new int[5]; // To keep track of each reel's current value

    void Start()
    {
        InitializeReels();
        StartCoroutine(ScrollReelsCoroutine());
    }

    void InitializeReels()
    {
        for (int i = 0; i < reels.Length; i++)
        {
            // Assign random values to each child of the reel
            reelValues[i] = Random.Range(0, 10);
            UpdateReel(reels[i], reelValues[i]);
        }
    }

    void UpdateReel(GameObject reel, int startValue)
    {
        for (int i = 0; i < reel.transform.childCount; i++)
        {
            TextMeshProUGUI text = reel.transform.GetChild(i).GetComponent<TextMeshProUGUI>();
            text.text = ((startValue + i) % 10).ToString();
        }
    }

    IEnumerator ScrollReelsCoroutine()
    {
        while (true)
        {
            // Scroll the rightmost reel
            yield return StartCoroutine(ScrollReel(reels.Length - 1));
        }
    }

    IEnumerator ScrollReel(int reelIndex)
    {
        Transform reelTransform = reels[reelIndex].transform;
        float elapsedTime = 0f;
        float scrollDistance = reelTransform.GetChild(0).GetComponent<RectTransform>().rect.height;

        bool shouldScrollNextReel = (reelValues[reelIndex] == 9);

        while (elapsedTime < scrollDuration)
        {
            for (int i = 0; i < reelTransform.childCount; i++)
            {
                Transform child = reelTransform.GetChild(i);
                child.localPosition += Vector3.up * (scrollDistance / scrollDuration) * Time.deltaTime;

                if (child.localPosition.y >= scrollDistance)
                {
                    // Move the child to the bottom of the hierarchy
                    child.SetAsLastSibling();
                    child.localPosition -= Vector3.up * scrollDistance;

                    // Update the text value
                    int newValue = (reelValues[reelIndex] + 1) % 10;
                    reelValues[reelIndex] = newValue;
                    child.GetComponent<TextMeshProUGUI>().text = newValue.ToString();
                }
            }

            elapsedTime += Time.deltaTime;

            if (shouldScrollNextReel && reelIndex > 0)
            {
                // Scroll the next reel to the left simultaneously, but only once during the roll-over
                Transform nextReelTransform = reels[reelIndex - 1].transform;
                for (int i = 0; i < nextReelTransform.childCount; i++)
                {
                    Transform child = nextReelTransform.GetChild(i);
                    child.localPosition += Vector3.up * (scrollDistance / scrollDuration) * Time.deltaTime;

                    if (child.localPosition.y >= scrollDistance)
                    {
                        // Move the child to the bottom of the hierarchy
                        child.SetAsLastSibling();
                        child.localPosition -= Vector3.up * scrollDistance;

                        // Update the text value
                        int newValue = (reelValues[reelIndex - 1] + 1) % 10;
                        reelValues[reelIndex - 1] = newValue;
                        child.GetComponent<TextMeshProUGUI>().text = newValue.ToString();
                    }
                }
            }

            yield return null;
        }

        // After scrolling, update the reel values
        UpdateReel(reels[reelIndex], reelValues[reelIndex]);

        // If the current reel was at 9 and should have triggered the next reel
        if (shouldScrollNextReel && reelIndex > 0)
        {
            // Roll-over to the next reel
            if (reelValues[reelIndex - 1] == 0)
            {
                // Continue with the rightmost reel
                yield return ScrollReel(reelIndex);
            }
        }
    }
}