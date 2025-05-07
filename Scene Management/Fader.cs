using System.Collections;
using UnityEngine;

public class Fader : MonoBehaviour
{
    CanvasGroup canvasGroup;
    Coroutine currentActiveRoutine = null;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void FadeOutInmediate()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;
    }

    public Coroutine FadeIn(float time)
    {
        canvasGroup.blocksRaycasts = false;
        return Fade(0, time);
    }

    public Coroutine FadeOut(float time)
    {
        canvasGroup.blocksRaycasts = true;
        return Fade(1, time);
    }

    public Coroutine Fade(float targetValue, float time)
    {
        if (currentActiveRoutine != null) StopCoroutine(currentActiveRoutine);
        currentActiveRoutine = StartCoroutine(FadeRoutine(targetValue, time));
        return currentActiveRoutine;
    }

    private IEnumerator FadeRoutine(float targetValue, float time)
    {
        while (!Mathf.Approximately(canvasGroup.alpha, targetValue))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetValue, Time.deltaTime / time);
            yield return null;
        }
    }

}
