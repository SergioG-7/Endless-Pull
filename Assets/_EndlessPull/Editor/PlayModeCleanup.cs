#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Borra al volver a modo Edición cualquier objeto de combate creado en Play que se haya
// colado en la escena persistente, antes de que se pueda guardar con esa basura dentro.
[InitializeOnLoad]
internal static class PlayModeCleanup
{
    static PlayModeCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;

        int removed = 0;

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Object.DestroyImmediate(enemy);
            removed++;
        }

        var poolRoot = GameObject.Find("Pool_Projectiles");
        if (poolRoot != null)
        {
            Object.DestroyImmediate(poolRoot);
            removed++;
        }

        if (removed > 0)
            Debug.LogWarning($"[PlayModeCleanup] {removed} objeto(s) de Play se habían filtrado a la escena; borrados al volver a Edición.");
    }
}
#endif
