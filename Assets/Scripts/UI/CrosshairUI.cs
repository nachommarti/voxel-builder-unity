using UnityEngine;
using UnityEngine.UI;

namespace VoxelBuilder
{
    /// <summary>
    /// Crosshair mínimo en el centro de la pantalla: dos barras finas formando
    /// una cruz, generadas por código para no depender de ningún sprite. Es
    /// puramente informativo, complementa a BlockHighlightRenderer (que solo
    /// marca un bloque cuando hay uno dentro del alcance) ayudando a apuntar
    /// incluso cuando no hay nada delante.
    /// </summary>
    public class CrosshairUI : MonoBehaviour
    {
        [SerializeField] private float size = 20f;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
        [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

        // Grosor deseado en píxeles reales de pantalla (el que se ve a 4K con
        // este mismo canvas). El brazo (size) sí queremos que crezca con la
        // resolución; el grosor en cambio se ve mejor si no lo hace, así que se
        // compensa a mano dividiendo por el factor de escala del canvas.
        [SerializeField] private float thicknessInPixels = 3.5f;

        private void Awake()
        {
            var canvasObject = new GameObject("Crosshair Canvas");
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // ConstantPixelSize (el valor por defecto de CanvasScaler) mantiene
            // el tamaño en píxeles fijo: a 4K el crosshair se ve ínfimo frente a
            // 1080p. ScaleWithScreenSize lo reescala con la resolución real.
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;

            float thickness = thicknessInPixels / ComputeCanvasScaleFactor();
            CreateBar(canvasObject.transform, new Vector2(size, thickness));
            CreateBar(canvasObject.transform, new Vector2(thickness, size));
        }

        /// <summary>Replica el cálculo de escala de CanvasScaler (modo ScaleWithScreenSize) para poder contrarrestarlo.</summary>
        private float ComputeCanvasScaleFactor()
        {
            float widthRatio = Screen.width / referenceResolution.x;
            float heightRatio = Screen.height / referenceResolution.y;
            return Mathf.Pow(widthRatio, 1f - matchWidthOrHeight) * Mathf.Pow(heightRatio, matchWidthOrHeight);
        }

        private void CreateBar(Transform parent, Vector2 barSize)
        {
            var barObject = new GameObject("CrosshairBar", typeof(Image));
            barObject.transform.SetParent(parent, worldPositionStays: false);
            barObject.GetComponent<Image>().color = color;

            RectTransform rect = barObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = barSize;
        }
    }
}
