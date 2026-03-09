namespace Pkmvp.Api.Auth
{
    public interface ICurrentUserAccessor
    {
        CurrentUser Get();
    }
}