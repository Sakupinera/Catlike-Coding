using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏对象生成区域
    /// </summary>
    public abstract class SpawnZone : PersistableObject
    {
        /// <summary>
        /// 获取一个任意的生成点
        /// </summary>
        public abstract Vector3 SpawnPoint { get; }
    }
}
