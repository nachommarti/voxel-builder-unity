using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Dibuja un wireframe sobre el bloque al que apunta la cámara, con un único
    /// LineRenderer (componente incorporado del motor, sin ningún asset de
    /// contorno). Puramente visual: BlockInteractor decide qué bloque señalar.
    ///
    /// Un cubo tiene 8 vértices de grado impar, así que ninguna línea continua
    /// puede recorrer sus 12 aristas sin repetir alguna: esta secuencia de 16
    /// puntos es el mínimo (repite exactamente 3 aristas) para dibujar el
    /// wireframe completo con un solo LineRenderer.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class BlockHighlightRenderer : MonoBehaviour
    {
        private static readonly Vector3[] LocalPath =
        {
            new(0, 0, 0), new(1, 0, 0), new(1, 0, 1), new(0, 0, 1), new(0, 0, 0),
            new(0, 1, 0), new(1, 1, 0), new(1, 0, 0), new(1, 1, 0), new(1, 1, 1),
            new(1, 0, 1), new(1, 1, 1), new(0, 1, 1), new(0, 0, 1), new(0, 1, 1), new(0, 1, 0),
        };

        [SerializeField] private float lineWidth = 0.02f;

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = LocalPath.Length;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            // Reutiliza nuestro propio shader (ya en Always Included Shaders
            // para los chunks) en vez de depender del Unlit de URP: un
            // LineRenderer pinta sus vértices con start/endColor, que es
            // justo lo que VoxelBuilder/VertexColor lee por vértice.
            Shader shader = RuntimeShader.FindWithFallback("VoxelBuilder/VertexColor");
            lineRenderer.material = new Material(shader);
            lineRenderer.startColor = Color.black;
            lineRenderer.endColor = Color.black;

            lineRenderer.enabled = false;
        }

        public void ShowAt(Vector3Int blockPos)
        {
            for (int i = 0; i < LocalPath.Length; i++)
                lineRenderer.SetPosition(i, blockPos + LocalPath[i]);

            lineRenderer.enabled = true;
        }

        public void Hide()
        {
            lineRenderer.enabled = false;
        }
    }
}
