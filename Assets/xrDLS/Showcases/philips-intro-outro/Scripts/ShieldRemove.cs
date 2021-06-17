using System.Collections;
using UnityEngine;

namespace Assets.Examples.Projects.Examples.Scripts
{
    public class ShieldRemove : MonoBehaviour
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
        private float totalDurationInFloatSeconds = 1.0f;
        private float currentTimeInDelta; // the amount of time that has elapsed so far

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


            Invoke(nameof(StartSunrise), 1f);
            Invoke(nameof(StartSundown), 6f);

            bottomPlaneMaterial.SetColor("_Color", new Color(0f, 0f, 0f));
        }

        private IEnumerator StartTune()
        {
            yield return new WaitForSeconds(1.0f);
            tune.Play();
            yield return new WaitForSeconds(1.0f + offset);
            ShowStar(0);
            yield return new WaitForSeconds(0.6f);
            ShowStar(1);
            yield return new WaitForSeconds(0.4f);
            ShowStar(2);
            yield return new WaitForSeconds(0.3f);
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
            totalDurationInFloatSeconds = 3f;
            executeSundown = true;
            GetComponent<Animator>().SetBool("EndFade", true); // start the fading out animation
            Invoke(nameof(ExitApplication), 2.5f);
        }

        private void ExitApplication()
        {
            Debug.Log("EXIT");
            Application.Quit();
        }

        private void Update()
        {
            currentTimeInDelta += Time.deltaTime;
            float percentageComplete;
            if (executeSunrise)
            {
                // This prevents the sunrise from exceeding 100%
                // totalDurationInFloatSeconds float in seconds how long it takes to complete the activity
                percentageComplete = Mathf.Clamp01(currentTimeInDelta / totalDurationInFloatSeconds);
                if (!augmentedReality)
                {
                    RenderSettings.skybox.SetFloat("_Exposure", percentageComplete);
                    RenderSettings.fogColor = new Color((44f / 255f) * percentageComplete, (175f / 255f) * percentageComplete, (227f / 255f) * percentageComplete);
                }
            }

            if (executeSundown)            
            {
                // This prevents the sunrise from exceeding 100%
                // totalDurationInFloatSeconds float in seconds how long it takes to complete the activity
                percentageComplete = Mathf.Clamp01(1f- currentTimeInDelta / totalDurationInFloatSeconds);
                if (!augmentedReality)
                {
                    RenderSettings.skybox.SetFloat("_Exposure", percentageComplete);
                    RenderSettings.fogColor = new Color((44f / 255f) * percentageComplete, (175f / 255f) * percentageComplete, (227f / 255f) * percentageComplete);
                }
                bottomPlaneMaterial.SetColor("_EmissionColor", new Color(0.0f * percentageComplete, 0.67f * percentageComplete, 0.905f * percentageComplete));
            }
        }

    }
}