using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Object_Management.Editor
{
    /// <summary>
    /// 检查GameLevel的编辑器扩展
    /// </summary>
    [CustomEditor(typeof(GameLevel))]
    public class GameLevelInspector : UnityEditor.Editor
    {
        /// <summary>
        /// 绘制GUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var gameLevel = (GameLevel)target;
            if (gameLevel.HasMissingLevelObjects)
            {
                EditorGUILayout.HelpBox("Missing level objects!", MessageType.Error);
                if (GUILayout.Button("Remove Missing Elements"))
                {
                    Undo.RecordObject(gameLevel, "Remove Missing Level Objects.");
                    gameLevel.RemoveMissingLevelObjects();
                }
            }
        }
    }
}
