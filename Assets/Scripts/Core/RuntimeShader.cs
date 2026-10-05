using UnityEngine;

namespace VoxelBuilder
{
    /// <summary>
    /// Busca un shader por nombre con una cadena de alternativas de reserva,
    /// terminando siempre en el shader de error interno de Unity (compilado
    /// dentro del propio motor, nunca se elimina de ningún build).
    ///
    /// Existe porque Shader.Find puede devolver null en un build si ese shader
    /// no está en "Always Included Shaders" (Project Settings > Graphics): al
    /// usarse solo desde código, sin ningún Material del proyecto que lo
    /// referencie, Unity no detecta que se usa y lo descarta al compilar. Sin
    /// esta red de seguridad, new Material(null) lanza ArgumentNullException y
    /// puede tirar por la mitad la inicialización de todo el juego.
    /// </summary>
    public static class RuntimeShader
    {
        public static Shader FindWithFallback(params string[] shaderNames)
        {
            foreach (string name in shaderNames)
            {
                Shader shader = Shader.Find(name);
                if (shader != null) return shader;
                Debug.LogWarning($"[RuntimeShader] No se encontró el shader '{name}'.");
            }

            Debug.LogError("[RuntimeShader] Ningún shader de la lista está disponible; usando el shader de error interno de Unity.");
            return Shader.Find("Hidden/InternalErrorShader");
        }
    }
}
