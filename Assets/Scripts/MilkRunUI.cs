using System.Collections.Generic;
using UnityEngine;
using static ToyGarden;

public partial class MilkRun
{
    static readonly Color Navy=Hex("263547"), Paper=Hex("FFF4DF"), Pale=Hex("E9E3D4"), Gold=Hex("F5C66D"),
        Aqua=Hex("9ADAC4"), Pink=Hex("F5A0AC"), Danger=Hex("FF7D79"), Soft=Hex("BECBC7"), Dark=Hex("415360"), Grey=Hex("48575C");
    readonly List<Rect> enemyBadges=new List<Rect>();
    Font font;
    Texture2D milkIcon,bossIcon,guideArrow,fortBadge;
    GUIStyle label;
    readonly Dictionary<int,GUIStyle> styles=new Dictionary<int,GUIStyle>();
    float ww,hh,uiScale;
    static Color Hex(string val){ColorUtility.TryParseHtmlString("#"+val,out var col);return col;}
    void CreateUI()
    {
        font=Resources.Load<Font>("GardenFont");
        guideArrow=new Texture2D(32,32,TextureFormat.RGBA32,false);guideArrow.wrapMode=TextureWrapMode.Clamp;
        for(int j=0;j<32;j++)for(int i=0;i<32;i++)
        {
            float dx=Mathf.Abs(i-15.5f);
            bool filled=(j<15 && dx<4.8f)||(j>=12 && dx<(32-j)*.79f);
            guideArrow.SetPixel(i,j,filled?Color.white:Color.clear);
        }
        guideArrow.Apply();
        fortBadge=new Texture2D(48,48,TextureFormat.RGBA32,false);fortBadge.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<48;y++)for(int x=0;x<48;x++)
        {
            float h=y/47f,edge=Mathf.Min(.45f,h*.82f+.012f);
            bool inside=h<.94f && Mathf.Abs((x+.5f)/48-.5f)<edge;
            fortBadge.SetPixel(x,y,inside?Paper:Color.clear);
        }
        fortBadge.Apply();
        milkIcon=Resources.Load<Texture2D>("ToyIcons/Milk");bossIcon=Resources.Load<Texture2D>("ToyIcons/Boss");
    }
    static void Plate(float x,float y,float w,float h,Color c,float r=0)
    {
        if(w<=0||h<=0)return;
        GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,c,0,r);
    }
    void Caption(float x,float y,float w,float h,string text,int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
    {
        int key=size*32+(bold?16:0)+(int)align;
        if(!styles.TryGetValue(key,out label))
        {
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,alignment=align,clipping=TextClipping.Clip,wordWrap=false,padding=new RectOffset(0,0,0,0),margin=new RectOffset(0,0,0,0)};
            styles[key]=label;
        }
        label.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),text,label);
    }
    static void Art(float x,float y,float w,float h,Texture tex){if(tex!=null)GUI.DrawTexture(new Rect(x,y,w,h),tex,ScaleMode.ScaleToFit,true);}
    Texture BossArt(Foe foe)=>foe.CookieCommander && portraits.TryGetValue(PortraitKey(foe),out var portrait)?portrait:(Texture)bossIcon;
    bool Button(float x,float y,float w,float h,string name,Color bg,Color fg,int size=19)
    {
        Rect r=new Rect(x,y,w,h);bool hover=r.Contains(Event.current.mousePosition);
        Plate(x,y+3,w,h,Navy,11);Plate(x,y,w,h,hover?Color.Lerp(bg,Color.white,.19f):bg,11);
        Caption(x+5,y,w-10,h,name,size,fg,true,TextAnchor.MiddleCenter);
        return GUI.Button(r,GUIContent.none,GUIStyle.none);
    }
    void Bar(float x,float y,float w,float h,float fraction,Color fg,Color bg)
    {
        Plate(x,y,w,h,bg,h*.5f);Plate(x,y,w*Mathf.Clamp01(fraction),h,fg,h*.5f);
    }
    static string Timer(float secs){int t=Mathf.Max(0,Mathf.CeilToInt(secs));return (t/60).ToString("0")+":"+(t%60).ToString("00");}
    void OnGUI()
    {
        uiScale=Mathf.Max(.10f,Mathf.Min(Screen.width/1200f,Screen.height/720f));ww=Screen.width/uiScale;hh=Screen.height/uiScale;
        var previous=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(uiScale,uiScale,1));
        if(phase==Phase.Title)TitleScreen();
        else if(phase==Phase.Story)StoryScreen();
        else if(phase==Phase.Rewards)RewardScreen();
        else if(phase==Phase.Celebration)CelebrationCaption();
        else if(phase==Phase.Collapse || phase==Phase.Victory)EndingCaption();
        else if(phase==Phase.Loss||phase==Phase.Finish)ResultScreen();
        else if(Active)
        {
            if(betrayalChild!=null)BetrayalLessonCaption();
            else if(milkThiefIntro!=null)MilkThiefIntroCaption();
            else if(waveEntranceTime>0)WaveEntranceCaption();
            else if(ShowingFoodLesson)FoodLessonCaption();
            else if(practicePepper!=null)PracticePepperCaption();
            else if(shieldLessonTime>0)ShieldLessonCaption();
            else if(fortTour>0 || bossReveal>0)FilmCaption();
            else
            {
                WorldLabels();Hud();TacticalMap();
                if(phase==Phase.Wave && elapsed<.20f)
                    Plate(0,0,ww,hh,new Color(.14f,.14f,.20f,Mathf.Lerp(.85f,0,elapsed/.20f)));
            }
            if(betrayalChild==null && milkThiefIntro==null)EnemyArrivalCaption();
            if(!Directing && blockedHandsTime>0)BlockedHandsCaption();
            if(paused)PauseScreen();
        }
        GUI.matrix=previous;
    }
    void TitleScreen()
    {
        const float width=533,height=636;
        float x=38,y=(hh-height)*.5f;
        float artWidth=Mathf.Min(694,ww-x-width-67);
        float artX=(ww+x+width-artWidth)*.5f;
        Plate(0,0,ww,hh,new Color(.28f,.34f,.32f,.41f));
        Plate(artX+6,y+6,artWidth,height,new Color(.12f,.18f,.2f,.27f),17);
        Plate(artX,y,artWidth,height,Paper,17);
        if(openingArt[CoverArt]!=null)
            GUI.DrawTexture(new Rect(artX+9,y+9,artWidth-18,height-18),openingArt[CoverArt],ScaleMode.ScaleAndCrop,false);
        Plate(x+6,y+7,width,height,new Color(.12f,.18f,.2f,.28f),24);
        Plate(x,y,width,height,Hex("172735"),24);
        Caption(x+32,y+21,width-64,27,"体験版 0.18.0",15,Aqua,true);
        Caption(x+30,y+52,width-60,83,"MILK RUN",68,Color.white,true);
        Caption(x+32,y+132,width-64,50,"ミルク防衛隊",34,Gold,true);
        Caption(x+32,y+192,width-64,38,"ミルクを届け、東西のおうちを守ろう。",23,Color.white,true);
        Caption(x+32,y+231,width-64,30,"屋台と赤ちゃんに近づくだけで受け取り・配達。",16,Paper,true);

        const float inset=28;
        float inner=width-inset*2;
        Plate(x+inset,y+283,inner,242,Hex("FFF7E8"),16);
        Caption(x+44,y+292,inner-32,33,"操作方法",21,Navy,true);
        Plate(x+44,y+327,inner-32,1,Hex("CCBEA4"));
        TitleControl(x+44,y+340,82,"WASD","移動",20);
        TitleControl(x+302,y+340,83,"Space","ジャンプ",20);
        TitleControl(x+44,y+384,82,"マウス","見回す",17);
        TitleControl(x+302,y+384,83,"ホイール","距離調整",15);
        TitleControl(x+44,y+428,128,"F／左クリック","攻撃・盾割り",15);
        TitleControl(x+302,y+428,52,"M","マップ拡大",22);
        TitleControl(x+44,y+472,128,"E／右クリック","置く・拾う  ／  長押しでなでる",15);

        if(Button(x+inset,y+549,inner,56,"プレイする  ›",Gold,Navy,23))ReadStory();
        Caption(x+inset,y+610,inner,20,"Enter でも進めます",13,Paper,true,TextAnchor.MiddleCenter);
    }
    void TitleControl(float x,float y,float keyWidth,string key,string action,int keySize)
    {
        Plate(x,y,keyWidth,34,Navy,7);
        Caption(x,y,keyWidth,34,key,keySize,Color.white,true,TextAnchor.MiddleCenter);
        Caption(x+keyWidth+11,y,425-keyWidth,34,action,16,Navy,true);
    }
    void StoryScreen()
    {
        Plate(0,0,ww,hh,new Color(.14f,.20f,.23f,.70f));
        const float w=1060,h=648;
        float x=(ww-w)*.5f,y=(hh-h)*.5f;
        Plate(x+6,y+6,w,h,new Color(.07f,.11f,.14f,.46f),24);
        Plate(x,y,w,h,Paper,24);
        Caption(x+32,y+18,w-64,48,"おもちゃの庭のおはなし",33,Navy,true);
        const float panelWidth=490,panelHeight=236;
        for(int i=0;i<StoryLines.Length;i++)
        {
            float left=x+31+i%2*509;
            float top=y+76+i/2*252;
            bool revealed=i<=storyPanel;
            Color frame=i==storyPanel?Hex("AD5D63"):Hex("B49B83");
            Plate(left,top,panelWidth,panelHeight,revealed?frame:Hex("E5D7BD"),12);
            Plate(left+3,top+3,panelWidth-6,panelHeight-6,Hex("FAEBD4"),9);
            if(revealed && openingArt[i]!=null)
                GUI.DrawTexture(new Rect(left+9,top+8,panelWidth-18,183),openingArt[i],ScaleMode.ScaleAndCrop,false);
            if(revealed)
                Caption(left+12,top+192,panelWidth-24,36,StoryLines[i],16,Navy,true,TextAnchor.MiddleCenter);
            Plate(left+16,top+15,33,33,revealed?Paper:Hex("E5D7BD"),16);
            Caption(left+16,top+15,33,33,(i+1).ToString(),17,revealed?Navy:Hex("7B6656"),true,TextAnchor.MiddleCenter);
        }
        bool last=storyPanel==StoryLines.Length-1;
        Caption(x+32,y+582,550,42,last?"東西のおうちを2分間守り、王冠ピーマンを倒そう。":"Enter またはボタンで次のコマへ",last?18:15,Navy,true);
        if(Button(x+w-298,y+580,267,48,last?"ミルクを届けにいく  ›":"次のコマへ  ›",Gold,Navy,21))AdvanceStory();
    }
    void Hud()
    {
        float x=21,y=19;
        Plate(x,y,288,142,Navy,15);
        string header=Training?(sparring!=null?"攻撃の練習":"出撃準備"):phase==Phase.Wave?"防衛・残り時間":"最終決戦";
        Caption(x+15,y+9,149,28,header,19,Paper,true);
        string stat=Training?(openingCountdown>0?Timer(openingCountdown):"練習中"):phase==Phase.Wave?Timer(Remaining):"BOSS";
        Caption(x+157,y+9,114,28,stat,24,phase==Phase.Wave && Remaining<=10?Danger:Gold,true,TextAnchor.MiddleRight);
        Plate(x+15,y+40,255,1,Dark);
        Art(x+16,y+51,21,24,fortBadge);
        Caption(x+43,y+47,103,30,"おうちの耐久",16,Paper,true);
        Caption(x+148,y+47,123,30,"片方でも 0 で敗北",13,Pink,true,TextAnchor.MiddleRight);
        for(int i=0;i<2;i++)
        {
            float row=y+82+i*26;
            Caption(x+16,row,84,23,FortNames[i],15,hitFlash[i]>0?Pink:Paper,true);
            Bar(x+103,row+7,102,9,health[i]/100f,health[i]<35||hitFlash[i]>0?Pink:Aqua,Dark);
            Caption(x+211,row,60,23,health[i]+"/100",14,hitFlash[i]>0?Pink:Paper,true,TextAnchor.MiddleRight);
        }
        DrawMoodHud(highlight:practiceTime>0?practiceChild.index:-1, strength:MoodLandingLight(PracticeLength-practiceTime));
        if(candyTown && phase==Phase.Wave)DrawTownStatus();
        else if(Training || (phase==Phase.Wave && waveMessage>0))
        {
            bool counting=openingCountdown>0;
            string sub=Training?(sparring!=null?(sparring.guard>0?"攻撃の練習   1 / 2":"攻撃の練習   2 / 2"):
                counting?"チュートリアル完了":practiceTime>0?"はじめての配達   3 / 3":(carrying?"はじめての配達   2 / 3":"はじめての配達   1 / 3")):
                "防衛開始";
            string heading=Training?(counting?"敵の到着まで  "+Mathf.CeilToInt(openingCountdown)+" 秒":objective.title):"敵が来た！　マップは M で拡大";
            float mw=490,mx=(ww-mw)*.5f;
            bool opening=phase==Phase.Wave;
            Plate(mx,17,mw,66,opening||(counting && openingCountdown<=3)?Hex("763638"):Navy,15);
            Caption(mx+7,20,mw-14,24,sub,14,Gold,true,TextAnchor.MiddleCenter);
            Caption(mx+5,42,mw-10,34,heading,opening||counting?24:21,Paper,true,TextAnchor.MiddleCenter);
        }
        if(phase==Phase.Boss && king!=null)
        {
            float bx=ww*.5f-184;
            Plate(bx,17,368,74,Navy,14);Art(bx+8,21,55,55,BossArt(king));
            Caption(bx+64,20,181,28,EnemyName(king),20,Paper,true);
            Caption(bx+239,20,115,27,king.hp+" / "+king.maxHp,16,Paper,true,TextAnchor.MiddleRight);
            Bar(bx+65,50,286,7,(float)king.hp/king.maxHp,Danger,Dark);
            string state=king.stomp>0?"踏みつけ！　赤い輪の外へ":king.guard>0?"盾　あと "+king.guard+" 打":king.enraged && king.CookieCommander?"加速中　ハートと打撃で止める":"盾なし　ハートと打撃が効く";
            Caption(bx+65,59,289,23,state,13,king.guard>0?Gold:Aqua,true);
        }
        if(noticeTime>0 && !paused && sparring==null && blockedHandsTime<=0)
        {float w=438;Plate((ww-w)/2,hh-144,w,33,Paper,16);Caption((ww-w)/2,hh-144,w,33,notice,15,Navy,true,TextAnchor.MiddleCenter);}
        if(sparring!=null)SparringPrompt();
        else ActionBar();
    }
    Rect MoodHudPanel => new Rect(ww-293,19,272,168);

    Rect MoodHudMeter(int index)
    {
        Rect panel=MoodHudPanel;
        return new Rect(panel.x+16,panel.y+71+index*35,panel.width-32,8);
    }

    void DrawMoodHud(int inFlight=-1,int highlight=-1,float strength=0)
    {
        Rect panel=MoodHudPanel;
        float right=panel.x,y=panel.y;
        Plate(right,y,panel.width,panel.height,Navy,15);
        Art(right+12,y+8,29,29,milkIcon);
        Caption(right+45,y+5,209,27,"赤ちゃんの機嫌",17,Paper,true);
        Caption(right+45,y+29,211,18,"ミルクで回復・空になると泣く",13,Aqua,true);
        for(int i=0;i<children.Count;i++)
        {
            var baby=children[i];
            Rect meter=MoodHudMeter(i);
            float row=meter.y-20;
            Color c=BabyColor(baby);
            if(i==highlight && strength>0)
            {
                Color halo=c;
                halo.a=(i==inFlight?.12f:.29f)*strength;
                Plate(right+8,row-5,panel.width-16,33,halo,7);
            }
            Plate(right+16,row+4,10,10,c,5);
            Caption(right+34,row-3,65,23,Names[i],15,Paper,true);
            if(baby.protein>0)
            {
                Plate(right+91,row-1,50,17,Gold,6);
                Caption(right+91,row-1,50,17,"連射中",10,Navy,true,TextAnchor.MiddleCenter);
            }
            if(i!=inFlight)
            {
                string stat=baby.mood==Mood.Crying?"寝返りまで "+Mathf.CeilToInt(Grace-baby.tears)+"秒":
                    baby.mood==Mood.Turned?"寝返り中":baby.mood==Mood.Returning?"持ち場へ":(Training?"ミルク待ち":"泣くまで "+Mathf.CeilToInt(baby.food+baby.delay)+"秒");
                if(Training && baby.food>=FullBottle-.1f)stat="満タン";
                Caption(right+125,row-3,131,23,stat,14,c,true,TextAnchor.MiddleRight);
            }
            float fill=i==inFlight?0:baby.mood==Mood.Crying?1-baby.tears/Grace:
                baby.mood==Mood.Turned?baby.pet/PetTime:baby.food/FullBottle;
            Bar(meter.x,meter.y,meter.width,meter.height,fill,c,Dark);
        }
    }
    Color BabyColor(Child b)=>b.mood==Mood.Turned?Danger:b.mood==Mood.Crying?Pink:Hex(Accents[b.index]);
    void WorldLabels()
    {
        foreach(var b in children)
        {
            string text=petting==b?"なでなで…":b.mood==Mood.Returning?"持ち場へ  "+Names[b.index]:b.joy>0?"ごきげん！  "+Names[b.index]:b.mood==Mood.Turned?"寝返り  "+Names[b.index]:b.mood==Mood.Crying?"ミルク！  "+Names[b.index]:Names[b.index];
            Tag(b.actor.position+Vector3.up*2.8f,text,BabyColor(b),b.mood==Mood.Turned||b.mood==Mood.Crying);
        }
        if(!Training || objective.kind!=TaskKind.Milk)
            Tag(MilkPoint+Vector3.up*3.5f,MilkAvailable?"補給所・ミルク屋台":"補給所・ミルクがない！",MilkAvailable?Paper:Pink,!MilkAvailable);
        if(storkTarget!=null)Tag(stork.position+Vector3.up*.95f,StorkName,Aqua,true);
        for(int i=0;i<2;i++)
        {
            bool hit=hitFlash[i]>.05f;
            Tag(Forts[i]+Vector3.up*4.48f,hit?FortNames[i]+"が攻撃されている！":"守る拠点・"+FortNames[i],hit?Pink:Aqua,true);
        }
        if(sparring!=null && SparringInReach)
        {
            Vector3 head=sparring.actor.position+Vector3.up*1.96f;
            if(sparring.shieldBreakTime>0)Tag(head,"盾が割れた！",Gold,true);
            else WorldAction(head,carrying?"E":"F",!shieldLessonSeen?"盾の練習":carrying?"瓶を置く":sparring.guard>0?"盾を割る":"本体を攻撃",true);
        }
        else if(Training && !paused)DestinationMarker();
        var childToPet=petting??Closest(children,b=>b.actor.position,b=>b.mood==Mood.Turned,9f);
        if(childToPet!=null)
        {
            bool ready=!carrying && Nearby(true)==childToPet && height<.16f && placing<=0;
            string hint=petting!=null?"なでなで中":carrying?"瓶を置いて長押し":"長押しでなでる";
            WorldAction(childToPet.actor.position+Vector3.up*2.8f,"E",hint,ready,36,petting?.pet/PetTime??-1);
        }
        enemyBadges.Clear();
        if(!Training)foreach(var enemy in foes)EnemyBadge(enemy);
        foreach(var bottle in bottles)
        {
            bool protein=bottle.kind==MilkKind.Protein;
            Tag(bottle.actor.position+Vector3.up*1.25f,protein?"プロテインミルク":"哺乳瓶",protein?Gold:Paper,false);
        }
    }
    void WorldAction(Vector3 at,string key,string action,bool ready,float lift=0,float progress=-1)
    {
        Vector3 view=cam.WorldToViewportPoint(at);
        float width=47+action.Length*14,x=view.x*ww-width*.5f,y=(1-view.y)*hh-16-lift;
        if(view.z<.2f || x<8 || x+width>ww-8 || y<79 || y>hh-119)return;
        Plate(x,y,width,32,Navy,8);
        DrawWorldAction(x+3,y+3,width-6,key,action,ready);
        if(progress>=0)Bar(x+32,y+27,width-40,3,progress,Aqua,Dark);
    }

    void DrawWorldAction(float x,float y,float w,string key,string action,bool ready)
    {
        Plate(x,y,25,24,ready?Gold:Paper,5);
        Caption(x,y,25,24,key,19,Navy,true,TextAnchor.MiddleCenter);
        Caption(x+31,y,w-33,24,action,13,Paper,true);
    }

    void EnemyBadge(Foe f)
    {
        if(f.hp<=0)return;
        float dist=Vector3.Distance(player.position,f.actor.position);
        if(dist>24 && !f.boss && f.windup<=0)return;
        Vector3 view=cam.WorldToViewportPoint(f.actor.position+Vector3.up*(f.boss?4.05f:1.96f));
        float x=view.x*ww,y=(1-view.y)*hh;
        if(view.z<.2f || x<58 || x>ww-58 || y<100 || y>hh-112)return;
        string name=!f.boss && f.guard>0?"盾持ちピーマン":EnemyName(f);
        bool stolen=f==milkCarrier;
        bool critical=f.hitTime>0 && IsMilkThiefCritical(f);
        string label=critical?f.hitWord:stolen?"ミルクを盗んだ！":f.shieldBreakTime>0?"盾が割れた！":f.stomp>0?"踏みつけ！":f.windup>0?"おうちを攻撃！":f.guardFlash>0?f.hitWord:f.hitTime>0?f.hitWord:name;
        bool speaking=f.lineUntil>clock;
        float w=Mathf.Max(critical?146:f.boss?134:120,speaking?f.line.Length*13+15:0);
        bool actionPrompt=(f.guard>0 || stolen) && dist<9.5f;
        float promptHeight=actionPrompt?28:0;
        float speechHeight=speaking?23:0;
        Rect card=new Rect(x-w*.5f,y-30-promptHeight-speechHeight,w,46+promptHeight+speechHeight);
        for(int offset=0;offset<4;offset++)
        {
            bool overlaps=false;
            foreach(var placed in enemyBadges)if(card.Overlaps(placed)){overlaps=true;break;}
            if(!overlaps)break;
            card.y-=card.height+2;
        }
        if(card.y<91)return;
        enemyBadges.Add(card);
        float left=card.x,top=card.y+promptHeight+speechHeight;
        Plate(left,card.y,w,card.height,Navy,8);
        if(speaking)
        {
            Plate(left+3,card.y+3,w-6,20,Gold,5);
            Caption(left+4,card.y+3,w-8,20,f.line,13,Navy,true,TextAnchor.MiddleCenter);
        }
        if(actionPrompt)DrawWorldAction(left+6,card.y+speechHeight+4,w-12,carrying?"E":"F",carrying?"瓶を置く":stolen?"捕まえる":"盾を割る",dist<StrikeRange);
        Caption(left+3,top+1,w-6,18,label,12,critical?Gold:stolen?Pink:f.shieldBreakTime>0?Gold:f.guardFlash>0?Color.white:f.windup>0||f.stomp>0?Danger:f.guard>0?Gold:Paper,true,TextAnchor.MiddleCenter);
        int power=EnemyDamage(f);
        string detail=f.kind==MilkThiefKind?(stolen?"補給所をストップ中":"ミルク屋台へ進軍中"):"HP "+f.hp+"/"+f.maxHp+"　おうち −"+power;
        Caption(left+4,top+18,w-8,16,detail,11,Pale,false,TextAnchor.MiddleCenter);
        Bar(left+8,top+35,w-16,5,(float)f.hp/f.maxHp,Pink,Dark);
        for(int j=0;j<f.guard;j++)Plate(left+8+j*13,top+41,10,3,Gold,1);
    }
    void FilmCaption()
    {
        bool tour=fortTour>0;
        float t=BossRevealLength-bossReveal;
        string name,detail,kicker;
        if(tour)
        {
            for(int i=0;i<2;i++)Tag(Forts[i]+Vector3.up*4.48f,FortNames[i],Aqua,true);
            kicker="守る拠点";
            name="東西のおうちを守ろう";
            detail="盾の旗が目印。片方でも壊れたら敗北。";
        }
        else if(t<.88f){kicker="残り  0:00";name=(candyTown?"1分15秒":"2分間")+"、守りきった！";detail=EntranceName(king.side)+"から、大きな足音がする……";}
        else{kicker="最後のボス   /   "+EntranceName(king.side);name=EnemyName(king)+"が出現！";
            detail=king.CookieCommander?"ハートと打撃で迎え撃とう。大ジャンプは赤い輪が目印！":"F または左クリックで盾を割れ。赤い輪はジャンプで回避！";}
        if(!tour && king.lineUntil>clock)
            Tag(king.actor.position+Vector3.up*3.4f,king.line,Gold,true);
        float w=580,x=(ww-w)*.5f,y=hh-157;
        Plate(x,y,w,122,Navy,16);
        Caption(x+12,y+8,w-24,23,kicker,15,tour?Aqua:Gold,true,TextAnchor.MiddleCenter);
        Caption(x+12,y+31,w-24,53,name,33,Paper,true,TextAnchor.MiddleCenter);
        Caption(x+12,y+83,w-24,29,detail,17,Paper,false,TextAnchor.MiddleCenter);
    }
    void Tag(Vector3 at,string text,Color color,bool urgent)
    {
        Vector3 s=cam.WorldToScreenPoint(at);if(s.z<=.2f)return;
        float x=s.x/uiScale,y=(Screen.height-s.y)/uiScale;float w=22+text.Length*14;
        if(x<w*.5f || x>ww-w*.5f || y<75 || y>hh-95)return;
        Plate(x-w*.5f,y-13,w,25,urgent?Navy:color,12);Caption(x-w*.5f,y-13,w,25,text,13,urgent?color:Navy,true,TextAnchor.MiddleCenter);
    }
    void DestinationMarker()
    {
        if(objective.kind==TaskKind.Ready)return;
        Vector3 at=objective.at;
        float heightOffset=objective.kind==TaskKind.Milk?3.8f:objective.kind==TaskKind.Practice?2.7f:3.25f;
        Vector3 v=cam.WorldToViewportPoint(at+Vector3.up*heightOffset);
        float dist=Vector3.Distance(player.position,at);
        float x=v.x*ww,y=(1-v.y)*hh;
        bool besideMap=x>ww-300 && y<480;
        bool visible=v.z>.2f && x>124 && x<ww-124 && y>108 && y<hh-160 && !besideMap;
        float angle=180;
        if(!visible)
        {
            Vector3 relative=Quaternion.Euler(0,-yaw,0)*(at-player.position);
            float az=Mathf.Atan2(relative.x,relative.z)*Mathf.Rad2Deg;
            if(Mathf.Abs(az)<28 && relative.z>0){x=ww*.5f;y=hh*.30f;angle=0;}
            else {bool right=az>=0;x=right?ww-104:104;y=right?hh*.72f:hh*.48f;angle=right?90:-90;}
        }
        else y+=2*Mathf.Sin(clock*5);
        float width=visible?132:178;
        Plate(x-width*.5f,y-19,width,39,Gold,16);
        var m=GUI.matrix;var old=GUI.color;
        GUI.matrix=m*Matrix4x4.TRS(new Vector3(x-width*.5f+19,y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        GUI.color=Navy;GUI.DrawTexture(new Rect(-9,-9,18,18),guideArrow);
        GUI.color=old;GUI.matrix=m;
        Caption(x-width*.5f+31,y-17,width-34,32,visible?objective.place:objective.place+"  "+Mathf.CeilToInt(dist)+"m",visible?13:14,Navy,true,TextAnchor.MiddleCenter);
    }
    void SparringPrompt()
    {
        string key,title,detail;
        if(placing>0){key="E";title="哺乳瓶を置いている…";detail="手が空いたら攻撃できるよ";}
        else if(carrying){key="E";title="哺乳瓶を床に置こう";detail="E／右クリックで置く・拾う。F／左クリックで攻撃";}
        else if(!shieldLessonSeen){key="WASD";title="盾持ちピーマンに近づこう";detail="赤ちゃんのハートが盾で弾かれるところを見よう";}
        else if(!SparringInReach){key="WASD";title="盾持ちピーマンに近づこう";detail="手が届くところで F／左クリックで攻撃";}
        else
        {
            key="F";title=sparring.guard>0?"近くで盾を割ろう":"もう一度攻撃して、本体を倒そう";
            detail="F を押すか、左クリックで攻撃";
        }
        float w=555,x=(ww-w)*.5f,y=hh-137;
        Plate(x,y,w,88,Navy,15);Plate(x+14,y+13,72,60,Gold,11);
        Caption(x+14,y+13,72,60,key,key.Length==1?39:18,Navy,true,TextAnchor.MiddleCenter);
        Caption(x+101,y+10,w-112,37,title,23,Paper,true);
        Caption(x+102,y+45,w-112,30,detail,14,Pale);
        Plate(ww*.5f-116,hh-37,232,26,Paper,13);
        Caption(ww*.5f-116,hh-37,232,26,"Tab  練習を飛ばして開戦",13,Navy,true,TextAnchor.MiddleCenter);
    }

    void BlockedHandsCaption()
    {
        const float w=550,h=106;
        float x=(ww-w)*.5f,y=hh-245;
        Plate(x+4,y+5,w,h,new Color(.12f,.10f,.15f,.35f),17);
        Plate(x,y,w,h,Hex("693C42"),17);
        Plate(x+16,y+20,57,62,Paper,10);
        Art(x+22,y+24,45,54,milkIcon);
        Caption(x+90,y+9,w-104,49,"両手がふさがって攻撃できない！",27,Color.white,true);
        Caption(x+91,y+59,w-105,32,"E または右クリックで哺乳瓶を置こう",20,Gold,true);
    }

    void ActionBar()
    {
        string hint,key=null;float progress=-1;
        var inReach=Training?null:Closest(foes,f=>f.actor.position,f=>f.hp>0,StrikeRange);
        if(stumble>0){hint="踏みつけでよろけた！";progress=1-stumble/.75f;}
        else if(placing>0){hint="哺乳瓶を床に置いている";progress=1-placing/.5f;}
        else if(petting!=null){hint=Names[petting.index]+"をなでている　"+Mathf.Min(5,Mathf.FloorToInt(petting.pet))+" / 5秒";progress=petting.pet/PetTime;}
        else if(Training && practiceTime>0)hint="ミルクが届いた！　ハートの行き先を見よう";
        else if(inReach!=null && (inReach.guard>0 || carrying))
        {
            key=carrying?"E":"F";
            hint=carrying?"E／右クリックで哺乳瓶を置こう":"F／左クリック　盾を割る";
        }
        else if(inReach!=null && inReach==milkCarrier){key="F";hint="F／左クリック　ミルクどろぼうを捕まえる";}
        else if(!carrying && Nearby(true)!=null){key="E";hint="E／右クリック 長押し　なでて味方に戻す";}
        else if(inReach!=null){key="F";hint="F / 左クリック　近くの敵を攻撃";}
        else if(!carrying && NearbyBottle()!=null)hint="E／右クリック　床の哺乳瓶を拾う";
        else if(phase==Phase.Prep && !firstDelivery)hint=carrying?"哺乳瓶を運んでいる　／　近づくと自動で配達":"WASD で矢印の先へ　／　屋台で自動受取";
        else if(carrying)hint=carriedMilk==MilkKind.Protein?"プロテインミルク運搬中　／　近づいて配達":"近づいて配達　／　E または右クリックで置く";
        else hint=phase==Phase.Prep?"屋台に近づくと、次のミルクを受け取れる":"F / 左クリック　近くの敵を押し返す";
        float w=510,x=(ww-w)/2,y=hh-97;
        Plate(x,y,w,61,Navy,15);Plate(x+9,y+9,44,43,key!=null?Gold:carrying?Aqua:Dark,9);
        if(key!=null)Caption(x+9,y+9,44,43,key,26,Navy,true,TextAnchor.MiddleCenter);
        else if(carrying)Art(x+8,y+7,47,47,milkIcon);else Caption(x+9,y+9,44,43,"手",19,Paper,true,TextAnchor.MiddleCenter);
        Caption(x+66,y+5,w-76,50,hint,17,Paper,true);
        if(progress>=0)Bar(x+66,y+49,w-83,4,progress,Gold,Dark);
        string keys="WASD 移動　Space ジャンプ　 マウス 視点　 ホイール 距離　 F／左クリック 攻撃　 E／右クリック お世話　 M マップ";
        Plate(ww*.5f-460,hh-32,920,26,Paper,12);Caption(ww*.5f-460,hh-32,920,26,keys,13,Navy,true,TextAnchor.MiddleCenter);
    }
    void EndingCaption()
    {
        bool win=phase==Phase.Victory;float y=hh*.77f;
        Plate(ww*.5f-259,y,518,68,Navy,17);
        Caption(ww*.5f-249,y+4,498,59,win?(candyTown?"クッキー将軍撃破！　2面クリア":"王冠ピーマン撃破！　1面クリア"):FortNames[fallen]+"が崩れる！",26,win?Gold:Pink,true,TextAnchor.MiddleCenter);
    }
    void CelebrationCaption()
    {
        const float width=650,height=133,y=25;
        float x=(ww-width)*.5f;
        if(elapsed<.32f)Plate(0,0,ww,hh,new Color(1,.95f,.84f,.68f*(1-elapsed/.32f)));
        Plate(x+4,y+5,width,height,new Color(.21f,.13f,.17f,.22f),20);
        Plate(x,y,width,height,Paper,20);
        Caption(x+22,y+10,width-44,25,"2面クリア　／　お菓子の町",15,Grey,true,TextAnchor.MiddleCenter);
        Caption(x+22,y+34,width-44,53,"お庭に平和が戻った！",38,Navy,true,TextAnchor.MiddleCenter);
        Caption(x+22,y+87,width-44,32,"みんなで力を合わせて、ふたつのおうちを守りました。",19,Navy,false,TextAnchor.MiddleCenter);
    }
    void PauseScreen()
    {
        Plate(0,0,ww,hh,new Color(.09f,.15f,.20f,.75f));float w=450,h=400,x=(ww-w)/2,y=(hh-h)/2;
        Plate(x,y,w,h,Paper,22);Caption(x+30,y+21,w-60,51,"ひとやすみ",31,Navy,true);
        Caption(x+30,y+72,w-60,27,"F／クリックで攻撃　・　ホイールでカメラ距離",15,Navy,true);
        Caption(x+30,y+98,w-60,43,"E／右クリックで瓶を置く・拾う。\n瓶を持っていないときに長押しで、赤ちゃんをなでる。",14,Grey);
        Caption(x+30,y+150,168,37,"BGM 音量   "+musicPercent+"%",17,Navy,true);
        Bar(x+193,y+165,105,8,musicPercent*.01f,Dark,Pale);
        if(Button(x+310,y+150,49,34,"−",Pale,Navy,21))SetMusicVolume(musicPercent-10);
        if(Button(x+372,y+150,49,34,"＋",Pale,Navy,21))SetMusicVolume(musicPercent+10);
        if(Button(x+28,y+203,w-56,49,"ゲームに戻る",Gold,Navy))TogglePause();
        if(Button(x+28,y+268,190,42,muted?"すべての音をオン":"すべての音をオフ",Aqua,Navy,15))muted=!muted;
        if(Button(x+232,y+268,190,42,"最初から遊ぶ",Pale,Navy,15))Begin();
        if(Button(x+28,y+333,w-56,42,"ゲームを終了",Navy,Paper,15))ExitGame();
    }
    void ResultScreen()
    {
        Plate(0,0,ww,hh,new Color(.10f,.16f,.20f,.74f));float w=548,h=392,x=(ww-w)/2,y=(hh-h)/2;
        bool win=phase==Phase.Finish;Plate(x,y,w,h,Paper,23);
        Caption(x+29,y+25,w-58,29,win?"2面クリア　／　お菓子の町":"防衛終了　／　"+(candyTown?"2面":"1面"),15,Grey,true);
        Caption(x+27,y+68,w-54,56,win?"お庭に平和が戻った！":FortNames[fallen]+"が壊れた",33,Navy,true);
        Caption(x+30,y+133,w-60,58,win?"クッキー将軍を撃破！\n使用アイテム  "+RewardNames[(int)reward-1]+"　／　ミルク奪還  "+milkRecoveries+"回":lastAttacker[fallen]+"の攻撃で耐久がゼロに。\n"+(petDamage[fallen]?"E または右クリックを5秒長押ししてなだめよう。":"おうちに迫る敵には、哺乳瓶を置いて F で攻撃。"),16,Grey);
        Plate(x+29,y+201,w-58,46,Navy,10);Caption(x+40,y+201,w-80,46,"配達  "+deliveries+"回     撃退  "+defeats+"体     盾破壊  "+lidsBroken+"枚",17,Paper,true,TextAnchor.MiddleCenter);
        if(win)
        {
            if(Button(x+29,y+266,w-58,49,"2面をもう一度遊ぶ",Gold,Navy,18))StartTown(reward);
        }
        else
        {
            if(Button(x+29,y+257,w-58,43,candyTown?"同じ報酬で2面を再挑戦":"満タンでバトルから再挑戦",Gold,Navy,18))RetryBattle();
            if(Button(x+29,y+311,235,44,candyTown?"1面から遊ぶ":"チュートリアルから",Pale,Navy,15))Begin();
        }
        float titleX=win?x+29:x+275,titleWidth=win?w-58:w-304;
        if(Button(titleX,y+(win?330:311),titleWidth,win?35:44,"タイトルへ",Pale,Navy,15))
            ReturnToTitle();
    }
    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
}
