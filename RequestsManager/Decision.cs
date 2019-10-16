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
        SenderFailedCondition,
        ReceiverFailedCondition,
        SenderLeft,
        ReceiverLeft,
        AlreadySentToSamePlayer,
        AlreadySentToDifferentPlayer,
        Expired
    }
}