using FCG.Domain.Interfaces;
using FCG.Domain.Entities;
using FCG.Application.Requests.Usuarios;
using FCG.Application.Events;
using FCG.Application.Interfaces;

namespace FCG.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ISqsPublisher _sqsPublisher;

        public UsuarioService(IUsuarioRepository repository, IMessagePublisher messagePublisher, ISqsPublisher sqsPublisher)
        {
            _repository = repository;
            _messagePublisher = messagePublisher;
            _sqsPublisher = sqsPublisher;
        }

        public async Task CriarAsync(string identityUserId, CriarUsuarioRequest request)
        {
            var usuario = new UsuarioEntity
            {
                Nome = request.Nome,
                Email = request.Email,
                DataNascimento = request.DataNascimento!.Value,
                IdentityUserId = identityUserId
            };

            await _repository.AdicionarAsync(usuario);

            var evento = new UserCreatedEvent
            {
                UserId = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                CriadoEm = DateTime.UtcNow
            };

            await _messagePublisher.PublicarAsync(evento, "user.created");
            await _sqsPublisher.PublicarAsync(evento, "user-created");
        }

        public async Task<UsuarioEntity?> ObterPorIdentityIdAsync(string identityUserId)
            => await _repository.ObterPorIdentityIdAsync(identityUserId);
    }
}