using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ukubola.Combat;
using Ukubola.Player;
using Ukubola.World;
using Object=UnityEngine.Object;

namespace Ukubola.EditorTools
{
    public static class UkubolaPickupTests
    {
        [MenuItem("Tools/UKUBOLA/Phase 07/Run Pickup Self-Tests",false,83)]
        public static void Run()
        {
            if(!UkubolaPickupSetup.CanRun())return;
            int count=0;var cleanup=new List<Object>();
            void Check(bool result,string description){if(!result)throw new InvalidOperationException(description);count++;}
            PickupDefinition Def(PickupKind kind,int amount=0,AmmoType ammo=AmmoType.Bullets,ArmorKind armor=ArmorKind.None,WeaponId weapon=WeaponId.Fist,KeyId key=KeyId.None)
            {var d=ScriptableObject.CreateInstance<PickupDefinition>();d.hideFlags=HideFlags.HideAndDontSave;cleanup.Add(d);d.Configure("Test",kind,null,amount,ammo,armor,weapon,key);return d;}
            try
            {
                var go=EditorUtility.CreateGameObjectWithHideFlags("Pickup test actor",HideFlags.HideAndDontSave,typeof(Armor),typeof(Health),typeof(PlayerInventory));cleanup.Add(go);
                var hp=go.GetComponent<Health>();var armor=go.GetComponent<Armor>();var bag=go.GetComponent<PlayerInventory>();hp.Configure(100,armor);
                bool Apply(PickupDefinition d)=>PickupEffect.TryApply(d,hp,bag);
                var med=Def(PickupKind.Health,25);var green=Def(PickupKind.Armor,armor:ArmorKind.Green);var blue=Def(PickupKind.Armor,armor:ArmorKind.Blue);
                Check(!Apply(med)&&hp.Current==100,"Full health should reject medikit");hp.TakeDamage(new DamageInfo(40));Check(Apply(med)&&hp.Current==85,"Medikit heal");Check(Apply(med)&&hp.Current==100,"Health cap");Check(!Apply(med),"No effect at cap");
                Check(Apply(green)&&armor.Points==100,"Green armor");Check(!Apply(green),"Equal armor should remain");Check(Apply(blue)&&armor.Points==200,"Blue armor");Check(!Apply(green)&&armor.Kind==ArmorKind.Blue,"No armor downgrade");
                hp.TakeDamage(new DamageInfo(1000));Check(hp.IsDead&&!Apply(med)&&!Apply(green),"Dead actor rejects pickups");hp.ResetState();bag.ResetState();
                foreach(AmmoType type in Enum.GetValues(typeof(AmmoType)))
                {
                    var d=Def(PickupKind.Ammo,10,type);int cap=bag.Capacity(type);bag.AddAmmo(type,cap);Check(!Apply(d)&&bag.Count(type)==cap,"Full ammo "+type);
                    Check(bag.TryConsumeAmmo(type,3),"Consume "+type);Check(Apply(d)&&bag.Count(type)==cap,"Partial ammo grant "+type);
                }
                bag.ResetState();var gun=Def(PickupKind.Weapon,8,AmmoType.Shells,weapon:WeaponId.Shotgun);
                Check(!bag.HasWeapon(WeaponId.Shotgun),"No initial shotgun");Check(Apply(gun)&&bag.HasWeapon(WeaponId.Shotgun)&&bag.Count(AmmoType.Shells)==8,"Grant shotgun and ammo");Check(Apply(gun)&&bag.Count(AmmoType.Shells)==16,"Duplicate shotgun grants ammo");bag.AddAmmo(AmmoType.Shells,100);Check(!Apply(gun),"Duplicate gun and full ammo");
                foreach(KeyId key in Enum.GetValues(typeof(KeyId)))if(key!=KeyId.None)
                {var d=Def(PickupKind.Key,key:key);Check(Apply(d)&&bag.HasKeys(key),"Grant key "+key);Check(!Apply(d),"Duplicate key "+key);}
                Check(!Apply(Def(PickupKind.Key,key:KeyId.BlueCard|KeyId.RedCard)),"Reject combined key definition");
                bag.ResetState();var pack=Def(PickupKind.Backpack);
                Check(Apply(pack)&&bag.HasBackpack,"First backpack");Check(bag.Count(AmmoType.Bullets)==60&&bag.Count(AmmoType.Shells)==4&&bag.Count(AmmoType.Rockets)==1&&bag.Count(AmmoType.Cells)==20,"Backpack ammo");
                foreach(AmmoType type in Enum.GetValues(typeof(AmmoType)))Check(bag.Capacity(type)==2*PlayerInventory.BaseCapacity(type),"Backpack capacity "+type);
                Check(Apply(pack)&&bag.Count(AmmoType.Bullets)==70,"Second backpack grants ammo");foreach(AmmoType type in Enum.GetValues(typeof(AmmoType)))bag.AddAmmo(type,bag.Capacity(type));Check(!Apply(pack),"Full second backpack rejected");
                Check(!Apply(Def(PickupKind.Ammo,0)),"Invalid amount");Check(!PickupEffect.TryApply(med,hp,null),"Missing inventory");
                var texture=new Texture2D(2,2);texture.hideFlags=HideFlags.HideAndDontSave;cleanup.Add(texture);var sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f));sprite.hideFlags=HideFlags.HideAndDontSave;cleanup.Add(sprite);
                var dAmmo=Def(PickupKind.Ammo,10);dAmmo.Configure("Test ammo",PickupKind.Ammo,sprite,10);
                var itemGo=EditorUtility.CreateGameObjectWithHideFlags("Pickup test item",HideFlags.HideAndDontSave,typeof(SphereCollider),typeof(Rigidbody),typeof(Pickup),typeof(SpriteRenderer));cleanup.Add(itemGo);
                var item=itemGo.GetComponent<Pickup>();var visual=itemGo.GetComponent<SpriteRenderer>();visual.sprite=sprite;item.Configure(dAmmo,visual);itemGo.GetComponent<SphereCollider>().isTrigger=true;itemGo.GetComponent<Rigidbody>().isKinematic=true;itemGo.GetComponent<Rigidbody>().useGravity=false;
                bag.ResetState();int events=0;item.Taken+=_=>events++;
                Check(item.TryCollect(hp,bag)&&item.Collected&&bag.Count(AmmoType.Bullets)==60&&events==1&&!visual.enabled&&!itemGo.GetComponent<SphereCollider>().enabled,"Pickup consumption and event");
                Check(!item.TryCollect(hp,bag)&&events==1&&bag.Count(AmmoType.Bullets)==60,"No double collection");item.ResetPickup();Check(!item.Collected&&visual.enabled&&itemGo.GetComponent<SphereCollider>().enabled,"Reset pickup");Check(item.TryCollect(hp,bag)&&events==2&&bag.Count(AmmoType.Bullets)==70,"Collect after reset");
                item.ResetPickup();bag.AddAmmo(AmmoType.Bullets,1000);Check(!item.TryCollect(hp,bag)&&!item.Collected&&visual.enabled&&itemGo.GetComponent<SphereCollider>().enabled,"Full item stays visible");
                Debug.Log("[UKUBOLA P07] PICKUP SELF-TESTS PASSED | "+count+" checks.");
            }
            catch(Exception ex){Debug.LogError("[UKUBOLA P07] SELF-TEST FAILED after "+count+" checks: "+ex.Message+"\n"+ex);}
            finally{for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i])Object.DestroyImmediate(cleanup[i]);}
        }
    }
}
