using UnityEngine;
using Ukubola.Combat;
using Ukubola.Enemies;

namespace Ukubola.Rendering
{
    public interface IEnemyPresentation
    {
        bool IsValid { get; }
        void Bind(Camera viewer);
        void Present(EnemyState state, bool moving, float time);
    }
    public interface IWeaponPresentation
    {
        void Present(WeaponDefinition definition, bool alive, bool firing, float speed, float elapsed);
    }
}
