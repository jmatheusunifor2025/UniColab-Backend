namespace UniColab.Api.Entities;

public sealed class Instituicao
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string Sigla { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
}

public sealed class Curso
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public string Nome { get; set; } = "";
    public string Codigo { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
}

public sealed class Usuario
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long? CursoId { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string TipoPerfil { get; set; } = "estudante";
    public string? Matricula { get; set; }
    public string? IdentificadorExterno { get; set; }
    public bool ElegivelEnade { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime? DesativadoEm { get; set; }
    public long? DesativadoPorId { get; set; }
    public DateTime? UltimoAcessoEm { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public sealed class DocenteCurso
{
    public long UsuarioId { get; set; }
    public long CursoId { get; set; }
    public long InstituicaoId { get; set; }
}

public sealed class AreaConhecimento
{
    public short Id { get; set; }
    public string Nome { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Componente { get; set; } = "";
    public bool Ativo { get; set; } = true;
}

public sealed class ProvaOficial
{
    public long Id { get; set; }
    public short Ano { get; set; }
    public string Nome { get; set; } = "";
}

public sealed class Questao
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long? ProvaId { get; set; }
    public short AreaId { get; set; }
    public long? CriadoPorId { get; set; }
    public long? RevisadoPorId { get; set; }
    public string Enunciado { get; set; } = "";
    public int Ano { get; set; }
    public string Dificuldade { get; set; } = "";
    public string Tipo { get; set; } = "multipla_escolha";
    public string Origem { get; set; } = "institucional";
    public string? CodigoItem { get; set; }
    public string? PadraoResposta { get; set; }
    public string Status { get; set; } = "rascunho";
    public string? MotivoRetirada { get; set; }
    public DateTime? RetiradaEm { get; set; }
    public long? RetiradaPorId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public sealed class Alternativa
{
    public long Id { get; set; }
    public long QuestaoId { get; set; }
    public long InstituicaoId { get; set; }
    public string Letra { get; set; } = "";
    public byte Ordem { get; set; }
    public string Texto { get; set; } = "";
    public bool EhCorreta { get; set; }
}

public sealed class Justificativa
{
    public long Id { get; set; }
    public long AlternativaId { get; set; }
    public long InstituicaoId { get; set; }
    public long ProfessorId { get; set; }
    public string Explicacao { get; set; } = "";
    public bool Validada { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public sealed class Simulado
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long? CursoId { get; set; }
    public long CriadoPorId { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descricao { get; set; }
    public string ModoComposicao { get; set; } = "fixo";
    public int TempoLimiteMinutos { get; set; } = 240;
    public int QuantidadeQuestoes { get; set; } = 35;
    public short? FiltroAreaId { get; set; }
    public string? FiltroDificuldade { get; set; }
    public int? FiltroAno { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
}

public sealed class SimuladoQuestao
{
    public long SimuladoId { get; set; }
    public long QuestaoId { get; set; }
    public long InstituicaoId { get; set; }
    public short Ordem { get; set; }
}

public sealed class Trilha
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long UsuarioId { get; set; }
    public string Titulo { get; set; } = "";
    public string Origem { get; set; } = "sistema";
    public DateTime CriadaEm { get; set; }
}

public sealed class TrilhaQuestao
{
    public long TrilhaId { get; set; }
    public long QuestaoId { get; set; }
    public long InstituicaoId { get; set; }
    public short Ordem { get; set; }
    public string Motivo { get; set; } = "";
}

public sealed class Tentativa
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long UsuarioId { get; set; }
    public long? SimuladoId { get; set; }
    public long? TrilhaId { get; set; }
    public string Modo { get; set; } = "";
    public DateTime IniciadoEm { get; set; }
    public DateTime? LimiteEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public int TempoGastoSegundos { get; set; }
    public decimal NotaTotal { get; set; }
    public string Status { get; set; } = "em_andamento";
}

public sealed class TentativaQuestao
{
    public long Id { get; set; }
    public long TentativaId { get; set; }
    public long QuestaoId { get; set; }
    public long InstituicaoId { get; set; }
    public short Ordem { get; set; }
    public long? AlternativaSelecionadaId { get; set; }
    public bool MarcadaRevisao { get; set; }
    public bool Acertou { get; set; }
    public int TempoRespostaSegundos { get; set; }
    public DateTime? RespondidaEm { get; set; }
}

public sealed class ComentarioForum
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long QuestaoId { get; set; }
    public long UsuarioId { get; set; }
    public long? ComentarioPaiId { get; set; }
    public string Mensagem { get; set; } = "";
    public string Status { get; set; } = "publicado";
    public long? ModeradoPorId { get; set; }
    public DateTime? ModeradoEm { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? EditadoEm { get; set; }
}

public sealed class ForumReporte
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long ComentarioId { get; set; }
    public long QuestaoId { get; set; }
    public long ReportadoPorId { get; set; }
    public string Motivo { get; set; } = "";
    public string? Detalhe { get; set; }
    public string Status { get; set; } = "aberto";
    public long? AnalisadoPorId { get; set; }
    public DateTime? AnalisadoEm { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class DashboardMetrica
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long UsuarioId { get; set; }
    public short AreaId { get; set; }
    public int TotalResolvidas { get; set; }
    public int TotalAcertos { get; set; }
    public decimal PercentualAcerto { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

public sealed class Badge
{
    public short Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public int Pontos { get; set; }
    public string CriterioTipo { get; set; } = "";
    public int CriterioQuantidade { get; set; }
    public decimal? CriterioPercentual { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class UsuarioBadge
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public short BadgeId { get; set; }
    public short? AreaId { get; set; }
    public int Pontos { get; set; }
    public DateTime ConcedidoEm { get; set; }
}

public sealed class DocenteLancamento
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long ProfessorId { get; set; }
    public string Tipo { get; set; } = "";
    public decimal Valor { get; set; }
    public string ReferenciaTipo { get; set; } = "";
    public long ReferenciaId { get; set; }
    public string Descricao { get; set; } = "";
    public DateTime CriadoEm { get; set; }
}

public sealed class ChamadoTi
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long AbertoPorId { get; set; }
    public string Assunto { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Status { get; set; } = "aberto";
    public DateTime CriadoEm { get; set; }
    public DateTime? EncerradoEm { get; set; }
}

public sealed class AuditoriaRegistro
{
    public long Id { get; set; }
    public long InstituicaoId { get; set; }
    public long? AtorId { get; set; }
    public string Acao { get; set; } = "";
    public string Entidade { get; set; } = "";
    public long EntidadeId { get; set; }
    public string? Detalhe { get; set; }
    public DateTime CriadoEm { get; set; }
}

public sealed class HeatmapLinha
{
    public long InstituicaoId { get; set; }
    public long CursoId { get; set; }
    public string Curso { get; set; } = "";
    public short AreaId { get; set; }
    public string AreaConhecimento { get; set; } = "";
    public long TotalRespostas { get; set; }
    public decimal TotalAcertos { get; set; }
    public decimal PercentualAcerto { get; set; }
}

public sealed class AdesaoLinha
{
    public long InstituicaoId { get; set; }
    public long CursoId { get; set; }
    public string Curso { get; set; } = "";
    public long Elegiveis { get; set; }
    public long Participantes { get; set; }
    public decimal TaxaAdesao { get; set; }
}
