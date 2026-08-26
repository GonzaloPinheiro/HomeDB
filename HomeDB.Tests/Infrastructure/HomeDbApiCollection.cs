
namespace HomeDB.Tests.Infrastructure
{
    [CollectionDefinition("HomeDbApi")]
    public sealed class HomeDbApiCollection : ICollectionFixture<HomeDbApiFactory>
    {
        // Esta clase no necesita cuerpo. Su único propósito es aplicar
        // [CollectionDefinition] y asociar la interfaz ICollectionFixture<> para tener solo una instancia.
    }
}
