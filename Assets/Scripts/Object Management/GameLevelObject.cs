namespace Assets.Scripts.Object_Management
{
    /// <summary>
    /// 游戏关卡对象
    /// </summary>
    public class GameLevelObject : PersistableObject
    {
        /// <summary>
        /// 对象更新逻辑
        /// </summary>
        public virtual void GameUpdate() { }
    }
}
