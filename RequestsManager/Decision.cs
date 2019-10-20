namespace RequestsManagerAPI
{
    public enum Decision
    {
        Accepted,
        AcceptedAnother,
        Refused,
        Cancelled,
        Disposed,
        RequestedOwnPlayer,
        Blocked,
        SenderFailedCondition,
        ReceiverFailedCondition,
        SenderLeft,
        ReceiverLeft,
        AlreadySentToSamePlayer,
        AlreadySentToDifferentPlayer,
        Expired
    }
}