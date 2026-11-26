using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ukubola.Enemies;

namespace Ukubola.World
{
    [Serializable] public sealed class AcousticLink
    {
        public string portal;
        public WorldMover door;
        public bool dampened;
    }
    public sealed class MissionAcoustics:MonoBehaviour,INoisePropagation
    {
        [SerializeField] private MissionContext context;
        [SerializeField] private AcousticLink[] links=Array.Empty<AcousticLink>();
        private MissionDefinition Mission=>context.Definition;
        public void Configure(MissionContext world,AcousticLink[] connections)
        {context=world;links=connections;ValidateConfiguration();}
        private void Awake()=>ValidateConfiguration();
        public void ValidateConfiguration()
        {
            if(!context||!Mission||links==null||links.Any(l=>l==null||!Mission.portals.Any(p=>p.key==l.portal)||l.door&&l.door.gameObject.scene!=gameObject.scene)||links.Select(l=>l.portal).Distinct(StringComparer.Ordinal).Count()!=links.Length)
                throw new InvalidOperationException("Invalid acoustic portal connections.");
        }
        private MissionSector Sector(Vector3 point)=>Mission.sectors.Where(s=>s.Contains(point,.1f)).OrderByDescending(s=>s.floor).FirstOrDefault();
        public bool CanReach(Vector3 origin,Vector3 listener,float range)
        {
            if(!MissionDefinition.Finite(origin)||!MissionDefinition.Finite(listener)||!MissionDefinition.Finite(range)||range<0)return false;
            if(Vector3.Distance(origin,listener)>range)return false;
            var start=Sector(origin);var goal=Sector(listener);if(start==null||goal==null)return false;
            if(start.key==goal.key)return true;
            // Nodes are entered sectors and the portal from which sound entered them.
            // Distances through portal positions preserve useful limits around rings.
            var nodes=new List<(string sector,Vector3 point,float cost)>{(start.key,origin,0)};
            var best=new Dictionary<string,float>(StringComparer.Ordinal);
            while(nodes.Count>0)
            {
                int index=0;for(int i=1;i<nodes.Count;i++)if(nodes[i].cost<nodes[index].cost)index=i;
                var current=nodes[index];nodes.RemoveAt(index);
                if(current.cost>range)continue;
                if(current.sector==goal.key&&current.cost+Vector3.Distance(current.point,listener)<=range)return true;
                foreach(var portal in Mission.portals.Where(p=>p.from==current.sector||p.to==current.sector))
                {
                    var link=links.FirstOrDefault(l=>l.portal==portal.key);
                    if(link!=null&&link.door&&!link.door.AtFinalStop)continue;
                    string destination=portal.from==current.sector?portal.to:portal.from;
                    float cost=current.cost+Vector3.Distance(current.point,portal.position)*(link!=null&&link.dampened?2:1);
                    string key=portal.key+":"+destination;
                    if(cost>range||best.TryGetValue(key,out float previous)&&previous<=cost)continue;
                    best[key]=cost;nodes.Add((destination,portal.position,cost));
                }
            }
            return false;
        }
    }
}
