using System.Collections.Generic;
using UnityEngine;
using static ToyArt;
using static ToyGarden;

public partial class MilkRun
{
    const int BroccoliKind=5, CookieKind=6;
    const float ArrivalLength=3.8f;
    const float MilkThiefIntroLength=6.4f, MilkThiefTargetShotAt=4.2f;
    const float BetrayalLength=3.8f, BetrayalAt=1.06f;
    const int PortraitLayer=30;
    static readonly string[] ArrivalHints={
        "足が速い。おうちに着く前に止めよう。",
        "おうちに近づくと、チクッと突進する。",
        "盾がハートを弾く。哺乳瓶を置いて F で割ろう。",
        "動きはゆっくり。おうちへの一撃は重い。",
        "ミルク屋台がねらい。盗まれたら F で捕まえよう！",
        "押し返しに強い。赤ちゃんのハートと一緒に止めよう。",
        "転がってすばやく迫る。先回りして止めよう。"
    };
    static readonly string[] ArrivalColors={"F5C66D","B5D6E8","AEE0AC","D6BCD9","F5A0AC","8EDF9C","F8C078"};
    readonly HashSet<int> seenArrivals=new HashSet<int>();
    readonly Queue<Foe> queuedArrivals=new Queue<Foe>();
    readonly Dictionary<int,RenderTexture> portraits=new Dictionary<int,RenderTexture>();
    Camera portraitCamera;
    Foe featuredEnemy;
    float arrivalTime;
    Foe milkThiefIntro;
    float milkThiefIntroTime;
    Child betrayalChild;
    float betrayalTime;
    bool betrayalLessonSeen;

    void ResetEncounterLessons()
    {
        seenArrivals.Clear();
        betrayalLessonSeen=false;
    }

    void ResetEncounterScenes()
    {
        queuedArrivals.Clear();
        featuredEnemy=null;
        arrivalTime=0;
        milkThiefIntro=null;
        milkThiefIntroTime=0;
        betrayalChild=null;
        betrayalTime=0;
    }

    static int PortraitKey(Foe foe)=>foe.boss?-1-foe.kind:foe.kind;

    void CueEnemyArrival(Foe foe)
    {
        if(!seenArrivals.Add(PortraitKey(foe)))return;
        if(candyTown && foe.kind==MilkThiefKind)
        {
            milkThiefIntro=foe;
            milkThiefIntroTime=0;
            PointAt(foe.actor,Route(foe.side,1));
            DuckMusic(MilkThiefIntroLength+.1f);
            return;
        }
        queuedArrivals.Enqueue(foe);
    }

    void AdvanceMilkThiefIntro(float dt)
    {
        float before=milkThiefIntroTime;
        milkThiefIntroTime+=dt;
        milkThiefIntro.step+=dt*5;
        FoePose(milkThiefIntro,0);
        if(before<.24f && milkThiefIntroTime>=.24f)Chime(415,.20f);
        if(before<MilkThiefTargetShotAt && milkThiefIntroTime>=MilkThiefTargetShotAt)Chime(622,.14f);
        if(milkThiefIntroTime<MilkThiefIntroLength)return;
        milkThiefIntro=null;
        milkThiefIntroTime=0;
        MoveCamera(true);
    }

    void ShowMilkThiefIntro()
    {
        Vector3 eye,focus;
        if(milkThiefIntroTime<MilkThiefTargetShotAt)
        {
            Transform thief=milkThiefIntro.actor;
            float zoom=Mathf.SmoothStep(0,1,milkThiefIntroTime/1.25f);
            focus=thief.position+Vector3.up*Mathf.Lerp(1.02f,1.18f,zoom);
            eye=thief.position+thief.forward*Mathf.Lerp(5.65f,2.75f,zoom)
                +thief.right*Mathf.Lerp(.78f,.49f,zoom)+Vector3.up*Mathf.Lerp(2.07f,1.53f,zoom);
            cam.fieldOfView=Mathf.Lerp(39,34,zoom);
        }
        else
        {
            float zoom=Mathf.SmoothStep(0,1,(milkThiefIntroTime-MilkThiefTargetShotAt)/1.55f);
            focus=MilkPoint+Vector3.up*1.46f;
            eye=MilkPoint+V(Mathf.Lerp(3.3f,2.65f,zoom),Mathf.Lerp(3.85f,3.25f,zoom),Mathf.Lerp(-8.1f,-6.85f,zoom));
            cam.fieldOfView=39;
        }
        cam.transform.position=eye;
        cam.transform.LookAt(focus);
        ClearSightline(eye,focus,false);
    }

    void MilkThiefIntroCaption()
    {
        if(milkThiefIntroTime>=MilkThiefTargetShotAt)
            Tag(MilkPoint+Vector3.up*3.38f,"ねらわれている補給所",Gold,true);
        const float width=744,height=161;
        float x=(ww-width)*.5f,y=hh-height-27;
        Plate(x+4,y+5,width,height,new Color(.12f,.16f,.22f,.34f),17);
        Plate(x,y,width,height,Navy,17);
        Plate(x+13,y+17,6,height-34,Pink,3);
        Caption(x+32,y+10,width-64,23,"2面　お菓子の町　／　"+EntranceName(milkThiefIntro.side),15,Pink,true);
        Caption(x+31,y+34,width-62,46,"ミルクどろぼうが現れた！",34,Paper,true);
        Caption(x+33,y+83,width-66,25,"ねらいは補給所。盗まれると、ミルクを受け取れなくなるよ。",18,Paper);
        Caption(x+33,y+116,width-66,26,"哺乳瓶を置いて近づき、F または左クリックで攻撃して取り返そう！",17,Gold,true);
        Plate(x+width-173,y+15,145,23,Dark,7);
        Caption(x+width-173,y+15,145,23,"マップの「盗」が目印",12,Paper,true,TextAnchor.MiddleCenter);
    }

    void AdvanceEnemyArrival(float dt)
    {
        if(arrivalTime>0)
        {
            arrivalTime=Mathf.Max(0,arrivalTime-dt);
            if(arrivalTime>0)return;
            featuredEnemy=null;
        }
        if(queuedArrivals.Count==0)return;
        featuredEnemy=queuedArrivals.Dequeue();
        arrivalTime=ArrivalLength;
        PrepareEnemyPortrait(featuredEnemy);
        Chime(featuredEnemy.boss?370:587,.11f);
    }

    void PrepareEnemyPortrait(Foe foe)
    {
        int key=PortraitKey(foe);
        if(portraits.ContainsKey(key) || foe.body==null ||
            SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        if(portraitCamera==null)
        {
            portraitCamera=Root("Enemy portrait camera",transform,Vector3.zero).gameObject.AddComponent<Camera>();
            portraitCamera.enabled=false;
            portraitCamera.orthographic=true;
            portraitCamera.cullingMask=1<<PortraitLayer;
            portraitCamera.clearFlags=CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor=Color.clear;
            portraitCamera.nearClipPlane=.12f;
            portraitCamera.farClipPlane=12;
        }

        Transform model=Instantiate(foe.body);
        model.SetPositionAndRotation(V(650,0,650),Quaternion.identity);
        foreach(var part in model.GetComponentsInChildren<Transform>(true))part.gameObject.layer=PortraitLayer;
        var renderers=model.GetComponentsInChildren<Renderer>();
        Bounds shape=renderers[0].bounds;
        foreach(var renderer in renderers)shape.Encapsulate(renderer.bounds);
        portraitCamera.orthographicSize=shape.extents.magnitude*1.02f;
        portraitCamera.transform.position=shape.center+V(2.35f,1.07f,4.2f);
        portraitCamera.transform.LookAt(shape.center);

        var target=new RenderTexture(160,160,24,RenderTextureFormat.ARGB32);
        target.name="Portrait of "+foe.actor.name;
        portraitCamera.targetTexture=target;
        portraitCamera.Render();
        portraitCamera.targetTexture=null;
        portraits.Add(key,target);
        model.gameObject.SetActive(false);
        Destroy(model.gameObject);
    }

    void ReleaseEnemyPortraits()
    {
        foreach(var portrait in portraits.Values)
        {
            portrait.Release();
            Destroy(portrait);
        }
        portraits.Clear();
    }

    void EnemyArrivalCaption()
    {
        if(featuredEnemy==null || arrivalTime<=0)return;
        var foe=featuredEnemy;
        float t=ArrivalLength-arrivalTime;
        float appear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.30f,t));
        float depart=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,0,arrivalTime));
        float opacity=Mathf.Min(appear,1-depart);
        const float width=495;
        bool atBossGate=foe.boss && bossReveal>0;
        float x=(atBossGate?ww-width-39:(ww-width)*.5f)+(1-opacity)*(ww+width)*.45f,y=atBossGate?26:96;
        Color accent=foe.boss?Gold:Hex(ArrivalColors[foe.kind]);
        Color previous=GUI.color;
        GUI.color=new Color(previous.r,previous.g,previous.b,previous.a*opacity);
        Plate(x+4,y+5,width,96,new Color(.12f,.16f,.22f,.37f),15);
        Plate(x,y,width,96,Navy,15);
        Plate(x+9,y+9,79,78,accent,10);
        if(portraits.TryGetValue(PortraitKey(foe),out RenderTexture portrait))
            GUI.DrawTexture(new Rect(x+10,y+9,77,78),portrait,ScaleMode.ScaleToFit,true);
        string name=!foe.boss && foe.kind==2?"盾持ちピーマン":EnemyName(foe);
        Caption(x+105,y+6,width-120,21,(foe.boss?"ボス登場":"初登場")+"　／　"+EntranceName(foe.side),13,accent,true);
        Caption(x+102,y+26,width-113,35,name+" 接近中！",25,Paper,true);
        string hint=foe.CookieCommander?"ハートが効く。大ジャンプは赤い輪の外へ。":
            foe.boss?"盾を割って、踏みつけをジャンプでよけよう。":ArrivalHints[foe.kind];
        Caption(x+104,y+61,width-119,25,hint,14,Paper);
        GUI.color=previous;
    }

    void StartBetrayalLesson(Child baby)
    {
        betrayalLessonSeen=true;
        betrayalChild=baby;
        betrayalTime=0;
        StopCry(baby);
        DuckMusic(BetrayalLength+.2f);
        PointAt(baby.actor,baby.actor.position+V(.76f,0,-4.1f));
    }

    void TurnBaby(Child baby)
    {
        StopCry(baby);
        baby.mood=Mood.Turned;
        baby.face.SetDevil(true);
        baby.target=baby.actor.position.x<0?0:1;
        Chime(196);
        Say(Names[baby.index]+"が寝返った！　近くで E を5秒長押し");
    }

    void AdvanceBetrayalLesson(float dt)
    {
        var baby=betrayalChild;
        float before=betrayalTime;
        betrayalTime+=dt;
        if(before<BetrayalAt && betrayalTime>=BetrayalAt)
        {
            TurnBaby(baby);
            Chime(294,.17f);
        }

        bool turned=baby.mood==Mood.Turned;
        float change=turned?Mathf.Clamp01((betrayalTime-BetrayalAt)/.55f):0;
        float bounce=Mathf.Sin(change*Mathf.PI);
        baby.face.Pose(turned?BabyExpression.Look.Upset:BabyExpression.Look.Crying,betrayalTime,0);
        baby.body.localPosition=V(0,turned?.36f*bounce:.28f,0);
        baby.body.localScale=V(1+.13f*bounce,1-.12f*bounce,1+.13f*bounce);
        baby.body.localRotation=Quaternion.Euler(0,0,turned?0:Mathf.Sin(betrayalTime*16)*6);
        if(betrayalTime<BetrayalLength)return;
        baby.body.localScale=Vector3.one;
        betrayalChild=null;
        betrayalTime=0;
        MoveCamera(true);
    }

    void ShowBetrayalLesson()
    {
        float zoom=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,1.16f,betrayalTime));
        Vector3 place=betrayalChild.actor.position;
        Vector3 look=place+V(0,1.23f,0);
        Vector3 eye=place+V(Mathf.Lerp(1.02f,.67f,zoom),Mathf.Lerp(2.20f,1.75f,zoom),Mathf.Lerp(-5.35f,-3.85f,zoom));
        cam.fieldOfView=Mathf.Lerp(45,36,zoom);
        cam.transform.position=eye;
        cam.transform.LookAt(look);
        ClearSightline(eye,look,true);
    }

    void BetrayalLessonCaption()
    {
        bool turned=betrayalChild.mood==Mood.Turned;
        string name=Names[betrayalChild.index];
        const float width=635;
        float x=(ww-width)*.5f,y=hh-159;
        Plate(0,0,ww,32,Navy);
        float flash=turned?.21f*(1-Mathf.InverseLerp(BetrayalAt,BetrayalAt+.35f,betrayalTime)):0;
        if(flash>0)Plate(0,0,ww,hh,new Color(.92f,.18f,.24f,flash));
        Plate(x,y,width,131,Navy,17);
        Plate(x+12,y+18,5,94,turned?Danger:Pink,2);
        Caption(x+30,y+9,width-60,23,"ミルクが間に合わなかった",15,turned?Danger:Pink,true);
        Caption(x+29,y+32,width-58,46,turned?name+"が寝返った！":name+"が泣き続けている…",31,Paper,true);
        string why=turned?"泣き始めてから10秒。赤ちゃんが味方のおうちを攻撃するよ。":
            "機嫌ゲージが空になると泣き始める。泣いているうちにミルクを届けよう。";
        Caption(x+31,y+77,width-62,24,why,15,Paper);
        if(turned)Caption(x+31,y+103,width-62,20,"哺乳瓶を置いて近づき、E または右クリックを5秒長押し。",15,Gold,true);
    }
}
