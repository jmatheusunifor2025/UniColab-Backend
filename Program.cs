using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UniColab.Api.Data;
using UniColab.Api.Erros;
using UniColab.Api.Repositories;
using UniColab.Api.Seguranca;
using UniColab.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opcoes =>
{
    opcoes.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    opcoes.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UniColab",
        Version = "v1",
        Description = "API do acervo, simulados e relatórios da instituição."
    });
    var esquema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", esquema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [esquema] = Array.Empty<string>() });
});

var conexao = builder.Configuration.GetConnectionString("Unicolab")
    ?? throw new InvalidOperationException("Connection string Unicolab ausente.");
builder.Services.AddDbContext<UnicolabDbContext>(opcoes =>
    opcoes.UseMySql(conexao, new MySqlServerVersion(new Version(8, 4, 11)))
        .UseSnakeCaseNamingConvention());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.AddSingleton<JwtEmissor>();
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IAcervoRepositorio, AcervoRepositorio>();
builder.Services.AddScoped<IAvaliacaoRepositorio, AvaliacaoRepositorio>();
builder.Services.AddScoped<IProcedimentoRepositorio, ProcedimentoRepositorio>();
builder.Services.AddScoped<IComunidadeRepositorio, ComunidadeRepositorio>();
builder.Services.AddScoped<IPainelRepositorio, PainelRepositorio>();
builder.Services.AddScoped<IAuthServico, AuthServico>();
builder.Services.AddScoped<IUsuarioServico, UsuarioServico>();
builder.Services.AddScoped<IQuestaoServico, QuestaoServico>();
builder.Services.AddScoped<ISimuladoServico, SimuladoServico>();
builder.Services.AddScoped<ITentativaServico, TentativaServico>();
builder.Services.AddScoped<IForumServico, ForumServico>();
builder.Services.AddScoped<IPainelServico, PainelServico>();
builder.Services.AddScoped<TratamentoDeErros>();
builder.Services.AddHostedService<ExpiracaoDeTentativas>();

var chave = builder.Configuration["Jwt:Key"] ?? "";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.MapInboundClaims = true;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization(opcoes =>
{
    opcoes.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(opcoes =>
{
    opcoes.AddPolicy("Frontend", politica =>
        politica.WithOrigins(origens).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<TratamentoDeErros>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
