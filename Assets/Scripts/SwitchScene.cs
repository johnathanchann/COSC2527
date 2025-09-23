using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    private AsyncOperation _preloadOp;

    void Start()
    {
        _preloadOp = SceneManager.LoadSceneAsync("FrogGame2", LoadSceneMode.Additive);
        _preloadOp.allowSceneActivation = false;
    }

    public void ActivateFrog2()
    {
        Time.timeScale = 0f;

        _preloadOp.allowSceneActivation = true;

        Scene newScene = SceneManager.GetSceneByName("FrogGame2");
        SceneManager.SetActiveScene(newScene);

        Time.timeScale = 1f;
    }
}
