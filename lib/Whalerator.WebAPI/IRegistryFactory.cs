namespace Whalerator.WebAPI
{
    public interface IRegistryFactory
    {
        IRegistry GetRegistry(string name, string username, string password);
    }
}