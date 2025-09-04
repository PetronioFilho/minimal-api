using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;

namespace minimal_api.Dominio.Interfaces
{
    public interface IAdministradorServico
    {
        Administrador Login(LoginDTO loginDTO);

        Administrador Cadastrar(Administrador administrador);

        Administrador BuscaPorId(int id);

        List<Administrador> Todos(int? pagina);

        Administrador Atualizar(Administrador administrador);
    }
}
