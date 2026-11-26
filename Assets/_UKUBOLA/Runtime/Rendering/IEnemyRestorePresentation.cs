using Ukubola.Enemies;
namespace Ukubola.Rendering
{
    public interface IEnemyRestorePresentation
    {
        void RestoreState(EnemyState state,float phaseRemaining,float now);
    }
}
