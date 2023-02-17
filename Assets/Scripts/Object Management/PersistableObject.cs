using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 可持久化对象
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistableObject : MonoBehaviour
    {
        /// <summary>
        /// 存档游戏对象
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Save(GameDataWriter writer)
        {
            writer.Write(transform.localPosition);
            writer.Write(transform.localRotation);
            writer.Write(transform.localScale);
        }

        /// <summary>
        /// 读档游戏对象
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Load(GameDataReader reader)
        {
            transform.localPosition = reader.ReadVector3();
            transform.localRotation = reader.ReadQuaternion();
            transform.localScale = reader.ReadVector3();
        }
    }
}
