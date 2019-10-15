namespace RequestsManagerAPI
{
    public enum Decision
    {
        Accepted,
        Refused,
        Cancelled,
        Disposed,
        SenderFailedCondition,
        ReceiverFailedCondition,
        SenderLeft,
        ReceiverLeft,
        AlreadySent,
        Expired
    }
}