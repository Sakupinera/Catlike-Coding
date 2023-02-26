using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// Kill区域
    /// </summary>
    public class KillZone : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 检测是否有游戏对象进入Kill区域
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            var shape = other.GetComponent<Shape>();
            if (shape)
            {
                if (m_dyingDuration <= 0f)
                {
                    shape.Die();
                }
                else if(!shape.IsMarkedAsDying)
                {
                    shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, m_dyingDuration);
                }
            }
        }

        /// <summary>
        /// 可视化区域大小
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.matrix = transform.localToWorldMatrix;
            var c = GetComponent<Collider>();

            var b = c as BoxCollider;
            if (b != null)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(b.center, b.size);
                return;
            }
            var s = c as SphereCollider;
            if (s != null)
            {
                Vector3 scale = transform.lossyScale;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
                Gizmos.DrawWireSphere(s.center, s.radius);
                return;
            }
        }

        #endregion

        #region 依赖的字段

        /// <summary>
        /// 垂死的持续时间
        /// </summary>
        [SerializeField]
        private float m_dyingDuration;

        #endregion
    }
}
