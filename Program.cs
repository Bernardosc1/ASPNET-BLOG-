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
ConfigureAuthentication(builder); // Configura a autentica��o do aplicativo que serve para proteger as rotas do aplicativo
ConfigureMvc(builder); // Configura o MVC do aplicativo que serve para criar rotas e controladores
ConfigureServices(builder); // Configura os servi�os do aplicativo que servem para adicionar servi�os ao aplicativo


builder.Services.AddEndpointsApiExplorer(); // Adiciona o explorador de API
builder.Services.AddSwaggerGen(); // Adiciona o Swagger

var app = builder.Build(); // Cria uma inst�ncia do WebApplication
loadConfiguration(app); // Carrega a configura��o

app.UseHttpsRedirection(); // Redireciona para HTTPS
app.UseAuthentication(); // Habilita a autentica��o
app.UseAuthorization(); // Habilita a autoriza��o
app.MapControllers(); // Mapeia os controllers
app.UseStaticFiles(); // Habilita o uso de arquivos est�ticos
app.UseResponseCompression(); // Habilita a compress�o de resposta

if (app.Environment.IsDevelopment()) // Se o ambiente for de desenvolvimento
{
    app.UseSwagger(); // Habilita o Swagger
    app.UseSwaggerUI(); // Habilita o Swagger UI
}


app.Run();  // Inicia a aplica��o


void loadConfiguration(WebApplication app)
{
    Configuration.JwtKey = app.Configuration.GetValue<string>("JwtKey"); // L� o valor da se��o JwtKey
    Configuration.ApiKeyName = app.Configuration.GetValue<string>("ApiKeyName"); // L� o valor da se��o ApiKeyName
    Configuration.ApiKey = app.Configuration.GetValue<string>("ApiKey"); // L� o valor da se��o ApiKey

    var smtp = new Configuration.SmtpConfiguration(); // Cria uma nova inst�ncia de SmtpConfiguration
    app.Configuration.GetSection("Smtp").Bind(smtp); // Faz o bind da se��o Smtp
    Configuration.Smtp = smtp; // Define a configura��o de Smtp
}


void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var key = Encoding.ASCII.GetBytes(Configuration.JwtKey); // Converte a chave para bytes
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  // Define o esquema padr�o
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // Define o desafio padr�o
    }).AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters // Define os par�metros de valida��o do token
        {
            ValidateIssuerSigningKey = true, // Valida a chave do emissor
            IssuerSigningKey = new SymmetricSecurityKey(key), // Define a chave do emissor
            ValidateIssuer = false, // Valida o emissor
            ValidateAudience = false // Valida o p�blico
        };

    }); // Adiciona o JWT Bearer
}

void ConfigureMvc(WebApplicationBuilder builder)
{

    builder.Services.AddMemoryCache(); // Adiciona o cache em mem�ria

    builder.Services.AddResponseCompression(options =>
    {
        //options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>(); // Adiciona o provedor de compress�o Gzip
        //options.Providers.Add<CustomCompressionProvider>();

    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal; // Configura o n�vel de compress�o para �timo 
    });

    builder
    .Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true) // Desabilita a valida��o de modelo
    .AddJsonOptions(x => {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; //Ignora refer�ncias c�clicas (loop)
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault; // Ignora valores padr�o ao escrever JSON
        }); // Desabilita a formata��o de nomes de propriedades
}

void ConfigureServices(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); // L� a string de conex�o
    builder.Services.AddDbContext<BlogDataContext>(options => options.UseSqlServer(connectionString)); // Adiciona o contexto do banco de dados
    builder.Services.AddTransient<TokenService>(); // Sempre criar um novo Token
    builder.Services.AddTransient<EmailService>(); // Sempre criar um novo Email
    //builder.Services.AddScoped(); //Dura por requisi��o
    //builder.Services.AddSingleton(); // Singleton  -> 1 por App!
}