using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Ukubola.Persistence;

namespace Ukubola.World
{
    // Activation changes the original declaration to "sealed partial class".
    public sealed partial class MissionContext
    {
        private void ApplyRestorePass(EntitySnapshot[] data,bool actors)
        {
            foreach(var state in data.Where(s=>(entities[s.id].RestoreOrder>=200)==actors).OrderBy(s=>entities[s.id].RestoreOrder).ThenBy(s=>s.id,StringComparer.Ordinal))entities[state.id].RestoreEntity(state);
        }
        private void FinishEntityRestore()
        {
            foreach(var entity in entities.Values.OrderBy(e=>e.RestoreOrder).ThenBy(e=>e.PersistentId,StringComparer.Ordinal))
                if(entity is IEntityRestoreComplete finish)finish.FinishRestore();
        }
        public IEnumerator RestoreEntitiesRoutine(EntitySnapshot[] data,Action<Exception> completed)
        {
            EntitySnapshot[] previous=null;Exception problem=null;
            try{ValidateEntities(data);previous=CaptureEntities();}
            catch(Exception error){problem=error;}
            if(problem!=null){completed?.Invoke(problem);yield break;}
            Restoring=true;
            try
            {
                try{ApplyRestorePass(data,false);Physics.SyncTransforms();}
                catch(Exception error){problem=error;}
                if(problem==null)
                {
                    // Carving and navigation update after moving doors/platforms, before agents bind.
                    yield return null;yield return null;
                    try{ApplyRestorePass(data,true);FinishEntityRestore();}
                    catch(Exception error){problem=error;}
                }
                if(problem!=null)
                {
                    try{ApplyRestorePass(previous,false);Physics.SyncTransforms();}
                    catch(Exception error){problem=new AggregateException("World restore and rollback failed.",problem,error);}
                    yield return null;yield return null;
                    try{ApplyRestorePass(previous,true);FinishEntityRestore();}
                    catch(Exception error){problem=new AggregateException("Actor restore and rollback failed.",problem,error);}
                }
            }
            finally{Restoring=false;}
            completed?.Invoke(problem);
        }
    }
}
