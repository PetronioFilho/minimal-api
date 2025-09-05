using System.ComponentModel.DataAnnotations;

namespace minimal_api.Dominio.ModelViews
{
    public record AdminLogadoModelView
    {
        public int Id { get; set; } = default!;

        public string Email { get; set; } = default!;
        
        public string Token { get; set; } = default!;
    }
}
