using Microsoft.EntityFrameworkCore;
using UniColab.Api.Entities;

namespace UniColab.Api.Data;

public sealed class UnicolabDbContext : DbContext
{
    public UnicolabDbContext(DbContextOptions<UnicolabDbContext> options) : base(options)
    {
    }

    public DbSet<Instituicao> Instituicoes => Set<Instituicao>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<DocenteCurso> DocenteCursos => Set<DocenteCurso>();
    public DbSet<AreaConhecimento> Areas => Set<AreaConhecimento>();
    public DbSet<ProvaOficial> Provas => Set<ProvaOficial>();
    public DbSet<Questao> Questoes => Set<Questao>();
    public DbSet<Alternativa> Alternativas => Set<Alternativa>();
    public DbSet<Justificativa> Justificativas => Set<Justificativa>();
    public DbSet<Simulado> Simulados => Set<Simulado>();
    public DbSet<SimuladoQuestao> SimuladoQuestoes => Set<SimuladoQuestao>();
    public DbSet<Trilha> Trilhas => Set<Trilha>();
    public DbSet<TrilhaQuestao> TrilhaQuestoes => Set<TrilhaQuestao>();
    public DbSet<Tentativa> Tentativas => Set<Tentativa>();
    public DbSet<TentativaQuestao> TentativaQuestoes => Set<TentativaQuestao>();
    public DbSet<ComentarioForum> Comentarios => Set<ComentarioForum>();
    public DbSet<ForumReporte> Reportes => Set<ForumReporte>();
    public DbSet<DashboardMetrica> Metricas => Set<DashboardMetrica>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UsuarioBadge> UsuarioBadges => Set<UsuarioBadge>();
    public DbSet<DocenteLancamento> Lancamentos => Set<DocenteLancamento>();
    public DbSet<ChamadoTi> Chamados => Set<ChamadoTi>();
    public DbSet<AuditoriaRegistro> Auditoria => Set<AuditoriaRegistro>();
    public DbSet<HeatmapLinha> Heatmap => Set<HeatmapLinha>();
    public DbSet<AdesaoLinha> Adesao => Set<AdesaoLinha>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Instituicao>().ToTable("instituicoes");
        modelBuilder.Entity<Curso>().ToTable("cursos");
        modelBuilder.Entity<Usuario>().ToTable("usuarios");
        modelBuilder.Entity<DocenteCurso>().ToTable("docente_cursos").HasKey(d => new { d.UsuarioId, d.CursoId });
        modelBuilder.Entity<AreaConhecimento>().ToTable("areas_conhecimento");
        modelBuilder.Entity<ProvaOficial>().ToTable("provas_oficiais");
        modelBuilder.Entity<Questao>().ToTable("questoes");
        modelBuilder.Entity<Alternativa>().ToTable("alternativas");
        modelBuilder.Entity<Justificativa>().ToTable("justificativas");
        modelBuilder.Entity<Simulado>().ToTable("simulados");
        modelBuilder.Entity<SimuladoQuestao>().ToTable("simulado_questoes").HasKey(s => new { s.SimuladoId, s.Ordem });
        modelBuilder.Entity<Trilha>().ToTable("trilhas");
        modelBuilder.Entity<TrilhaQuestao>().ToTable("trilha_questoes").HasKey(t => new { t.TrilhaId, t.Ordem });
        modelBuilder.Entity<Tentativa>().ToTable("tentativas");
        modelBuilder.Entity<TentativaQuestao>().ToTable("tentativas_questoes");
        modelBuilder.Entity<ComentarioForum>().ToTable("comentarios_forum");
        modelBuilder.Entity<ForumReporte>().ToTable("forum_reportes");
        modelBuilder.Entity<DashboardMetrica>().ToTable("dashboard_metricas");
        modelBuilder.Entity<Badge>().ToTable("badges");
        modelBuilder.Entity<UsuarioBadge>().ToTable("usuario_badges");
        modelBuilder.Entity<DocenteLancamento>().ToTable("docente_lancamentos");
        modelBuilder.Entity<ChamadoTi>().ToTable("chamados_ti");
        modelBuilder.Entity<AuditoriaRegistro>().ToTable("auditoria");

        modelBuilder.Entity<HeatmapLinha>().HasNoKey().ToView("vw_heatmap_curso");
        modelBuilder.Entity<AdesaoLinha>().HasNoKey().ToView("vw_adesao_curso");

        modelBuilder.Entity<DashboardMetrica>()
            .Property(m => m.PercentualAcerto)
            .ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<TentativaQuestao>()
            .Property(t => t.Acertou)
            .ValueGeneratedOnAddOrUpdate();

        modelBuilder.Entity<AuditoriaRegistro>()
            .Property(a => a.Detalhe)
            .HasColumnType("json");

        GeradoNoBanco<Instituicao>(modelBuilder, i => i.CriadoEm);
        GeradoNoBanco<Curso>(modelBuilder, c => c.CriadoEm);
        GeradoNoBanco<Usuario>(modelBuilder, u => u.CriadoEm);
        GeradoNoBanco<Questao>(modelBuilder, q => q.CriadoEm);
        GeradoNoBanco<Justificativa>(modelBuilder, j => j.CriadoEm);
        GeradoNoBanco<Simulado>(modelBuilder, s => s.CriadoEm);
        GeradoNoBanco<Trilha>(modelBuilder, t => t.CriadaEm);
        GeradoNoBanco<ComentarioForum>(modelBuilder, c => c.CriadoEm);
        GeradoNoBanco<ForumReporte>(modelBuilder, r => r.CriadoEm);
        GeradoNoBanco<UsuarioBadge>(modelBuilder, b => b.ConcedidoEm);
        GeradoNoBanco<DocenteLancamento>(modelBuilder, l => l.CriadoEm);
        GeradoNoBanco<ChamadoTi>(modelBuilder, c => c.CriadoEm);
        GeradoNoBanco<AuditoriaRegistro>(modelBuilder, a => a.CriadoEm);

        modelBuilder.Entity<Usuario>().Property(u => u.AtualizadoEm).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<Questao>().Property(q => q.AtualizadoEm).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<Justificativa>().Property(j => j.AtualizadoEm).ValueGeneratedOnAddOrUpdate();
        modelBuilder.Entity<DashboardMetrica>().Property(m => m.AtualizadoEm).ValueGeneratedOnAddOrUpdate();

        foreach (var propriedade in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            if (propriedade.ClrType == typeof(decimal) || propriedade.ClrType == typeof(decimal?))
            {
                propriedade.SetPrecision(12);
                propriedade.SetScale(2);
            }
        }
    }

    private static void GeradoNoBanco<T>(ModelBuilder modelBuilder, System.Linq.Expressions.Expression<Func<T, DateTime>> propriedade)
        where T : class
    {
        modelBuilder.Entity<T>().Property(propriedade).ValueGeneratedOnAdd();
    }
}
