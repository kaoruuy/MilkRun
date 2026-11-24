using UnityEngine;
using static ToyArt;
using static ToyGarden;

public partial class MilkRun
{
    const float FortTourLength=1.9f, BossRevealLength=3.4f, StompTime=1.5f, StompRadius=4.35f;
    const int BossHealth=60, TownBossHealth=52;
    static readonly int[] FoeHP={5,8,9,14,14,17,6};
    static readonly int[] CastleDamage={3,4,5,7,0,6,4};
    static readonly float[] FoeSpeed={2.95f,2.55f,1.92f,1.65f,5.2f,1.43f,3.75f};
    Foe king;
    Transform fallenKing;
    float fortTour,bossReveal,stumble,waveMessage;
    float Remaining => Training?DefenseLength:phase==Phase.Wave?Mathf.Max(0,DefenseLength-elapsed):0;
    bool Directing => practiceTime>0 || fortTour>0 || shieldLessonTime>0 || bossReveal>0 || waveEntranceTime>0 || betrayalChild!=null || milkThiefIntro!=null;

    static string EnemyName(Foe f)=>f.CookieCommander?"クッキー将軍":f.boss?"王冠ピーマン":FoeNames[f.kind];
    static int EnemyDamage(Foe f)=>!f.boss?CastleDamage[f.kind]:f.CookieCommander?(f.enraged?11:9):(f.enraged?13:10);
    static string EntranceName(int side)=>side==0?"西の入口":"東の入口";

    void ShowCastles()
    {
        float t=FortTourLength-fortTour;
        float pan=Mathf.SmoothStep(0,1,t/FortTourLength);
        Vector3 middle=(Forts[0]+Forts[1])*.5f;
        float distance=Mathf.Max(23,34f/cam.aspect);
        Vector3 eye=middle+V(Mathf.Lerp(-1.4f,1.4f,pan),9.2f,-distance);
        Vector3 focus=middle+Vector3.up*1.65f;
        cam.fieldOfView=47;
        cam.transform.position=eye;
        cam.transform.LookAt(focus);
        ClearSightline(eye,focus,false);
    }

    void StartBoss()
    {
        phase=Phase.Boss;elapsed=0;bossReveal=BossRevealLength;noticeTime=0;
        int side=candyTown?1:0;
        king=Spawn(side,candyTown?CookieKind:2,true);
        Vector3 gate=Route(side,0),lane=Route(side,1);
        king.actor.position=Vector3.MoveTowards(gate,lane,2.2f);
        PointAt(king.actor,lane);
        Chime(196,.26f);
    }
    void ShowBoss()
    {
        float t=BossRevealLength-bossReveal;
        if(t<.88f)return; // Hold the player's view while the clock shows 0:00.
        Vector3 at=king.actor.position+Vector3.up*(king.CookieCommander?1.95f:1.58f);
        Vector3 eye=at+V(1.55f,.62f,Mathf.Lerp(-7.9f,-6.05f,Mathf.SmoothStep(0,1,(t-.88f)/2.45f)));
        cam.fieldOfView=46;cam.transform.position=eye;cam.transform.LookAt(at);
        ClearSightline(eye,at,false);
    }
    void AdvanceBossReveal(float dt)
    {
        float before=bossReveal;bossReveal=Mathf.Max(0,bossReveal-dt);
        if(before>BossRevealLength-.88f && bossReveal<=BossRevealLength-.88f)
        {
            Chime(147,.30f);Chime(294,.14f);
            SpeakOnApproach(king);
            CueEnemyArrival(king);
        }
        if(bossReveal>0)return;
        Vector3 toKing=king.actor.position-player.position;
        yaw=Mathf.Atan2(toKing.x,toKing.z)*Mathf.Rad2Deg;pitch=5;
        camFocus=player.position+V(0,1.62f+height*.72f,0);
        Say(EnemyName(king)+"が"+FortNames[king.side]+"へ進んでいる！");
    }

    void StrikeFoe(Foe f,bool byPlayer)
    {
        if(f.hp<=0)return;
        if(f.kind==MilkThiefKind)
        {
            HitMilkThief(f,byPlayer);
            return;
        }
        f.hitTime=.65f;
        if(f.guard>0)
        {
            if(!byPlayer){f.hitWord="ガード";return;}
            f.guard--;
            f.hitWord=f.guard>0?"盾にヒビ！":"盾が割れた！";
            DuckMusic(.24f);
            if(f.guard==0)
            {
                if(f!=sparring)lidsBroken++;
                ShowShieldBreak(f);
                f.lid.gameObject.SetActive(false);
            }
            else Chime(480,.17f);
            return;
        }
        int damage=byPlayer?PlayerDamage():2;f.hp=Mathf.Max(0,f.hp-damage);
        f.hitWord=byPlayer?"打撃  −"+damage:"ハート  −2";
        if(f.boss && !f.enraged && f.hp<=f.maxHp/2 && f.hp>0)
        {
            f.enraged=true;
            if(f.CookieCommander)
            {
                f.speed=2.72f;
                Say("クッキー将軍が加速！　赤い輪に気をつけよう");
            }
            else
            {
                f.guard=1;f.lid.gameObject.SetActive(true);f.speed=2.2f;
                Say("王冠ピーマンが本気に！　もう一度盾を割ろう");
            }
            f.stompWait=Mathf.Min(f.stompWait,2f);Chime(220,.21f);
        }
    }

    static Transform MakeStompRing(Transform parent)
    {
        var ring=Root("Stomp warning",parent,Vector3.up*.12f);
        const int count=30;
        for(int i=0;i<count;i++)
        {
            float a=i*2*Mathf.PI/count;
            var dash=Box(ring,Mathf.Sin(a)*StompRadius,.04f,Mathf.Cos(a)*StompRadius,
                .76f,.075f,.20f,i%3==0?White:"EA554C");
            dash.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
        }
        ring.gameObject.SetActive(false);return ring;
    }
    bool BossStomp(Foe f,float dt)
    {
        Vector3 delta=player.position-f.actor.position;
        float castleDistance=Vector3.Distance(f.actor.position,Forts[f.side]);
        if(f.stomp<=0)
        {
            if(f.ringFade>0)
            {
                f.ringFade=Mathf.Max(0,f.ringFade-dt);
                f.ring.localScale=Vector3.one*(1+.22f*(1-f.ringFade/.28f));
                if(f.ringFade<=0)f.ring.gameObject.SetActive(false);
            }
            f.stompWait-=dt;
            if(f.stompWait>0 || (delta.magnitude>StompRadius+1.3f && castleDistance>StompRadius))return false;
            f.stomp=StompTime;f.windup=0;f.ring.localScale=Vector3.one;f.ring.gameObject.SetActive(true);Chime(260,.14f);
            Speak(f,f.CookieCommander?"クッキー大ジャンプ！":"踏みつぶすぞ！");
        }
        f.stomp=Mathf.Max(0,f.stomp-dt);
        float t=1-f.stomp/StompTime;
        FoePose(f,0);
        f.body.localPosition=Vector3.up*(t<.62f?.06f:1.72f*Mathf.Sin(Mathf.PI*(t-.62f)/.38f));
        float crouch=t<.62f?.23f*Mathf.Sin(Mathf.PI*t/.62f):0;
        f.body.localScale=V(2.1f+crouch,2.1f-crouch*1.4f,2.1f+crouch);
        if(f.stomp>0)return true;
        DuckMusic(.32f);Chime(125,.30f);f.stompWait=f.enraged?4.1f:6.1f;f.ringFade=.28f;
        if(f.CookieCommander)Burst(f.actor.position+Vector3.up*.22f,9,false);
        if(castleDistance<=StompRadius)HitTower(f.side,EnemyDamage(f),EnemyName(f)+"の踏みつけ",false);
        if(delta.magnitude<=StompRadius && height<.62f)
        {
            Vector3 pushed=player.position+(delta.sqrMagnitude>.01f?delta.normalized:Vector3.back)*2.25f;
            Avoid(ref pushed,MilkPoint,1.12f);foreach(var fort in Forts)Avoid(ref pushed,fort,1.35f);
            player.position=V(Mathf.Clamp(pushed.x,-WalkX,WalkX),0,Mathf.Clamp(pushed.z,-WalkZ,WalkZ));
            if(petting!=null){petting.pet=0;petting=null;}
            stumble=.75f;Say("踏みつけ！　赤い輪はジャンプでよけよう");
        }
        return true;
    }
}
