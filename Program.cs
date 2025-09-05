using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

#region Builder

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();
var key = builder.Configuration["Jwt"];
if(string.IsNullOrEmpty(key)) key = "EssaChaveDeveSerTrocadaEmProducao";

// Configura��o do sistema de autentica��o JWT

//Este m�todo adiciona os servi�os fundamentais de autentica��o ao container de inje��o de depend�ncia da aplica��o.
//� o primeiro e essencial passo para habilitar a autentica��o.
builder.Services.AddAuthentication(option => {

    //Esta linha define o esquema de autentica��o padr�o para o processo de "Autentica��o".
    //Isso significa que, quando a aplica��o precisar autenticar um usu�rio (por exemplo, ao acessar uma rota protegida),
    //ela usar� o esquema especificado aqui. No caso, est� sendo definido como JwtBearerDefaults.AuthenticationScheme,
    //que � o esquema de autentica��o baseado em tokens JWT (JSON Web Tokens). Esse esquema � amplamente utilizado para autentica��o em APIs,
    //onde o cliente envia um token JWT no cabe�alho da requisi��o para provar sua identidade.
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

    //Esta linha define o esquema de autentica��o padr�o para o processo de "Desafio" (Challenge).
    //O desafio ocorre quando um usu�rio tenta acessar um recurso protegido sem estar autenticado.
    //Ao definir o DefaultChallengeScheme como JwtBearerDefaults.AuthenticationScheme, a aplica��o est� especificando que,
    //quando um usu�rio n�o autenticado tenta acessar um recurso protegido, o sistema de autentica��o deve usar o esquema JWT Bearer
    //para desafiar o usu�rio a fornecer um token JWT v�lido para autentica��o.
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;


    //O m�todo .AddJwtBearer() configura o esquema de autentica��o "Bearer" que foi definido como padr�o no AddAuthentication.
    //Dentro dele, voc� cria um objeto TokenValidationParameters que age como a "lista de verifica��es" para cada token que chega.
}).AddJwtBearer(option =>
{
    //Esses par�metros dizem ao sistema como validar os tokens JWT recebidos.
    option.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateLifetime = true,
        ValidateAudience = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<Contexto>
    (
         options => options.UseMySql(builder.Configuration.GetConnectionString("mysql"),
            ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql")))
    );

#endregion

#region App

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    options.SwaggerEndpoint("/openapi/v1.json", "Veiculos API"));
}

#endregion

#region Home

app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");

#endregion

#region Administradores

app.MapPost("/administradores/cadastrar", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
{
    var validacao = new ErrosDeValidacao
    {
        Mensagens = new List<string>()
    };

    var administrador = ValidaAdministrador(validacao, administradorDTO);

    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);
    else
    {
        administradorServico.Cadastrar(administrador);
    }

    return Results.Created($"/administradores/{administrador.Id}", administrador);

}).RequireAuthorization().WithTags("Adminstrador");

Administrador ValidaAdministrador(ErrosDeValidacao validacao, AdministradorDTO administradorDTO)
{
    var administrador = new Administrador();

    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("O email do administrador n�o pode ser vazio");
    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("A senha do administrador n�o pode ser vazia");
    if (administradorDTO.Perfil == 0)
        validacao.Mensagens.Add("O perfil do administrador deve ser informado");

    if (validacao.Mensagens.Count == 0)
    {
        administrador.Email = administradorDTO.Email;
        administrador.Senha = administradorDTO.Senha;
        administrador.Perfil = administradorDTO.Perfil.ToString();
    }

    return administrador;
}

app.MapPost("/administradores/todos", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
{
    var administradores = administradorServico.Todos(pagina);
    return Results.Ok(administradores);

}).RequireAuthorization().WithTags("Adminstrador");

string gerarTokenJwt(Administrador administrador)
{
    if(!string.IsNullOrEmpty(key))
    {
        var secutiryKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(secutiryKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Email, administrador.Email),
            new Claim("Perfil", administrador.Perfil)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    return string.Empty;
}

app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
    var admin = administradorServico.Login(loginDTO);
    if (admin != null)
    {
        string token = gerarTokenJwt(admin);
        return Results.Ok(new AdminLogadoModelView 
        {
            Id = admin.Id,
            Email = admin.Email,
            Token = token
        });
    }
    else
    {
        return Results.Unauthorized();
    }
}).WithTags("Adminstrador");


app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
{
    var admin = administradorServico.BuscaPorId(id);
    if (admin != null)
    {
        return Results.Ok(admin);
    }
    else
    {
        return Results.NotFound();
    }

}).RequireAuthorization().WithTags("Adminstrador");

app.MapPut("/administradores/{id}", ([FromRoute] int id, AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>
{
    var admin = administradorServico.BuscaPorId(id);
    if (admin != null)
    {
        admin.Senha = administradorDTO.Senha;
        admin.Email = administradorDTO.Email;
        admin.Perfil = administradorDTO.Perfil.ToString();

        administradorServico.Atualizar(admin);

        return Results.Ok(admin);
    }
    else
    {
        return Results.NotFound();
    }
}).RequireAuthorization().WithTags("Adminstrador");

#endregion

#region Veiculos

app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    var mensagens = new ErrosDeValidacao
    {
        Mensagens = new List<string>()
    };

    ValidaModelo(mensagens, veiculoDTO);

    if (mensagens.Mensagens.Count > 0)
        return Results.BadRequest(mensagens);

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };
    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculos/{veiculo.Id}", veiculo);

}).RequireAuthorization().WithTags("Veiculo");

void ValidaModelo(ErrosDeValidacao mensagens, VeiculoDTO veiculoDTO)
{
    if (string.IsNullOrEmpty(veiculoDTO.Nome))
    {
        mensagens.Mensagens.Add("O nome do ve�culo n�o pode ser vazio");
    }
    else if (string.IsNullOrEmpty(veiculoDTO.Marca))
    {
        mensagens.Mensagens.Add("O nome da marca n�o pode ser vazio");
    }
    else if (veiculoDTO.Ano == 0)
    {
        mensagens.Mensagens.Add("Informar o ano do veiculo");
    }
}

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
{
    var veiculos = veiculoServico.Todos(pagina);
    return Results.Ok(veiculos);
}).RequireAuthorization().WithTags("Veiculo");


app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo != null)
    {
        return Results.Ok(veiculo);
    }
    else
    {
        return Results.NotFound();
    }

}).RequireAuthorization().WithTags("Veiculo");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo != null)
    {
        veiculo.Nome = veiculoDTO.Nome;
        veiculo.Marca = veiculoDTO.Marca;
        veiculo.Ano = veiculoDTO.Ano;
        veiculoServico.Atualizar(id, veiculo);
        return Results.Ok(veiculo);
    }
    else
    {
        return Results.NotFound();
    }
}).RequireAuthorization().WithTags("Ve�culo");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo != null)
    {
        veiculoServico.Apagar(veiculo);

        return Results.Ok("Ve�culo deletado com sucesso");
    }
    else
    {
        return Results.NotFound();
    }
}).RequireAuthorization().WithTags("Ve�culo");

#endregion

app.UseAuthentication();
app.UseAuthorization();

app.Run();