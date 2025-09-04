using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.Dominio.ModelViews;
using minimal_api.Dominio.Servicos;
using minimal_api.Infraestrutura.Db;

#region Builder

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();


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

}).WithTags("Adminstrador");

Administrador ValidaAdministrador(ErrosDeValidacao validacao, AdministradorDTO administradorDTO)
{
    var administrador = new Administrador();

    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("O email do administrador não pode ser vazio");
    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("A senha do administrador não pode ser vazia");
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

}).WithTags("Adminstrador");


app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
{
    if (administradorServico.Login(loginDTO) != null)
    {
        return Results.Ok("Login com sucesso");
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

}).WithTags("Adminstrador");

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
}).WithTags("Veículo");

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

}).WithTags("Veiculo");

void ValidaModelo(ErrosDeValidacao mensagens, VeiculoDTO veiculoDTO)
{
    if (string.IsNullOrEmpty(veiculoDTO.Nome))
    {
        mensagens.Mensagens.Add("O nome do veículo não pode ser vazio");
    }
    else if (string.IsNullOrEmpty(veiculoDTO.Marca))
    {
        mensagens.Mensagens.Add("O nome da marca não pode ser vazio");
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
}).WithTags("Veiculo");


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

}).WithTags("Veiculo");

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
}).WithTags("Veículo");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
{
    var veiculo = veiculoServico.BuscaPorId(id);
    if (veiculo != null)
    {
        veiculoServico.Apagar(veiculo);

        return Results.Ok("Veículo deletado com sucesso");
    }
    else
    {
        return Results.NotFound();
    }
}).WithTags("Veículo");

#endregion

app.Run();