using TEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEntry : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod]
    private static void OnInit()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name == "Launcher")
        {
            SceneManager.LoadScene("main", LoadSceneMode.Single);
        }
    }

    void Awake()
    {
        ModuleSystem.GetModule<IUpdateDriver>();
        ModuleSystem.GetModule<IResourceModule>();
        ModuleSystem.GetModule<IDebuggerModule>();
        ModuleSystem.GetModule<IFsmModule>();
        Settings.ProcedureSetting.StartProcedure().Forget();
        DontDestroyOnLoad(this);
    }
}