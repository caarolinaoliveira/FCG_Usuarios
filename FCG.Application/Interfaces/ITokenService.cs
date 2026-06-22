namespace FCG.Application.Interfaces
{
    public interface ITokenService
    {
        string GerarToken( string email, IEnumerable<string> roles);
    }
}