namespace Forestry.Flo.External.Web.Exceptions;

public class MessageConsumptionException : Exception
{
    public MessageConsumptionException()
    {
    }

    public MessageConsumptionException(string message) 
        : base(message)
    {
    }

    public MessageConsumptionException(string message, Exception inner) 
        : base(message, inner)
    {
    }
}