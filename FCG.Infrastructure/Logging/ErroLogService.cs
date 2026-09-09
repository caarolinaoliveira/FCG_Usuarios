using System.Diagnostics;
using System.Text.Json;
using FCG.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FCG.Infrastructure.Logging
{
    public class ErroLogService : IErroLogService
    {
        private const string Servico = "FCG.Usuarios";

        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly ILogger<ErroLogService> _logger;

        public ErroLogService(IMongoClient mongoClient, IConfiguration configuration, ILogger<ErroLogService> logger)
        {
            var eventLogDatabase = mongoClient.GetDatabase(configuration["MongoDB:EventLogDatabase"] ?? "FCG_EventLog");
            _collection = eventLogDatabase.GetCollection<BsonDocument>("erros");
            _logger = logger;
        }

        public async Task RegistrarAsync(Exception excecao, string contexto, object? dadosAdicionais = null)
        {
            try
            {
                var documento = new BsonDocument
                {
                    { "servico", Servico },
                    { "contexto", contexto },
                    { "tipoExcecao", excecao.GetType().Name },
                    { "mensagem", excecao.Message },
                    { "stackTrace", excecao.StackTrace ?? string.Empty },
                    { "correlationId", Activity.Current?.TraceId.ToString() ?? (BsonValue)BsonNull.Value },
                    { "ocorridoEm", DateTime.UtcNow }
                };

                if (dadosAdicionais is not null)
                {
                    documento["dados"] = BsonDocument.Parse(JsonSerializer.Serialize(dadosAdicionais));
                }

                await _collection.InsertOneAsync(documento);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao registrar erro no Mongo | Contexto: {Contexto}", contexto);
            }
        }
    }
}
