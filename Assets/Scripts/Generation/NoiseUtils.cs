using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Ruido Perlin por octavas construido sobre Mathf.PerlinNoise (API nativa de
    /// Unity, sin dependencias externas). Sumar octavas de frecuencia creciente y
    /// amplitud decreciente da un terreno con forma general suave y detalle fino
    /// superpuesto, en vez del aspecto uniforme de una sola octava.
    /// </summary>
    public static class NoiseUtils
    {
        /// <summary>
        /// Devuelve ruido en el rango aproximado [-1, 1] para la columna (x,z).
        /// </summary>
        /// <param name="seed">Desplaza el muestreo de forma determinista para variar el mundo sin cambiar el algoritmo.</param>
        /// <param name="octaves">Número de capas de ruido sumadas.</param>
        /// <param name="persistence">Cuánto se atenúa la amplitud en cada octava sucesiva (0-1).</param>
        /// <param name="scale">Frecuencia base: valores más pequeños dan colinas más anchas.</param>
        public static float OctavePerlin(float x, float z, int seed, int octaves, float persistence, float scale)
        {
            float seedOffsetX = seed * 0.1013f;
            float seedOffsetZ = seed * 0.7919f;

            float total = 0f;
            float amplitude = 1f;
            float frequency = scale;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + seedOffsetX) * frequency;
                float sampleZ = (z + seedOffsetZ) * frequency;

                total += Mathf.PerlinNoise(sampleX, sampleZ) * amplitude;
                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2f;
            }

            float normalized = total / maxValue; // ~[0,1]
            return normalized * 2f - 1f;          // remapeado a [-1,1]
        }
    }
}
