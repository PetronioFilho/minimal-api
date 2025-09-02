using minimal_api.Dominio.DTOs;
using minimal_api.Dominio.Entidades;
using minimal_api.Dominio.Interfaces;
using minimal_api.Infraestrutura.Db;

namespace minimal_api.Dominio.Servicos
{
    public class AdministradorServico : IAdministradorServico
    {
        private readonly Contexto _contexto;
        public AdministradorServico(Contexto contexto)
        {
             _contexto = contexto;
        }
        public Administrador Login(LoginDTO loginDTO)
        {
            var admin = _contexto.Administradores.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
            return admin!;
        }
    }
}
