using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏关卡
    /// </summary>
    public class GameLevel : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Start()
        {
            Game.Instance.SpawnZoneOfLevel = m_spawnZone;
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 游戏对象的生成领域
        /// </summary>
        [SerializeField]
        private SpawnZone m_spawnZone;

        #endregion
    }
}
