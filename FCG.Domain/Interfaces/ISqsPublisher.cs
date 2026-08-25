namespace FCG.Application.Interfaces
{
    public interface ISqsPublisher
    {
        Task PublicarAsync<T>(T evento, string fila) where T : class;
    }
}
