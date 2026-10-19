using System.ComponentModel.DataAnnotations;

namespace UniColab.Api.Dtos;

public sealed class Pagina<T>
{
    public int PaginaAtual { get; init; }
    public int Tamanho { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<T> Itens { get; init; } = [];
}

public sealed class LoginRequest
{
    [Required]
    public string Identificador { get; set; } = "";

    [Required]
    public string Senha { get; set; } = "";

    public string? SiglaInstituicao { get; set; }
}

public sealed class LoginResposta
{
    public string Token { get; set; } = "";
    public DateTime ExpiraEm { get; set; }
    public UsuarioResumo Usuario { get; set; } = new();
}

public sealed class UsuarioResumo
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string TipoPerfil { get; set; } = "";
    public long InstituicaoId { get; set; }
    public long? CursoId { get; set; }
    public string? Matricula { get; set; }
}

public sealed class CriarUsuarioRequest
{
    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Senha { get; set; } = "";

    [Required]
    public string TipoPerfil { get; set; } = "";

    [MaxLength(20)]
    public string? Matricula { get; set; }

    public long? CursoId { get; set; }
    public long[] CursoIds { get; set; } = [];
    public bool ElegivelEnade { get; set; }
}

public sealed class AtualizarUsuarioRequest
{
    public string? TipoPerfil { get; set; }
    public bool? Ativo { get; set; }
    public long? CursoId { get; set; }
    public bool LimparCurso { get; set; }
}

public sealed class AreaDto
{
    public short Id { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Componente { get; set; } = "";
}

public sealed class CursoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string Codigo { get; set; } = "";
}

public sealed class QuestaoListaItem
{
    public long Id { get; set; }
    public int Ano { get; set; }
    public short AreaId { get; set; }
    public string Area { get; set; } = "";
    public string Dificuldade { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Status { get; set; } = "";
    public string Enunciado { get; set; } = "";
}

public sealed class AlternativaDto
{
    public long Id { get; set; }
    public string Letra { get; set; } = "";
    public byte Ordem { get; set; }
    public string Texto { get; set; } = "";
    public bool? EhCorreta { get; set; }
    public string? Explicacao { get; set; }
}

public sealed class QuestaoDetalhe
{
    public long Id { get; set; }
    public int Ano { get; set; }
    public short AreaId { get; set; }
    public string Area { get; set; } = "";
    public string Dificuldade { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Status { get; set; } = "";
    public string Origem { get; set; } = "";
    public string Enunciado { get; set; } = "";
    public string? PadraoResposta { get; set; }
    public IReadOnlyList<AlternativaDto> Alternativas { get; set; } = [];
}

public sealed class SalvarQuestaoRequest
{
    [Required, MinLength(15)]
    public string Enunciado { get; set; } = "";

    [Range(2004, 2100)]
    public int Ano { get; set; }

    public short AreaId { get; set; }

    [Required]
    public string Dificuldade { get; set; } = "";

    public string Tipo { get; set; } = "multipla_escolha";
    public long? ProvaId { get; set; }
    public string? CodigoItem { get; set; }
    public string? PadraoResposta { get; set; }
}

public sealed class SalvarAlternativaRequest
{
    [Required]
    public string Letra { get; set; } = "";

    [Range(1, 5)]
    public byte Ordem { get; set; }

    [Required]
    public string Texto { get; set; } = "";

    public bool EhCorreta { get; set; }
}

public sealed class SalvarJustificativaRequest
{
    [Required, MinLength(10)]
    public string Explicacao { get; set; } = "";
}

public sealed class RetirarQuestaoRequest
{
    [Required, MinLength(5)]
    public string Motivo { get; set; } = "";
}

public sealed class ResponderQuestaoRequest
{
    public long AlternativaId { get; set; }
    public int TempoSegundos { get; set; }
}

public sealed class ResultadoResposta
{
    public long TentativaId { get; set; }
    public bool Acertou { get; set; }
    public decimal Nota { get; set; }
    public string LetraCorreta { get; set; } = "";
    public IReadOnlyList<AlternativaDto> Alternativas { get; set; } = [];
}

public sealed class CriarSimuladoRequest
{
    [Required, MaxLength(200)]
    public string Titulo { get; set; } = "";

    public string? Descricao { get; set; }
    public string ModoComposicao { get; set; } = "fixo";

    [Range(1, 600)]
    public int TempoLimiteMinutos { get; set; } = 240;

    [Range(1, 200)]
    public int QuantidadeQuestoes { get; set; } = 35;

    public long? CursoId { get; set; }
    public short? FiltroAreaId { get; set; }
    public string? FiltroDificuldade { get; set; }
    public int? FiltroAno { get; set; }
}

public sealed class DefinirQuestoesSimuladoRequest
{
    [Required]
    public long[] QuestaoIds { get; set; } = [];
}

public sealed class SimuladoDto
{
    public long Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descricao { get; set; }
    public string ModoComposicao { get; set; } = "";
    public int TempoLimiteMinutos { get; set; }
    public int QuantidadeQuestoes { get; set; }
    public long? CursoId { get; set; }
    public short? FiltroAreaId { get; set; }
    public string? FiltroDificuldade { get; set; }
    public int? FiltroAno { get; set; }
    public bool Ativo { get; set; }
    public IReadOnlyList<long> QuestaoIds { get; set; } = [];
}

public sealed class RegistrarRespostaRequest
{
    public long QuestaoId { get; set; }
    public long? AlternativaId { get; set; }
    public bool MarcadaRevisao { get; set; }
    public int TempoSegundos { get; set; }
}

public sealed class FolhaItemDto
{
    public short Ordem { get; set; }
    public long QuestaoId { get; set; }
    public string Enunciado { get; set; } = "";
    public long? AlternativaSelecionadaId { get; set; }
    public bool MarcadaRevisao { get; set; }
    public bool? Acertou { get; set; }
    public int TempoRespostaSegundos { get; set; }
    public IReadOnlyList<AlternativaDto> Alternativas { get; set; } = [];
}

public sealed class TentativaDto
{
    public long Id { get; set; }
    public string Modo { get; set; } = "";
    public long? SimuladoId { get; set; }
    public string? SimuladoTitulo { get; set; }
    public string Status { get; set; } = "";
    public DateTime IniciadoEm { get; set; }
    public DateTime? LimiteEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public int TempoGastoSegundos { get; set; }
    public decimal? NotaTotal { get; set; }
    public IReadOnlyList<FolhaItemDto> Itens { get; set; } = [];
}

public sealed class TentativaResumo
{
    public long Id { get; set; }
    public string Modo { get; set; } = "";
    public string? Titulo { get; set; }
    public string Status { get; set; } = "";
    public DateTime IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public decimal NotaTotal { get; set; }
}

public sealed class ComentarioRequest
{
    [Required, MinLength(1), MaxLength(4000)]
    public string Mensagem { get; set; } = "";

    public long? ComentarioPaiId { get; set; }
}

public sealed class ComentarioDto
{
    public long Id { get; set; }
    public long? ComentarioPaiId { get; set; }
    public long UsuarioId { get; set; }
    public string Autor { get; set; } = "";
    public string Papel { get; set; } = "";
    public string Mensagem { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CriadoEm { get; set; }
}

public sealed class ReporteRequest
{
    [Required]
    public string Motivo { get; set; } = "";

    [MaxLength(500)]
    public string? Detalhe { get; set; }
}

public sealed class ModerarComentarioRequest
{
    [Required]
    public string Status { get; set; } = "";
}

public sealed class JulgarReporteRequest
{
    [Required]
    public string Status { get; set; } = "";
}

public sealed class FilaItemDto
{
    public long ReporteId { get; set; }
    public long QuestaoId { get; set; }
    public long ComentarioId { get; set; }
    public string Motivo { get; set; } = "";
    public string? Detalhe { get; set; }
    public string Mensagem { get; set; } = "";
    public DateTime CriadoEm { get; set; }
}

public sealed class MetricaDto
{
    public short AreaId { get; set; }
    public string Area { get; set; } = "";
    public int TotalResolvidas { get; set; }
    public int TotalAcertos { get; set; }
    public decimal PercentualAcerto { get; set; }
}

public sealed class BadgeDto
{
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public int Pontos { get; set; }
    public string? Area { get; set; }
    public DateTime ConcedidoEm { get; set; }
}

public sealed class TrilhaDto
{
    public long Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Origem { get; set; } = "";
    public DateTime CriadaEm { get; set; }
    public IReadOnlyList<TrilhaItemDto> Questoes { get; set; } = [];
}

public sealed class TrilhaItemDto
{
    public short Ordem { get; set; }
    public long QuestaoId { get; set; }
    public string Motivo { get; set; } = "";
    public string Area { get; set; } = "";
    public string Dificuldade { get; set; } = "";
}

public sealed class LancamentoDto
{
    public string Tipo { get; set; } = "";
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = "";
    public DateTime CriadoEm { get; set; }
}

public sealed class SaldoDto
{
    public int Lancamentos { get; set; }
    public decimal Saldo { get; set; }
}

public sealed class ChamadoRequest
{
    [Required, MinLength(5), MaxLength(180)]
    public string Assunto { get; set; } = "";

    [Required, MinLength(10)]
    public string Descricao { get; set; } = "";
}

public sealed class ChamadoDto
{
    public long Id { get; set; }
    public string Assunto { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CriadoEm { get; set; }
}
