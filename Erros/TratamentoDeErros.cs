using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using UniColab.Api.Seguranca;

namespace UniColab.Api.Erros;

public sealed class TratamentoDeErros : IMiddleware
{
    private readonly ILogger<TratamentoDeErros> _logger;

    public TratamentoDeErros(ILogger<TratamentoDeErros> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (NaoEncontradoException ex)
        {
            await Escrever(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (AcessoNegadoException ex)
        {
            await Escrever(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (RegraNegocioException ex)
        {
            await Escrever(context, StatusCodes.Status422UnprocessableEntity, ex.Message);
        }
        catch (Exception ex) when (TraduzirBanco(ex) is { } banco)
        {
            await Escrever(context, banco.status, banco.mensagem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha não tratada em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);
            await Escrever(context, StatusCodes.Status500InternalServerError, "Falha interna ao processar a requisição.");
        }
    }

    private static (int status, string mensagem)? TraduzirBanco(Exception ex)
    {
        var atual = ex;
        while (atual is not null)
        {
            if (atual is MySqlException mysql)
            {
                if (mysql.SqlState == "45000" || mysql.Number == 1644)
                {
                    return (StatusCodes.Status422UnprocessableEntity, mysql.Message);
                }

                if (mysql.Number == 1062)
                {
                    return (StatusCodes.Status409Conflict, "Esse registro já existe.");
                }

                if (mysql.Number is 1452 or 3819)
                {
                    return (StatusCodes.Status422UnprocessableEntity, mysql.Message);
                }
            }

            atual = atual is DbUpdateException ? atual.InnerException : atual.InnerException;
            if (atual == ex)
            {
                break;
            }
        }

        return null;
    }

    private static Task Escrever(HttpContext context, int status, string mensagem)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new { mensagem });
    }
}
