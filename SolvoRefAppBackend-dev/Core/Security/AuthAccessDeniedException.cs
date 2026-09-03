namespace Core.Security
{
    public class AuthAccessDeniedException : Exception
    {
        public AuthAccessDeniedException(string message) : base(message)
        {
        }
    }
}
