namespace RequestsManagerAPI
{
    #region ICondition

    public interface ICondition
    {
        bool Broken { get; }
        void TryToBreak(object Player);
    }

    #endregion
    public abstract class Condition<T> : ICondition
    {
        #region Data

        public bool Broken { get; protected set; }
        public bool Active { get; }
        public T Value { get; }

        #endregion
        #region Constructor

        public Condition()
        {
            this.Broken = false;
            this.Active = false;
            this.Value = default;
        }
        public Condition(T Value)
        {
            this.Broken = false;
            this.Active = true;
            this.Value = Value;
        }

        #endregion

        public void TryToBreak(object Player)
        {
            if (!Broken && !InvalidPlayer(Player) && Broke(Player))
                RequestsManager.BrokeCondition(Player, GetType());
        }

        protected abstract bool InvalidPlayer(object Player);
        protected abstract bool Broke(object Player);
    }
}