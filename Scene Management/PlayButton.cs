using UnityEngine;

public class PlayButton : MonoBehaviour
{
    [SerializeField] int gameSceneIndex;
    public void Play()
    {
        SceneLoader.instance.LoadScene(gameSceneIndex);
        AkSoundEngine.PostEvent("gameStart", this.gameObject);
    }
}
