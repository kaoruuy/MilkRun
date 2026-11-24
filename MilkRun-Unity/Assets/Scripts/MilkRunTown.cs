using UnityEngine;
using static ToyArt;
using static ToyGarden;

public partial class MilkRun
{
    enum Reward { None, Stork, ProteinMilk, Rattle }

    const int MilkThiefKind=4;
    const float TownWaveLength=75, ProteinLength=12;
    const float StorkFlightSpeed=3.2f, StorkFlightHeight=2.8f;
    const float MilkThiefEscapeSpeed=2.15f;
    const string StorkName="コウノトリ";
    const string MilkThiefCriticalText="クリティカル −";
    static readonly float[] TownWaves={0,4,8,12,17,22,26,31,35,39,43,47,51,55,59,63,66,69,73};
    static readonly int[] TownFoes={4,0,6,2,3,0,4,1,2,5,6,4,2,1,3,2,6,4,3};
    static readonly string[] RewardNames={StorkName,"プロテインミルク","ガラガラ"};
    static readonly string[] RewardDetails={
        "お腹が空いた赤ちゃんへ自動でミルクを届ける。\n補給所から、ゆっくり飛んで配達する。",
        "補給所の3本に1本がプロテインミルク。\n届けた子は12秒間、ハートを速く投げる。",
        "赤ちゃんをあやすおもちゃを左手に持つ。\nプレイヤーの攻撃力が1上がる。"
    };
    bool candyTown;
    Reward reward,rewardPreview;
    Transform arena,stork,storkBottle,wingLeft,wingRight;
    Child storkTarget;
    float storkRest;
    Foe milkCarrier;
    float milkProtectedUntil;
    int milkRecoveries;

    bool MilkAvailable => milkCarrier==null;
    float DefenseLength => candyTown?TownWaveLength:WaveLength;
    static Vector3 StorkHome => MilkPoint+V(-.65f,StorkFlightHeight,2.2f);
    bool RattleEquipped => reward==Reward.Rattle;
    int PlayerDamage(bool critical=false) => (critical?8:3)+(RattleEquipped?1:0);

    void LoadArena(bool town)
    {
        if(arena!=null && candyTown==town)return;
        if(arena!=null)
        {
            arena.gameObject.SetActive(false);
            Destroy(arena.gameObject);
        }
        candyTown=town;
        arena=ToyGarden.Create(town);
        cam.backgroundColor=RenderSettings.fogColor;
    }

    void SelectReward(Reward choice)
    {
        if(phase==Phase.Rewards)rewardPreview=choice;
    }

    void ConfirmReward()
    {
        if(phase==Phase.Rewards && rewardPreview!=Reward.None)StartTown(rewardPreview);
    }

    bool TryRewardTestShortcut(bool pHeld,bool aHeld,bool pressedThisFrame)
    {
        if(!pHeld || !aHeld || !pressedThisFrame || phase==Phase.Rewards)return false;
        reward=Reward.None;
        LoadArena(false);
        ResetActors();
        Ending(Phase.Rewards);
        return true;
    }

    void StartTown(Reward choice)
    {
        LoadArena(true);
        reward=choice;
        phase=Phase.Wave;
        ResetActors();
        foreach(var baby in children)
        {
            baby.food=FullBottle;
            baby.delay=baby.index*4;
        }
        firstDelivery=true;
        waveMessage=5.8f;
        CursorMode(true);
        Say("ミルクどろぼうは補給所をねらう！");
        Chime(660,.18f);
        Chime(988,.12f);
    }

    void ResetTownActors()
    {
        milkCarrier=null;
        rewardPreview=Reward.None;
        milkProtectedUntil=0;
        milkRecoveries=0;
        MilkStock.gameObject.SetActive(true);
        stork=storkBottle=wingLeft=wingRight=null;
        storkTarget=null;
        storkRest=3;
        if(!candyTown)return;
        if(reward==Reward.Stork)
        {
            stork=Root(StorkName,cast,StorkHome);
            stork.rotation=Quaternion.Euler(0,90,0);
            Ball(stork,0,0,0,.54f,.42f,1.04f,White);
            Ball(stork,0,.26f,.47f,.30f,.62f,.32f,White);
            Ball(stork,0,.53f,.56f,.34f,.32f,.37f,White);
            Ball(stork,0,.48f,.87f,.14f,.12f,.57f,Honey);
            Ball(stork,-.147f,.56f,.65f,.065f,.065f,.065f,Ink);
            Ball(stork,.147f,.56f,.65f,.065f,.065f,.065f,Ink);
            wingLeft=StorkWing(-1);wingRight=StorkWing(1);
            storkBottle=Bottle(stork,V(0,-.85f,.10f),.79f);
            storkBottle.gameObject.SetActive(false);
        }
        else if(RattleEquipped)Rattle(leftArm,V(0,-.36f,.15f));
    }

    Transform StorkWing(int side)
    {
        var wing=Root("Wing",stork,V(side*.25f,.13f,0));
        Ball(wing,side*.61f,0,-.10f,1.55f,.13f,.58f,White);
        Ball(wing,side*1.21f,0,-.19f,.44f,.12f,.34f,Ink);
        return wing;
    }

    void UpdateTownReward(float dt)
    {
        if(stork==null)return;
        float flap=Mathf.Sin(clock*11)*30;
        wingLeft.localRotation=Quaternion.Euler(0,0,flap);
        wingRight.localRotation=Quaternion.Euler(0,0,-flap);

        if(storkTarget!=null && (storkTarget.mood==Mood.Turned || storkTarget.food>=FullBottle-.1f))
        {
            storkTarget=NeedMilk();
            storkBottle.gameObject.SetActive(storkTarget!=null);
        }
        if(storkTarget==null)
        {
            Vector3 home=StorkHome;
            storkRest=Mathf.Max(0,storkRest-dt);
            WalkTo(stork,home,StorkFlightSpeed,dt);
            if(storkRest<=0 && MilkAvailable && (stork.position-home).sqrMagnitude<.04f)
            {
                storkTarget=NeedMilk();
                storkBottle.gameObject.SetActive(storkTarget!=null);
            }
            return;
        }

        Vector3 destination=storkTarget.actor.position+Vector3.up*StorkFlightHeight;
        WalkTo(stork,destination,StorkFlightSpeed,dt);
        if((stork.position-destination).sqrMagnitude>=.25f)return;
        Feed(storkTarget,true);
        Say(StorkName+"が"+Names[storkTarget.index]+"に配達！");
        storkTarget=null;
        storkRest=11;
        storkBottle.gameObject.SetActive(false);
    }

    Child NeedMilk()
    {
        Child neediest=null;
        foreach(var baby in children)
        {
            if(baby.mood==Mood.Turned || baby.mood==Mood.Returning || baby.food>FullBottle-3)continue;
            if(neediest==null || baby.food-baby.tears<neediest.food-neediest.tears)neediest=baby;
        }
        return neediest;
    }

    void UpdateMilkThief(Foe foe,float dt)
    {
        bool escaping=milkCarrier==foe;
        Vector3 target=escaping?Route(foe.side,0):foe.waypoint<=2?Route(foe.side,foe.waypoint):MilkPoint+V(0,0,1.35f);
        bool canSteal=MilkAvailable && clock>=milkProtectedUntil;
        if(!escaping && !canSteal && foe.waypoint>2)
            target+=V(foe.side==0?-2.5f:2.5f,0,1.1f);
        WalkTo(foe.actor,target,escaping?MilkThiefEscapeSpeed:foe.speed,dt);
        FoePose(foe,0);
        if(escaping || (foe.actor.position-target).sqrMagnitude>.13f)return;
        if(foe.waypoint<=2)
        {
            foe.waypoint++;
            return;
        }
        if(!canSteal)return;
        milkCarrier=foe;
        MilkStock.gameObject.SetActive(false);
        foe.loot.gameObject.SetActive(true);
        Speak(foe,AttackLines[MilkThiefKind]);
        Say("ミルクを盗まれた！　F でどろぼうを捕まえよう");
        Chime(207,.25f);
    }

    void HitMilkThief(Foe foe,bool byPlayer)
    {
        bool critical=byPlayer && foe==milkCarrier;
        int damage=byPlayer?PlayerDamage(critical):1;
        int before=foe.hp;
        foe.hp=Mathf.Max(byPlayer?0:1,foe.hp-damage);
        if(critical)
        {
            foe.hitTime=.85f;
            foe.hitWord=MilkThiefCriticalText+damage;
            Chime(820,.17f);
            Burst(foe.actor.position+Vector3.up*.8f,7,false);
            if(foe.hp==0)RestoreMilk(foe);
            else Say("クリティカル！　続けて F で追撃");
        }
        else if(foe.hitTime<=0 || byPlayer || !IsMilkThiefCritical(foe))
        {
            foe.hitTime=.65f;
            foe.hitWord=byPlayer?"打撃  −"+damage:foe.hp==before?"ハートに抵抗":"ハート  −1";
        }
    }

    static bool IsMilkThiefCritical(Foe foe) =>
        foe.hitWord?.StartsWith(MilkThiefCriticalText,System.StringComparison.Ordinal)==true;

    void RestoreMilk(Foe foe)
    {
        if(milkCarrier!=foe)return;
        milkCarrier=null;
        milkProtectedUntil=clock+4;
        milkRecoveries++;
        MilkStock.gameObject.SetActive(true);
        foe.loot.gameObject.SetActive(false);
        wasAtMilk=false;
        Say("ミルクを取り戻した！　補給所が使えるよ");
        MilkSound(bottleClink);
    }

    void RewardScreen()
    {
        Plate(0,0,ww,hh,new Color(.11f,.16f,.23f,.69f));
        const float width=1050,height=560;
        float x=(ww-width)*.5f,y=(hh-height)*.5f;
        Plate(x,y,width,height,Paper,24);
        Caption(x+29,y+21,width-58,26,"1面クリア  ／  次は「お菓子の町」",17,Grey,true);
        Caption(x+28,y+49,width-56,53,"報酬をひとつ選ぼう",36,Navy,true);
        const float cardGap=23,cardHeight=300;
        float cardWidth=(width-56-cardGap*(RewardNames.Length-1))/RewardNames.Length;
        for(int i=0;i<RewardNames.Length;i++)
        {
            float cx=x+28+i*(cardWidth+cardGap),cy=y+111;
            var card=new Rect(cx,cy,cardWidth,cardHeight);
            Reward choice=(Reward)(i+1);
            bool selected=rewardPreview==choice, hover=card.Contains(Event.current.mousePosition);
            Color color=choice switch
            {
                Reward.Stork => Aqua,
                Reward.ProteinMilk => Gold,
                _ => Hex("D1B9EC")
            };
            Plate(cx-3,cy-3,cardWidth+6,cardHeight+6,selected?color:Hex("D1C4AF"),18);
            Plate(cx,cy,cardWidth,cardHeight,Navy,16);
            Plate(cx+10,cy+10,cardWidth-20,227,hover?Color.Lerp(color,Color.white,.20f):color,12);
            DrawRewardIcon(choice,cx+cardWidth*.5f,cy+125);
            Plate(cx+20,cy+20,34,31,selected?Navy:Dark,7);
            Caption(cx+20,cy+20,34,31,(i+1).ToString(),17,Paper,true,TextAnchor.MiddleCenter);
            Caption(cx+12,cy+243,cardWidth-24,48,RewardNames[i],24,Paper,true,TextAnchor.MiddleCenter);
            if(GUI.Button(card,GUIContent.none,GUIStyle.none))SelectReward(choice);
        }
        if(rewardPreview!=Reward.None)
        {
            int index=(int)rewardPreview-1;
            Plate(x+28,y+428,width-56,88,Pale,13);
            Caption(x+46,y+434,685,27,RewardNames[index],19,Navy,true);
            Caption(x+46,y+462,685,44,RewardDetails[index],16,Navy);
            if(Button(x+width-262,y+446,216,48,"この報酬で2面へ",Gold,Navy,18))
            {
                ConfirmReward();
                return;
            }
        }
        if(Button(x+width-140,y+524,112,25,"タイトルへ",Pale,Navy,13)){ReturnToTitle();return;}
        Caption(x+30,y+525,800,21,rewardPreview==Reward.None?"カードをクリック、または 1・2・3 キー":"1・2・3 キーで選び直し  ／  Enter で決定",13,Grey,true);
    }

    void DrawRewardIcon(Reward choice,float x,float y)
    {
        Matrix4x4 previous=GUI.matrix;
        GUI.matrix=previous*Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.identity,new Vector3(2.48f,2.48f,1));
        if(choice==Reward.Stork)
        {
            MapLine(new Vector2(-43,-13),new Vector2(-5,3),Paper,13);
            MapLine(new Vector2(-5,3),new Vector2(28,-15),Paper,12);
            MapLine(new Vector2(-43,-13),new Vector2(-34,-9),Navy,8);
            MapLine(new Vector2(22,-12),new Vector2(28,-15),Navy,8);
            Plate(-16,-5,36,18,Paper,9);
            MapLine(new Vector2(10,1),new Vector2(24,-18),Paper,9);
            Plate(19,-24,14,13,Paper,6);
            Plate(29,-21,14,4,Gold,2);
            Plate(28,-21,2,2,Navy,1);
            MapLine(new Vector2(-2,10),new Vector2(-8,17),Hex("C68665"),1.9f);
            Art(-22,11,27,30,milkIcon);
        }
        else if(choice==Reward.ProteinMilk)
        {
            Plate(-31,22,62,8,Hex("DAA857"),4);
            Art(-35,-35,67,68,milkIcon);
            Plate(-15,3,29,14,Hex("9872C7"),4);
            Caption(-15,3,29,14,"P",10,Color.white,true,TextAnchor.MiddleCenter);
            Plate(20,-18,25,24,Navy,7);
            Caption(20,-18,25,24,"+",19,Color.white,true,TextAnchor.MiddleCenter);
        }
        else if(choice==Reward.Rattle)
        {
            GUI.matrix=GUI.matrix*Matrix4x4.Rotate(Quaternion.Euler(0,0,28));
            Plate(-6,-2,12,35,Navy,6);
            Plate(-4,-2,8,32,Aqua,4);
            Plate(-9,23,18,13,Navy,6);
            Plate(-7,25,14,8,Gold,4);
            Plate(-22,-40,44,44,Navy,22);
            Plate(-20,-38,40,40,Hex(Rose),20);
            Plate(-19,-22,38,9,Gold,4);
            Plate(-11,-32,12,6,Paper,3);
        }
        GUI.matrix=previous;
    }

    void DrawTownStatus()
    {
        const float width=502;
        float x=(ww-width)*.5f;
        Color background=MilkAvailable?Navy:Hex("763638");
        Plate(x,17,width,67,background,15);
        string item=RewardNames[(int)reward-1]+(RattleEquipped?"（攻撃力 +1）":"");
        Caption(x+12,21,width-24,24,"2面　お菓子の町　／　"+item,15,Gold,true,TextAnchor.MiddleCenter);
        string hint=MilkAvailable?(elapsed<8?"ミルクどろぼうは補給所をねらう！":"ふたつのおうちを守りきろう"):"ミルクを盗まれた！　F でどろぼうを捕まえよう";
        Caption(x+10,44,width-20,33,hint,20,Paper,true,TextAnchor.MiddleCenter);
    }
}
