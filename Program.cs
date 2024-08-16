using Blog;
using Blog.Data;
using Blog.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args); // Cria um novo WebApplicationBuilder
ConfigureAuthentication(builder); // Configura a autenticação do aplicativo que serve para proteger as rotas do aplicativo
ConfigureMvc(builder); // Configura o MVC do aplicativo que serve para criar rotas e controladores
ConfigureServices(builder); // Configura os serviços do aplicativo que servem para adicionar serviços ao aplicativo


builder.Services.AddEndpointsApiExplorer(); // Adiciona o explorador de API
builder.Services.AddSwaggerGen(); // Adiciona o Swagger

var app = builder.Build(); // Cria uma instância do WebApplication
loadConfiguration(app); // Carrega a configuração

app.UseHttpsRedirection(); // Redireciona para HTTPS
app.UseAuthentication(); // Habilita a autenticação
app.UseAuthorization(); // Habilita a autorização
app.MapControllers(); // Mapeia os controllers
app.UseStaticFiles(); // Habilita o uso de arquivos estáticos
app.UseResponseCompression(); // Habilita a compressão de resposta

if (app.Environment.IsDevelopment()) // Se o ambiente for de desenvolvimento
{
    app.UseSwagger(); // Habilita o Swagger
    app.UseSwaggerUI(); // Habilita o Swagger UI
}


app.Run();  // Inicia a aplicação


void loadConfiguration(WebApplication app)
{
    Configuration.JwtKey = app.Configuration.GetValue<string>("JwtKey"); // Lê o valor da seção JwtKey
    Configuration.ApiKeyName = app.Configuration.GetValue<string>("ApiKeyName"); // Lê o valor da seção ApiKeyName
    Configuration.ApiKey = app.Configuration.GetValue<string>("ApiKey"); // Lê o valor da seção ApiKey

    var smtp = new Configuration.SmtpConfiguration(); // Cria uma nova instância de SmtpConfiguration
    app.Configuration.GetSection("Smtp").Bind(smtp); // Faz o bind da seção Smtp
    Configuration.Smtp = smtp; // Define a configuração de Smtp
}


void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var key = Encoding.ASCII.GetBytes(Configuration.JwtKey); // Converte a chave para bytes
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  // Define o esquema padrão
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Define o desafio padrão
    }).AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters // Define os parâmetros de validação do token
        {
            ValidateIssuerSigningKey = true, // Valida a chave do emissor
            IssuerSigningKey = new SymmetricSecurityKey(key), // Define a chave do emissor
            ValidateIssuer = false, // Valida o emissor
            ValidateAudience = false // Valida o público
        };

    }); // Adiciona o JWT Bearer
}

void ConfigureMvc(WebApplicationBuilder builder)
{

    builder.Services.AddMemoryCache(); // Adiciona o cache em memória

    builder.Services.AddResponseCompression(options =>
    {
        //options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>(); // Adiciona o provedor de compressão Gzip
        //options.Providers.Add<CustomCompressionProvider>();

    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal; // Configura o nível de compressão para ótimo 
    });

    builder
    .Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true) // Desabilita a validação de modelo
    .AddJsonOptions(x => {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; //Ignora referências cíclicas (loop)
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault; // Ignora valores padrão ao escrever JSON
        }); // Desabilita a formatação de nomes de propriedades
}

void ConfigureServices(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); // Lê a string de conexão
    builder.Services.AddDbContext<BlogDataContext>(options => options.UseSqlServer(connectionString)); // Adiciona o contexto do banco de dados
    builder.Services.AddTransient<TokenService>(); // Sempre criar um novo Token
    builder.Services.AddTransient<EmailService>(); // Sempre criar um novo Email
    //builder.Services.AddScoped(); //Dura por requisição
    //builder.Services.AddSingleton(); // Singleton  -> 1 por App!
}