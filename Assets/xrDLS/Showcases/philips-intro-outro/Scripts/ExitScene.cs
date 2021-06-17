using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SceneSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitScene : MonoBehaviour
{
    public void DoExitClick()
    {
        Invoke(nameof(DoExit), 1f);
    }
    private async void DoExit()
    {
        if (Application.CanStreamedLevelBeLoaded("Philips_Outro"))
        {
            Debug.Log("Loading Philips_Outro");
            await MixedRealityToolkit.Instance.GetService<IMixedRealitySceneSystem>().LoadContent("Philips_Outro", LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("Could not load scene! Philips Outro");
            Debug.Log("Could not load scene! Philips Outro");
        }
    }
}
