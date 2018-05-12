namespace Whalerator.Client
{
    public interface IDistributionFactory
    {
        IDistributionClient GetClient(string host, IAuthHandler handler);
    }
}