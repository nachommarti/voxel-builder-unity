using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelBuilder
{
    /// <summary>
    /// Panel mínimo visible mientras el cursor está libre (Escape, ver
    /// CursorLockController): ajustar el radio de carga del streaming en
    /// caliente y salir del juego. Generado por código, sin ningún asset de UI.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private const int MinRadius = 1;
        private const int MaxRadius = 8;

        private GameBootstrap gameBootstrap;
        private GameObject panel;
        private Text radiusLabel;
        private int radius;

        public void Initialize(GameBootstrap gameBootstrap, int initialRadius)
        {
            this.gameBootstrap = gameBootstrap;
            radius = initialRadius;
            BuildUI();
            UpdateRadiusLabel();
        }

        private void Update()
        {
            panel.SetActive(!CursorLockController.IsLocked);
        }

        private void ChangeRadius(int delta)
        {
            radius = Mathf.Clamp(radius + delta, MinRadius, MaxRadius);
            UpdateRadiusLabel();
            gameBootstrap.SetLoadRadius(radius);
        }

        private void UpdateRadiusLabel()
        {
            radiusLabel.text = $"Radio de carga: {radius}\n(descarga: {radius + 1})";
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildUI()
        {
            var canvasObject = new GameObject("PauseMenu Canvas");
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(canvasObject.transform, worldPositionStays: false);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420f, 280f);
            panelRect.anchoredPosition = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;

            CreateLabel(panel.transform, "Pausa", 28);

            var radiusRow = new GameObject("RadiusRow", typeof(HorizontalLayoutGroup));
            radiusRow.transform.SetParent(panel.transform, worldPositionStays: false);
            var rowLayout = radiusRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;

            CreateButton(radiusRow.transform, "-", 50f, () => ChangeRadius(-1));
            radiusLabel = CreateLabel(radiusRow.transform, string.Empty, 20);
            radiusLabel.rectTransform.sizeDelta = new Vector2(220f, 60f);
            CreateButton(radiusRow.transform, "+", 50f, () => ChangeRadius(1));

            CreateButton(panel.transform, "Salir", 200f, Quit);
        }

        private Text CreateLabel(Transform parent, string initialText, int fontSize)
        {
            var textObject = new GameObject("Label", typeof(Text));
            textObject.transform.SetParent(parent, worldPositionStays: false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = initialText;

            text.rectTransform.sizeDelta = new Vector2(360f, fontSize + 16f);
            return text;
        }

        private void CreateButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject($"Button {label}", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, worldPositionStays: false);
            buttonObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            buttonObject.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 44f);
            buttonObject.GetComponent<Button>().onClick.AddListener(onClick);

            Text text = CreateLabel(buttonObject.transform, label, 20);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
    }
}
