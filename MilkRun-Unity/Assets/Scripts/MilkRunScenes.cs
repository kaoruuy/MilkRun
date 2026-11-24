using UnityEngine;
using static ToyArt;
using static ToyGarden;

public partial class MilkRun
{
    const float OpeningLength=4.35f, OutpostWindup=1.77f, EntranceCallAt=.36f;
    const float MoodMoveAt=1.14f, MoodArrivesAt=1.87f;
    const float OutpostImpact=OutpostWindup+AttackLength-ImpactTime;
    Foe waveLead;
    Transform outpost,outpostTop,outpostRuins;
    float waveEntranceTime;
    AudioClip waveCall;

    static Vector3 WaveGateStop()
    {
        Vector3 gate=Route(0,0);
        return Vector3.MoveTowards(gate,Route(0,1),1.36f);
    }

    void ShowFedBaby()
    {
        float time=PracticeLength-practiceTime;
        float zoom=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.52f));
        Vector3 baby=practiceChild.actor.position;
        Vector3 eye=baby+V(.23f,2.04f,Mathf.Lerp(-5.7f,-4.3f,zoom));
        Vector3 look=baby+V(.86f,1.44f,0);
        cam.fieldOfView=Mathf.Lerp(42,36,zoom);
        cam.transform.position=eye;
        cam.transform.LookAt(look);
        ClearSightline(eye,look,true);
    }

    Rect MoodLessonCard => new Rect(ww-476,hh*.5f-92,432,183);

    Rect MovingMoodMeter(int index,float time)
    {
        Rect card=MoodLessonCard;
        Rect from=new Rect(card.x+23,card.y+114,card.width-46,17);
        Rect to=MoodHudMeter(index);
        float move=Mathf.SmoothStep(0,1,Mathf.InverseLerp(MoodMoveAt,MoodArrivesAt,time));
        return new Rect(Vector2.Lerp(from.position,to.position,move),Vector2.Lerp(from.size,to.size,move));
    }

    static float MoodLandingLight(float time)
    {
        return time<MoodArrivesAt?0:1-Mathf.InverseLerp(MoodArrivesAt,MoodArrivesAt+.67f,time);
    }

    void FoodLessonCaption()
    {
        float time=PracticeLength-practiceTime;
        int index=practiceChild.index;
        Color accent=BabyColor(practiceChild);
        bool moving=time>=MoodMoveAt && time<MoodArrivesAt;
        if(time>=MoodMoveAt)
            DrawMoodHud(moving?index:-1,index,moving?1:MoodLandingLight(time));
        float shown=Mathf.SmoothStep(lessonFoodBefore,FullBottle,Mathf.InverseLerp(.13f,.78f,time));
        if(time<MoodMoveAt+.24f)
            DrawMoodLessonCard(shown,accent,1-Mathf.InverseLerp(MoodMoveAt,MoodMoveAt+.24f,time));
        if(time>=MoodArrivesAt)return;

        Rect meter=MovingMoodMeter(index,time);
        if(moving)Plate(meter.x-3,meter.y-3,meter.width+6,meter.height+6,Navy,7);
        Bar(meter.x,meter.y,meter.width,meter.height,shown/FullBottle,accent,Dark);
    }

    void DrawMoodLessonCard(float shown,Color accent,float opacity)
    {
        Rect card=MoodLessonCard;
        float x=card.x,y=card.y,width=card.width;
        Color original=GUI.color;
        GUI.color=new Color(original.r,original.g,original.b,original.a*opacity);
        Plate(x+5,y+6,width,183,new Color(.08f,.12f,.16f,.33f),17);
        Plate(x,y,width,183,Navy,17);
        Plate(x+22,y+20,46,46,Paper,12);
        Art(x+22,y+20,46,46,milkIcon);
        Caption(x+84,y+17,width-104,24,Names[practiceChild.index]+"にミルクが届いた！",15,accent,true);
        Caption(x+83,y+41,width-106,30,"機嫌ゲージが回復",25,Paper,true);
        Caption(x+23,y+82,220,27,"ミルクで満タン",17,Paper,true);
        Caption(x+256,y+80,155,30,Mathf.CeilToInt(shown)+" / "+(int)FullBottle+" 秒",
            22,accent,true,TextAnchor.MiddleRight);
        Caption(x+23,y+139,width-46,27,"バトル中に減ったら、またミルクを届けよう。",16,Paper,true);
        GUI.color=original;
    }

    void ShowShieldLesson()
    {
        float time=ShieldLessonLength-shieldLessonTime;
        Vector3 focus=Vector3.Lerp(practiceChild.actor.position,sparring.actor.position,.54f)+Vector3.up*1.12f;
        float zoom=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.57f));
        Vector3 eye=focus+V(0,1.60f,Mathf.Lerp(-7.05f,-5.90f,zoom));
        cam.fieldOfView=44;
        cam.transform.position=eye;
        cam.transform.LookAt(focus);
        ClearSightline(eye,focus,true);
    }

    void ShieldLessonCaption()
    {
        float width=626,x=(ww-width)*.5f,y=hh-171;
        Plate(x,y,width,129,Navy,16);
        Plate(x+19,y+21,55,55,Gold,10);
        Art(x+29,y+29,35,35,fortBadge);
        Caption(x+90,y+11,width-110,24,"盾持ちピーマン",15,Gold,true);
        Caption(x+90,y+38,width-111,36,
            shieldLessonBlocked?"盾がハートを弾いた！":"赤ちゃんのハートを見てみよう",24,Paper,true);
        string hint=shieldLessonBlocked?"盾がある間はダメージゼロ。手ぶらで F／左クリックで割ろう。":
            "さっきと同じハート。今度はピーマンが盾を持っているよ。";
        Caption(x+22,y+84,width-42,29,hint,17,Pale);
    }

    void PracticePepperCaption()
    {
        bool hit=practicePepper.hp<=0;
        if(!hit)Tag(practicePepper.actor.position+Vector3.up*1.77f,"盾なしピーマン",Aqua,true);
        const float w=565;
        float x=(ww-w)*.5f,y=hh-135;
        Plate(x,y,w,97,Navy,14);
        Caption(x+22,y+8,w-44,25,"盾なしピーマン",15,Aqua,true);
        Caption(x+21,y+36,w-42,47,hit?"ハートが当たって倒れた！":"ミルクを飲んだ赤ちゃんがハートを投げるよ",hit?29:22,Paper,true);
    }

    void PlaceOutpost()
    {
        Vector3 gate=Route(0,0);
        Vector3 ahead=(Route(0,1)-gate).normalized;
        Vector3 side=Vector3.Cross(Vector3.up,ahead);
        Vector3 at=gate+ahead*2.30f-side*1.20f;
        outpost=Root("Lookout post",cast,at);
        PointAt(outpost,gate+ahead*4);
        Box(outpost,0,.095f,0,1.57f,.19f,1.17f,"B99977");
        outpostTop=Root("Lookout wall",outpost,Vector3.zero);
        for(int i=-1;i<=1;i++)
        {
            Box(outpostTop,i*.47f,.63f,0,.43f,1.03f+Mathf.Abs(i)*.17f,.28f,i==0?"C69069":"AA6F51");
            Box(outpostTop,i*.47f,.30f,.17f,.42f,.12f,.09f,"885C48");
        }
        Box(outpostTop,.27f,1.30f,.025f,.087f,1.04f,.085f,"9B684C");
        Box(outpostTop,.49f,1.61f,.03f,.53f,.35f,.055f,FortTeal);
        Shield(outpostTop,V(.49f,1.61f,.084f),.265f,White);
        outpostRuins=Root("Fallen planks",outpost,Vector3.zero);
        Box(outpostRuins,-.43f,.19f,.28f,1.11f,.16f,.27f,"C69069").localRotation=Quaternion.Euler(0,37,0);
        Box(outpostRuins,.22f,.25f,.06f,.97f,.18f,.25f,"AA6F51").localRotation=Quaternion.Euler(12,-24,-8);
        Box(outpostRuins,.57f,.15f,-.39f,.63f,.16f,.28f,FortTeal).localRotation=Quaternion.Euler(0,32,0);
        outpostRuins.gameObject.SetActive(false);
    }

    void BreakOutpost(bool animate)
    {
        outpostRuins.gameObject.SetActive(true);
        if(!animate){outpostTop.gameObject.SetActive(false);return;}
        Burst(outpost.position+Vector3.up*.93f,6,false);
        Chime(138,.22f);Chime(240,.10f);
    }

    void StartWaveEntrance()
    {
        waveEntranceTime=OpeningLength;
        waveLead=Spawn(0,types[0],false);
        PlaceOutpost();
        next=1;
        Vector3 gate=Route(0,0);
        Vector3 direction=(Route(0,1)-gate).normalized;
        waveLead.actor.position=gate-direction*2.7f;
        PointAt(waveLead.actor,Route(0,1));
        CueEnemyArrival(waveLead);
        if(!muted && waveCall!=null)sounds.PlayOneShot(waveCall,.45f);
    }

    void AdvanceWaveEntrance(float dt)
    {
        float previous=OpeningLength-waveEntranceTime;
        waveEntranceTime=Mathf.Max(0,waveEntranceTime-dt);
        float time=OpeningLength-waveEntranceTime;
        Vector3 gate=Route(0,0);
        Vector3 direction=(Route(0,1)-gate).normalized;
        float approach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,OutpostWindup,time));
        waveLead.actor.position=Vector3.Lerp(gate-direction*2.7f,WaveGateStop(),approach);
        waveLead.step+=dt*9;
        float swingTime=time-OutpostWindup;
        if(previous<EntranceCallAt && time>=EntranceCallAt)SpeakOnApproach(waveLead);
        if(previous<OutpostWindup && time>=OutpostWindup)Speak(waveLead,"ひっかくニャ！");
        if(swingTime>0 && swingTime<AttackLength)
        {
            PointAt(waveLead.actor,outpost.position);
            FoePose(waveLead,swingTime/AttackLength);
        }
        else FoePose(waveLead,0);
        if(previous<OutpostImpact && time>=OutpostImpact)BreakOutpost(true);
        if(time>=OutpostImpact)
        {
            float fall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(OutpostImpact,OutpostImpact+.52f,time));
            outpostTop.localRotation=Quaternion.Euler(-24*fall,12*fall,78*fall);
            outpostTop.localPosition=V(.66f*fall,-.47f*fall,-.22f*fall);
            if(fall>=1)outpostTop.gameObject.SetActive(false);
        }
        UpdateSparks(dt);
        if(waveEntranceTime<=0)
        {
            PointAt(waveLead.actor,Route(0,1));
            waveLead=null;
            camFocus=player.position+V(0,1.62f+height*.72f,0);
        }
    }

    void ShowWaveEntrance()
    {
        float time=OpeningLength-waveEntranceTime;
        Vector3 gate=Route(0,0);
        Vector3 direction=(Route(0,1)-gate).normalized;
        Vector3 side=Vector3.Cross(Vector3.up,direction);
        float attackView=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.10f,2.10f,time));
        Vector3 eye=gate+direction*Mathf.Lerp(5.9f,5.1f,attackView)+
            side*Mathf.Lerp(2.6f,.70f,attackView)+Vector3.up*Mathf.Lerp(1.24f,1.56f,attackView);
        Vector3 focus=Vector3.Lerp(Vector3.Lerp(waveLead.actor.position,gate,.28f),
            Vector3.Lerp(WaveGateStop(),outpost.position,.43f),attackView)+Vector3.up*.99f;
        cam.fieldOfView=44;
        cam.transform.position=eye;
        cam.transform.LookAt(focus);
        ClearSightline(eye,focus,false);
    }

    void WaveEntranceCaption()
    {
        float time=OpeningLength-waveEntranceTime;
        Color curtain=Hex("252534"), banner=Hex("68363F");
        float letters=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.22f));
        Plate(0,0,ww,68*letters,curtain);
        Plate(0,hh-91*letters,ww,91*letters,curtain);
        Caption(37,15,228,39,"西の入口",19,Gold,true);
        Caption(ww-245,15,208,39,"防衛時間  02:00",22,Paper,true,TextAnchor.MiddleRight);
        float x=Mathf.Lerp(-498,37,letters),y=hh-213;
        Plate(x,y,462,108,banner,13);
        Plate(x,y+12,6,84,Danger,4);
        bool smashed=time>=OutpostImpact;
        Caption(x+23,y+7,416,24,smashed?"敵が庭に侵入":"敵が来た！",16,Gold,true);
        Caption(x+21,y+33,421,47,smashed?"見張り砦が壊された！":"西の入口に接近中",30,Paper,true);
        Caption(x+23,y+77,417,24,"東西のおうちを守ろう",15,Pale);
        if(waveLead.lineUntil>clock)
            Tag(waveLead.actor.position+Vector3.up*1.77f,waveLead.line,Gold,true);
        Plate(0,0,ww,hh,new Color(curtain.r,curtain.g,curtain.b,Mathf.Clamp01((.22f-waveEntranceTime)/.22f)*.85f));
    }
}
