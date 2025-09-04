namespace minimal_api.Dominio.ModelViews
{
    public struct ErrosDeValidacao
    {
        public ErrosDeValidacao()
        {
                
        }
        public List<string> Mensagens { get; set; } = default!;
    }
}
