namespace Whalerator.WebAPI
{
    public interface IRegistryFactory
    {
        IRegistry GetRegistry(RegistryCredentials credentials);
    }
}