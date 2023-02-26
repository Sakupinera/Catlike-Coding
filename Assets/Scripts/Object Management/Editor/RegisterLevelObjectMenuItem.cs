using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 注册关卡对象菜单项
    /// </summary>
    static class RegisterLevelObjectMenuItem
    {

        /// <summary>
        /// 注册等级对象
        /// </summary>
        [MenuItem(menuItem)]
        static void RegisterLevelObject()
        {
            foreach (Object o in Selection.objects)
            {
                Register(o as GameObject);
            }
        }

        /// <summary>
        /// 注册对象
        /// </summary>
        /// <param name="o"></param>
        static void Register(GameObject o)
        {
            // 检查是不是预制体
            if (PrefabUtility.GetPrefabType(o) == PrefabType.Prefab)
            {
                Debug.LogWarning(o.name + " is a prefab asset.", o);
            }

            // 接下来，获取GameLevelObject组件；如果没有，则中止
            var levelObject = o.GetComponent<GameLevelObject>();
            if (levelObject == null)
            {
                Debug.LogWarning(o.name + " isn't a game level object.", o);
                return;
            }

            foreach (GameObject rootObject in o.scene.GetRootGameObjects())
            {
                var gameLevel = rootObject.GetComponent<GameLevel>();
                if (gameLevel != null)
                {
                    // 如果我们找到了游戏关卡，检查对象是否已经被注册，如果是这样就终止
                    if (gameLevel.HasLevelObject(levelObject))
                    {
                        Debug.LogWarning(o.name + " is already registered.", o);
                        return;
                    }

                    Undo.RecordObject(gameLevel, "Register Level Object.");
                    gameLevel.RegisterLevelObject(levelObject);
                    Debug.Log(
                        o.name + " registered to game level " +
                        gameLevel.name + " in scene " + o.scene.name + ".", o
                    );
                    return;
                }
            }
            Debug.LogWarning(o.name + " isn't part of a game level.", o);
        }

        /// <summary>
        /// 验证选中对象
        /// </summary>
        /// <returns></returns>
        [MenuItem(menuItem, true)]
        static bool ValidateRegisterLevelObject()
        {
            if (Selection.objects.Length == 0)
            {
                return false;
            }

            foreach (Object o in Selection.objects)
            {
                if (!(o is GameObject))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// MenuItem路径
        /// </summary>
        private const string menuItem = "GameObject/Register Level Object";
    }
}
