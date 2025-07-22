namespace Forestry.Flo.External.Web.Exceptions;

public class NotExistingUserAccountException: Exception
{
    public NotExistingUserAccountException()
    {
    }

    public NotExistingUserAccountException(string message)
        : base(message)
    {
    }

    public NotExistingUserAccountException(string message, Exception inner) 
        : base(message, inner)
    {
    }
}