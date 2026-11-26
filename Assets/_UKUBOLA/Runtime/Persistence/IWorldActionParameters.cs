using Ukubola.World;
namespace Ukubola.Persistence
{
    public interface IWorldActionParameters
    {
        void ValidateAction(WorldVerb verb,float value);
    }
}
