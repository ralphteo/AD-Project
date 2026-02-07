namespace ADWebApplication.Services
{
    public interface ISecretProvider
    {
        string GetSecret(string name);
    }
}
