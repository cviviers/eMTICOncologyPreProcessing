using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Philips.Branding
{
    public class VRMenuItem : MonoBehaviour
    {
        public Action<VRMenuItem> OnClick;

        [System.Serializable]
        internal enum MenuDesign
        {
            Card,
            LeftBottom = 1,
            LeftUp = 2,
            RightBottom = 3,
            RightUp = 4,
        }

         [System.Serializable]
        public enum BackgroundGradient
        {

            BrightAqua = 0,
            BrightBlue = 1,
            BrightOrange = 2,
            BrightPurple = 3,
            DarkBlue = 4,
            DarkGreen = 5,
            DarkPink = 6
        }

        [Tooltip("Title of the menu item displayed in bold")]
        public string Title;
        [Tooltip("Description of the menu item displayed below the title")]
        public string Description;
        [Tooltip("Image for the menu item")]
        public Texture Image;
        [Tooltip("Color of the upper border")]
        public Color Border;
        [Tooltip("Background gradient")]
        public BackgroundGradient Background;
        [Tooltip("Playback speed of animations")]
        [Range(0.1f, 8.0f)]
        public float AnimationSpeed = 1f;

        [Tooltip("Height of the generated texture to use for the item")]
        public int Height = 1024;

        [Tooltip("Strength of blur when not selected during selection and in start- & end animations")]
        [Range(1f, 16f)]
        public float BlurStrength = 10f;
        [Tooltip("Delay in startup fade. <0 = automatic, 0 = immediate, >0 = start with given delay")]
        [Range(1f, 16f)]
        public float FadeDelay = 0f;

        [HideInInspector]
        public Material MenuMaterial;
        [HideInInspector]
        public Material ImageMaterial;
        [HideInInspector]
        public Material BorderMaterial;
        [HideInInspector]
        public Material BackgroundMaterial;
        [HideInInspector]
        public Texture2D[] Gradients;
        [HideInInspector]
        public float Curvation
        {
            set
            {
                curvation = value;
#if UNITY_EDITOR
                OnValidate();
#endif
            }
            get { return curvation; }
        }
        private float curvation;

        internal MenuDesign Design;
        internal bool ShowBorder;

        private Vector3 startPosition;
        private Material material;
        private float fadeIn = 0f, fadeDelay;

        private TMPro.TextMeshPro title, text;
        private Texture2D menuItemImage;

        private RenderTexture menuItemOutput;
        private Material blurMaterial;

        void Start()
        {
            fadeDelay = FadeDelay;
            startPosition = transform.localPosition;

            CreateMenuItemImage();
        }

        internal void CreateMenuItemImage(float blur = 16f)
        {
            if (MenuMaterial == null) return;

            var output = transform.Find("Output").gameObject;

            blurMaterial = new Material(Shader.Find("Philips/GaussianBlur"));

            material = Instantiate(MenuMaterial);
            material.SetFloat("_Curve", curvation);
            output.GetComponent<MeshRenderer>().sharedMaterial = material;

            var designName = "";
            switch(Design)
            {
                case MenuDesign.Card : designName = "CardWithBorder"; break;
                case MenuDesign.LeftBottom : designName = "LeftBottom"; break;
                case MenuDesign.LeftUp : designName = "LeftUp"; break;
                case MenuDesign.RightBottom : designName = "RightBottom"; break;
                case MenuDesign.RightUp : designName = "RightUp"; break;
            }

            var grid = transform.Find(designName).gameObject;
            grid.SetActive(true);

            try
            {
                var texts = grid.GetComponentsInChildren<TMPro.TextMeshPro>();

                foreach(var t in texts)
                {
                    if (t.gameObject.name == "Title") title = t;
                    if (t.gameObject.name == "Text") text = t;
                }

                if (!string.IsNullOrEmpty(Title)) title.text = Title;
                if (!string.IsNullOrEmpty(Description)) text.text = Description;

                if (Image != null)
                {
                    var image = grid.transform.Find("Image").gameObject.GetComponent<MeshRenderer>();
                    var m = Instantiate(ImageMaterial);
                    m.mainTexture = Image;
                    image.sharedMaterial = m;
                }

                var border = grid.transform.Find("UpperBorder").gameObject;
                var borderMeshRenderer = border.GetComponent<MeshRenderer>();
                if (ShowBorder)
                {
                    border.SetActive(true);
                    var borderMaterial = Instantiate(BorderMaterial);
                    borderMaterial.color = Border;
                    borderMeshRenderer.sharedMaterial = borderMaterial;
                }
                else
                {
                    border.SetActive(false);
                }

                var backgroundGradient = grid.transform.Find("Background").gameObject.GetComponent<MeshRenderer>();
                var backgroundMaterial = Instantiate(BackgroundMaterial);
//                var background = (int)(Design == MenuDesign.CardWithBorder ? BackgroundGradient.BrightBlue : Background);
                var background = (int)Background;

                backgroundMaterial.mainTexture = Gradients[background];
                backgroundGradient.sharedMaterial = backgroundMaterial;

                menuItemImage = RenderMenuItem();
                if (menuItemOutput == null) menuItemOutput = new RenderTexture(menuItemImage.width, menuItemImage.height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
                BlurImage(menuItemImage, menuItemOutput, blur);
                material.mainTexture = menuItemOutput;
            }
            catch {}

            grid.SetActive(false);
        }

        Texture2D RenderMenuItem()
        {
            var camera = transform.Find("ItemCamera").gameObject.GetComponent<Camera>();

            var scale = camera.gameObject.transform.localScale;
            var width = (int)(Height * camera.aspect);

            if (width > 4096) width = 4096; // catches bug in Unity on aspect ratio when switching away from Play mode

            var active = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, Height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
            RenderTexture.active = target;

            camera.targetTexture = target;
            camera.Render();

            var texture = new Texture2D(width, Height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, width, Height), 0, 0);
            texture.Apply();

            camera.targetTexture = null;
            RenderTexture.active = active;

            RenderTexture.ReleaseTemporary(target);

            return texture;
        }

        bool selected = false;
        bool animating = false;
        float animate = 0f;
        float blur = 0f;
        bool blurAnimating = false;
        bool blurActive = false;

        void Update()
        {
            if (blurAnimating && fadeIn >= 1f)
            {
                if (blurActive)
                {
                    blur += Time.deltaTime * AnimationSpeed;
                    if (blur > 1f)
                    {
                        blurAnimating = false;
                        blur = 1f;
                    }
                }
                else
                {
                    blur -= Time.deltaTime * AnimationSpeed;
                    if (blur < 0f)
                    {
                        blurAnimating = false;
                        blur = 0f;
                    }
                }
                BlurImage(menuItemImage, menuItemOutput, BlurStrength * blur);
            }

            if (fadeIn < 1f)
            {
                if (fadeDelay > 0f) fadeDelay -= Time.deltaTime * 7f; // delay between menu items fade in
                    else fadeIn += Time.deltaTime * 2f; // fade in speed
                if (fadeIn > 1f) fadeIn = 1f;
                var color = material.color;
                color.a = fadeIn;
                material.color = color;
                BlurImage(menuItemImage, menuItemOutput, BlurStrength * (1f - fadeIn));
            } else if (!animating) return;

            if (selected)
            {
                animate += Time.deltaTime * AnimationSpeed;
                if (animate > 1f)
                {
                    animate = 1f;
                    animating = false;
                }
            } else
            {
                animate -= Time.deltaTime * AnimationSpeed;
                if (animate < 0f)
                {
                    animate = 0f;
                    animating = false;
                }
            }
            transform.localPosition = startPosition - transform.forward *
                (EaseInOutCubic(0f, 1f, animate) - 1f + EaseInOutCubic(0f, 1f, fadeIn));
        }

        private static float EaseInOutCubic(float start, float end, float value)
        {
            value /= .5f;
            end -= start;
            if (value < 1) return end * 0.5f * value * value * value + start;
            value -= 2;
            return end * 0.5f * (value * value * value + 2) + start;
        }

        private void BlurImage(Texture2D src, RenderTexture dst, float blur)
        {
            blurMaterial.SetFloat("_Spread", blur + 1f);
            var tmp = RenderTexture.GetTemporary(src.width, src.height, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);

            Graphics.Blit(src, tmp, blurMaterial, 0);
            Graphics.Blit(tmp, dst, blurMaterial, 1);

            RenderTexture.ReleaseTemporary(tmp);
        }

        internal void Click()
        {
            Debug.Log("Click on " + name);
            if (OnClick != null)
                OnClick(this);
        }

        internal void Select()
        {
            selected = true;
            animating = true;
        }

        internal void DeSelect()
        {
            selected = false;
            animating = true;
        }

        internal void Blur(bool active)
        {
            blurActive = active;
            blurAnimating = true;
        }

#if UNITY_EDITOR
        internal bool dirty = true;
        private void OnValidate()
        {
            if (!MenuUpdater.menuItemsActive.Contains(this))
            {
                MenuUpdater.menuItemsActive.Add(this);
            }
            dirty = true;
        }

        VRMenuItem()
        {
            MenuUpdater.menuItemsActive.Add(this);
        }

        ~VRMenuItem()
        {
            MenuUpdater.menuItemsActive.Remove(this);
        }

        private void OnDestroy()
        {
            MenuUpdater.menuItemsActive.Remove(this);
        }
#endif
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    internal class MenuUpdater
    {
        // Unity forces the singleton for Editor-time responding to OnLoad...
        static internal List<VRMenuItem> menuItemsActive = new List<VRMenuItem>();

        static MenuUpdater()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            if (EditorApplication.isPlaying) return;

            var removeBecauseOfError = new List<VRMenuItem>(); // little hack to remove faulty items...
            foreach(var menuItem in menuItemsActive)
            {
                if (menuItem.dirty)
                {
                    try
                    {
                        menuItem.dirty = false;
                        menuItem.CreateMenuItemImage(0f);
                    }
                    catch { removeBecauseOfError.Add(menuItem); }
                }
            }

            foreach(var removeItem in removeBecauseOfError)
                menuItemsActive.Remove(removeItem);
        }
    }
#endif
}
