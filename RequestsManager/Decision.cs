namespace RequestsManagerAPI
{
    public enum Decision
    {
        Accepted,
        Refused,
        Cancelled,
        ForceCancelled,
        SenderFailedCondition,
        ReceiverFailedCondition,
        SenderLeft,
        ReceiverLeft,
        AlreadySent,
        Expired
    }
}