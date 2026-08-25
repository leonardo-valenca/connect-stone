namespace ConnectStone.Sdk.Exceptions;

public abstract class ConnectStoneException : Exception
{
    protected ConnectStoneException(string message) : base(message)
    {
    }

    protected ConnectStoneException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
