using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Entidades;

namespace minimal_api.Infraestrutura.Db
{
    public class Contexto : DbContext
    {
        private readonly IConfiguration _configuracaoAppSettings;

        public Contexto(IConfiguration configuracaoAppSettings)
        {
            _configuracaoAppSettings = configuracaoAppSettings;
        }

        public DbSet<Administrador> Administradores { get; set; } = default!;
        public DbSet<Veiculo> Veiculos { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Administrador>().HasData(
                    new Administrador {
                        Id = 1,
                        Email = "administrador@teste.com",
                        Senha = "123456",
                        Perfil = "Adm"
                    }
                );

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Se a configuração já não estiver feita, configura a conexão com o banco de dados MySQL
            if (!optionsBuilder.IsConfigured)
            {
                var stringConexao = _configuracaoAppSettings.GetConnectionString("mysql")?.ToString();

                if (!string.IsNullOrEmpty(stringConexao))
                {
                    optionsBuilder.UseMySql
                    (
                      stringConexao,
                      ServerVersion.AutoDetect(stringConexao)
                    );
                }
            }
        }
    }
}
