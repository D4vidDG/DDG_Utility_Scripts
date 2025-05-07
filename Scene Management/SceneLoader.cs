using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    [SerializeField] float loadingFadeTime = 1.5f;
    Fader fader;
    bool alreadyStarted = false;

    public static SceneLoader instance;

    private void Awake()
    {
        fader = GetComponent<Fader>();
        DontDestroyOnLoad(this.gameObject);

        if (instance != null)
        {
            Destroy(instance, loadingFadeTime);
            instance = this;
        }
        else
        {
            instance = this;
        }
    }

    public void LoadScene(int index)
    {
        StartCoroutine(LoadSceneRoutine(index));
    }

    private IEnumerator LoadSceneRoutine(int index)
    {
        yield return fader.FadeOut(loadingFadeTime);
        yield return SceneManager.LoadSceneAsync(index);
        yield return fader.FadeIn(loadingFadeTime);

    }
}
