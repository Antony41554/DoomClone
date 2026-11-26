using UnityEngine;
namespace Ukubola.Enemies
{
    public interface INoisePropagation
    {
        bool CanReach(Vector3 origin,Vector3 listener,float range);
    }
}
