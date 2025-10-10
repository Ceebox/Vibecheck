namespace Vibecheck.Utility;

public interface IFetcher<T>
{
    public Task<T> Fetch();
}
