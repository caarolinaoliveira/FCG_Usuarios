namespace FCG.Application.Responses.Usuarios
{
    public sealed record LoginResponse
    {
        public string AccessToken { get; set; }
        public DateTime Expiration { get; set; }


    }
    
}