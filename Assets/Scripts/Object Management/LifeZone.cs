using UnityEngine;

namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 生存区域
    /// </summary>
    public class LifeZone : MonoBehaviour
    {
        #region 方法

        /// <summary>
        /// 检测是否有游戏对象离开区域
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerExit(Collider other)
        {
            var shape = other.GetComponent<Shape>();
            if (shape)
            {
                if (m_dyingDuration <= 0f)
                {
                    shape.Die();
                }
                else if (!shape.IsMarkedAsDying)
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
            Gizmos.color = Color.yellow;
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
                // 传递有损缩放
                Vector3 scale = transform.lossyScale;
                scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
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