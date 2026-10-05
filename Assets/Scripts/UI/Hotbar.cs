using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VoxelBuilder
{
    /// <summary>
    /// Selección del bloque activo con las teclas 1-7 (una por cada BlockType
    /// distinto de Air) o con la rueda del ratón, y una etiqueta de texto
    /// mínima mostrando cuál está seleccionado. Consulta directamente
    /// Keyboard.current/Mouse.current en vez de añadir acciones al asset de
    /// Input: son controles fijos de una UI de selección, no algo reasignable.
    /// </summary>
    public class Hotbar : MonoBehaviour
    {
        /// <summary>Acceso de conveniencia, igual que World.Instance: solo existe un Hotbar por partida.</summary>
        public static Hotbar Instance { get; private set; }

        private static readonly BlockType[] SelectableBlocks =
        {
            BlockType.Stone, BlockType.Dirt, BlockType.Grass,
            BlockType.Sand, BlockType.Wood, BlockType.Leaves, BlockType.Water,
        };

        public BlockType SelectedBlock { get; private set; } = SelectableBlocks[0];

        private Text label;

        private void Awake()
        {
            Instance = this;
            label = CreateLabel();
            UpdateLabel();
        }

        private void Update()
        {
            ReadNumberKeys();
            ReadScrollWheel();
        }

        private void ReadNumberKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (int i = 0; i < SelectableBlocks.Length; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) Select(SelectableBlocks[i]);
            }
        }

        private void ReadScrollWheel()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) SelectRelative(-1);
            else if (scroll < 0f) SelectRelative(1);
        }

        /// <summary>Avanza/retrocede en la lista de bloques seleccionables, dando la vuelta en los extremos.</summary>
        private void SelectRelative(int step)
        {
            int currentIndex = Array.IndexOf(SelectableBlocks, SelectedBlock);
            int nextIndex = (currentIndex + step + SelectableBlocks.Length) % SelectableBlocks.Length;
            Select(SelectableBlocks[nextIndex]);
        }

        private void Select(BlockType block)
        {
            SelectedBlock = block;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            int slot = Array.IndexOf(SelectableBlocks, SelectedBlock) + 1;
            label.text = $"Bloque [{slot}]: {SelectedBlock}";
        }

        private Text CreateLabel()
        {
            var canvasObject = new GameObject("Hotbar Canvas");
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var textObject = new GameObject("SelectedBlockLabel", typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, worldPositionStays: false);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.LowerLeft;
            text.color = Color.white;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 24f);
            rect.sizeDelta = new Vector2(500f, 50f);

            return text;
        }
    }
}
