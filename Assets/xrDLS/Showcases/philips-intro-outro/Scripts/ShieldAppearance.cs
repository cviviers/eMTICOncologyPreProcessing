using System;
using System.Collections;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SceneSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Examples.Projects.Examples.Scripts
{
    public class ShieldAppearance : MonoBehaviour
    {
        public GameObject[] stars = new GameObject[4];

        private AudioSource tune;
        public AudioClip clip;

        public GameObject bottomPlane;
        private Material bottomPlaneMaterial;

        private bool executeSunrise;
        private bool executeSundown;
        public float offset = 0.2f;
        public bool augmentedReality;
        public string nextSceneName;
        private float currentTimeInDelta = 0; // the amount of time that has elapsed so far

        // Start is called before the first frame update
        private void Start()
        {
            if (!augmentedReality)
            {
                Material skyClone = new Material(RenderSettings.skybox);
                RenderSettings.skybox = skyClone;
                RenderSettings.skybox.SetFloat("_Exposure", 0f);
                RenderSettings.fogDensity = 0.09f;
                RenderSettings.fogColor = new Color(0f, 0f, 0f);
                bottomPlane.SetActive(true);
            }
            else
            {
                RenderSettings.skybox = null;
                bottomPlane.SetActive(false);
            }

            executeSunrise = false;

            bottomPlaneMaterial = bottomPlane.GetComponent<MeshRenderer>().material;
            bottomPlaneMaterial.SetColor("_EmissionColor", new Color(0f, 0f, 0f));

            tune = GetComponent<AudioSource>();
            tune.clip = clip;

            StartCoroutine(StartTune());

            Invoke(nameof(StartSunrise), 1.0f);
            Invoke(nameof(StartSundown), 10.0f);

            bottomPlaneMaterial.SetColor("_Color", new Color(0f, 0f, 0f));
        }

        private IEnumerator StartTune()
        {
            tune.Play();
            yield return new WaitForSeconds(4.2f + offset);
            ShowStar(0);
            yield return new WaitForSeconds(0.4f);
            ShowStar(1);
            yield return new WaitForSeconds(0.4f);
            ShowStar(2);
            yield return new WaitForSeconds(0.3f);
            GetComponent<Animator>().SetBool("StartFade", true);
            ShowStar(3);
        }

        private void ShowStar(int starId)
        {
            stars[starId].SetActive(true);
        }

        private void StartSunrise()
        {
            executeSunrise = true;
        }

        private void StartSundown()
        {
            currentTimeInDelta = 0f;
            executeSunrise = false;
            executeSundown = true;
            GetComponent<Animator>().SetBool("EndFade", true); // start the fading out animation
            Invoke(nameof(OpenScene), 1.5f);
        }

        private async void OpenScene()
        {
            if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.Log("Loading " + nextSceneName);
                await MixedRealityToolkit.Instance.GetService<IMixedRealitySceneSystem>().LoadContent(nextSceneName, LoadSceneMode.Single);
            }
            else if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Could not load scene!" + nextSceneName);
            }
        }

        private void Update()
        {
            currentTimeInDelta += Time.deltaTime; // add time each frame
            float percentageComplete;
            if (executeSunrise)
            {
                // This prevents the sunrise from exceeding 100%
                // 5.0f float in seconds how long it takes to complete the activity
                percentageComplete = Mathf.Clamp01(currentTimeInDelta / 5.0f);
                if (!augmentedReality)
                {
                    RenderSettings.skybox.SetFloat("_Exposure", percentageComplete);
                    RenderSettings.fogColor = new Color((44f / 255f) * percentageComplete,
                        (175 / 255f) * percentageComplete, (227f / 255f) * percentageComplete);
                }

                bottomPlaneMaterial.SetColor("_EmissionColor",
                    new Color(0.0f * percentageComplete, 0.67f * percentageComplete, 0.905f * percentageComplete));
            }

            if (executeSundown)
            {
                // This prevents the sundown from exceeding 100%
                // 1.0f float in seconds how long it takes to complete the activity
                percentageComplete = Mathf.Clamp01(currentTimeInDelta / 1.0f);
                if (!augmentedReality)
                {
                    RenderSettings.skybox.SetFloat("_Exposure", 1f - percentageComplete);
                }
            }
        }
    }
}