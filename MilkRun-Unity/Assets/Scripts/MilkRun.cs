using System.Collections.Generic;
using UnityEngine;
using static ToyArt;
using static ToyGarden;

public partial class MilkRun : MonoBehaviour
{
    enum Phase { Title, Story, Prep, Wave, Boss, Rewards, Finish, Loss, Collapse, Victory, Celebration }
    enum MilkKind { Regular, Protein }
    enum Mood { Ally, Crying, Turned, Returning }
    class Child
    {
        public int index;
        public Transform actor, body;
        public Mood mood;
        public float food=15, tears, pet, attack, delay, joy, returnTime, landing;
        public float protein;
        public BabyExpression face;
        public int target;
    }
    class Foe
    {
        public Transform actor,body;
        public int side, waypoint=1, hp, maxHp;
        public bool boss;
        public float speed, strike, stagger, step, windup;
        public int kind, guard;
        public bool enraged, announcedApproach, announcedAttack;
        public float hitTime, stompWait, stomp, ringFade;
        public float guardFlash, shieldBreakTime;
        public string hitWord, line;
        public float lineUntil;
        public Transform lid,ring,loot;
        public Transform movingPart;
        public bool CookieCommander => boss && kind==CookieKind;
    }
    class Spark
    {
        public Transform mark, target;
        public Vector3 start, end;
        public float age,life;
        public bool heart;
        public Foe receiver;
    }
    class FloorBottle
    {
        public Transform actor;
        public MilkKind kind;
    }

    const float WaveLength=120, FullBottle=30, Grace=10, PetTime=5;
    const float StrikeRange=3.45f;
    const float HeartsRange=10.8f;
    const float DefaultCameraDistance=3.85f, NearCameraDistance=2.60f, FarCameraDistance=7.15f;
    const int Durability=100;
    readonly int[] health={Durability,Durability};
    readonly List<Child> children=new List<Child>();
    readonly List<Foe> foes=new List<Foe>();
    readonly List<Spark> sparks=new List<Spark>();
    readonly List<FloorBottle> bottles=new List<FloorBottle>();
    // Enemy waves overlap on the two roads. The clock runs once for the whole siege.
    // The broccoli introduction finishes before Grandpa arrives at 1:00 remaining.
    readonly float[] waves={0,5,12,19,22,31,36,43,45,52,55,WaveLength-60,66,70,77,81,85,88,93,98,101,105,109,112,115};
    readonly int[] types=  {0,1, 0, 2, 0, 0, 1, 0, 2, 1, 5,            3, 0, 3, 2, 1, 5, 3, 1, 0, 2, 3, 1, 0, 3};
    Transform cast, player, keeper, held, heldProtein;
    Camera cam;
    AudioSource sounds;
    AudioClip bottleClink,milkGulp;
    readonly Dictionary<int,AudioClip> notes=new Dictionary<int,AudioClip>();
    Phase phase;
    bool paused, carrying, muted, jumpReady=true, firstDelivery, bossDown, mapExpanded;
    MilkKind carriedMilk;
    int milkIssued;
    float elapsed, clock, height, vertical, yaw, pitch, walk, placing, cool, swing, noticeTime;
    int next, deliveries, defeats, rescues, lidsBroken;
    Child petting;
    string notice="";
    Vector3 camFocus;
    float cameraDistance=DefaultCameraDistance, cameraDistanceGoal=DefaultCameraDistance, cameraShoulder;
    bool Active => phase==Phase.Prep || phase==Phase.Wave || phase==Phase.Boss;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if(Object.FindAnyObjectByType<MilkRun>()==null) new GameObject("Milk Run").AddComponent<MilkRun>();
    }
    void Awake()
    {
        var old=Camera.main;if(old!=null) old.gameObject.SetActive(false);
        cam=new GameObject("Follow Camera").AddComponent<Camera>();cam.gameObject.tag="MainCamera";
        cam.gameObject.AddComponent<AudioListener>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.84f,.91f,.90f);cam.nearClipPlane=.14f;
        sounds=gameObject.AddComponent<AudioSource>();sounds.spatialBlend=0;
        bottleClink=Resources.Load<AudioClip>("Sounds/BottleClink");
        milkGulp=Resources.Load<AudioClip>("Sounds/MilkGulp");
        CreateMusic();
        CreateCareSounds();
        shieldCrack=CreateShieldCrack();
        waveCall=Resources.Load<AudioClip>("Sounds/WaveCall");
        LoadArena(false);
        CreateUI(); CreateOpeningArt(); ResetActors(); phase=Phase.Title; CursorMode(false); MoveCamera(true);
    }
    void ResetActors()
    {
        ResetCareSounds();
        if(cast!=null) {cast.gameObject.SetActive(false);Destroy(cast.gameObject);}
        cast=Root("Live pieces",null,Vector3.zero);children.Clear();foes.Clear();bottles.Clear();sparks.Clear();
        for(int i=0;i<3;i++)
        {
            var a=Root(Names[i],cast,Posts[i]);var b=Baby(a,V(0,.28f,0),Accents[i],i);
            a.localRotation=Quaternion.Euler(0,180,0);
            children.Add(new Child{index=i,actor=a,body=b,face=b.GetComponent<BabyExpression>(),delay=i*9f});
        }
        player=Root("Player",cast,V(0,0,-16.4f));keeper=Keeper(player,Vector3.zero);
        RigKeeper();
        held=Bottle(handR,V(0,-.36f,.22f),.97f);
        heldProtein=Bottle(handR,V(0,-.36f,.22f),.97f,true);
        SetCarrying(false);
        milkIssued=0;placing=0;petting=null;
        elapsed=0;next=0;vertical=height=cool=swing=0;yaw=0;pitch=6;paused=mapExpanded=false;ResetPresentation();
        health[0]=health[1]=Durability;firstDelivery=false;bossDown=false;
        deliveries=defeats=rescues=lidsBroken=0;
        king=null;fallenKing=null;bossReveal=fortTour=stumble=waveMessage=0;
        waveEntranceTime=0;waveLead=null;outpost=null;outpostTop=null;outpostRuins=null;
        ResetEncounterScenes();
        ResetTownActors();
        camFocus=player.position;MoveCamera(true);
    }
    void ReadStory()
    {
        phase=Phase.Story;storyPanel=0;
        Chime(660,.10f);
    }
    void Begin()
    {
        ResetEncounterLessons();
        reward=Reward.None;
        LoadArena(false);
        phase=Phase.Prep;ResetActors();CursorMode(true); Say(""); Chime(660);
    }
    void RetryBattle()
    {
        if(candyTown){StartTown(reward);return;}
        phase=Phase.Prep;
        ResetActors();
        foreach(var baby in children)
        {
            baby.food=FullBottle;
            baby.joy=1.6f;
        }
        CursorMode(true);
        Battle(false);
        Say("みんな満タン！　東西のおうちを守ろう");
    }
    void ReturnToTitle()
    {
        ResetEncounterLessons();
        reward=Reward.None;
        LoadArena(false);
        phase=Phase.Title;
        ResetActors();
        CursorMode(false);
    }
    void Battle(bool showEntrance=true)
    {
        mapExpanded=false;openingCountdown=0;RemoveSparring(false);
        phase=Phase.Wave;elapsed=0;next=0;
        foreach(var mark in trail)mark.gameObject.SetActive(false);
        Say("");waveMessage=4.5f;
        if(showEntrance)StartWaveEntrance();
        else
        {
            PlaceOutpost();
            BreakOutpost(false);
            var firstEnemy=Spawn(0,types[0],false);
            firstEnemy.actor.position=WaveGateStop();
            PointAt(firstEnemy.actor,Route(0,1));
            CueEnemyArrival(firstEnemy);
            next=1;
            if(!muted && waveCall!=null)sounds.PlayOneShot(waveCall,.45f);
        }
    }
    void Ending(Phase end)
    {
        phase=end;elapsed=0;paused=false;petting=null;placing=0;noticeTime=0;CursorMode(false);
        Chime(end==Phase.Loss?165:784);
    }
    void CursorMode(bool play)
    {
        Cursor.lockState=play?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!play;
    }
    void TogglePause()
    {
        paused=!paused;CursorMode(!paused);
    }
    void OnApplicationFocus(bool focused)
    {
        if(!focused && Active && !paused){paused=true;CursorMode(false);}
    }
    void OnDestroy(){ ReleaseEnemyPortraits();ReleaseOpeningArt();CursorMode(false); }
    void Update()
    {
        MixMusic(Mathf.Min(Time.unscaledDeltaTime,.10f));
        if(TryRewardTestShortcut(Input.GetKey(KeyCode.P),Input.GetKey(KeyCode.A),
            Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.A)))return;
        if(phase==Phase.Collapse || phase==Phase.Victory){AdvanceEnding(Mathf.Min(Time.deltaTime,.1f));return;}
        if(phase==Phase.Celebration){AdvanceCelebration(Mathf.Min(Time.deltaTime,.1f));return;}
        if(Active && Input.GetKeyDown(KeyCode.Escape)) {TogglePause();return;}
        if(Active && Input.GetKeyDown(KeyCode.M))mapExpanded=!mapExpanded;
        if(paused)return;
        if(phase==Phase.Rewards)
        {
            if(Input.GetKeyDown(KeyCode.Alpha1)||Input.GetKeyDown(KeyCode.Keypad1))SelectReward(Reward.Stork);
            else if(Input.GetKeyDown(KeyCode.Alpha2)||Input.GetKeyDown(KeyCode.Keypad2))SelectReward(Reward.ProteinMilk);
            else if(Input.GetKeyDown(KeyCode.Alpha3)||Input.GetKeyDown(KeyCode.Keypad3))SelectReward(Reward.Rattle);
            else if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))ConfirmReward();
            else if(Input.GetKeyDown(KeyCode.Escape))rewardPreview=Reward.None;
            return;
        }
        if(phase==Phase.Title || phase==Phase.Story)
        {
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if(phase==Phase.Title)ReadStory();
                else AdvanceStory();
            }
            return;
        }
        if(!Active)return;
        float dt=Mathf.Min(Time.deltaTime,.10f);
        if(!Directing)
        {
            AdjustCameraDistance(Input.mouseScrollDelta.y);
            MovePlayer(dt);
            bool carePressed=Input.GetKeyDown(KeyCode.E)||Input.GetMouseButtonDown(1);
            bool careHeld=Input.GetKey(KeyCode.E)||Input.GetMouseButton(1);
            HandleCare(dt,carePressed,careHeld,Input.GetKeyDown(KeyCode.F)||Input.GetMouseButtonDown(0));
        }
        StepWorld(dt,Input.GetKeyDown(KeyCode.Tab));
    }
    void StepWorld(float dt,bool startEarly)
    {
        if(paused)return;
        if(betrayalChild!=null){AdvanceBetrayalLesson(dt);return;}
        if(milkThiefIntro!=null){AdvanceMilkThiefIntro(dt);return;}
        clock+=dt;
        AdvanceEnemyArrival(dt);
        if(waveEntranceTime>0){AdvanceWaveEntrance(dt);return;}
        if(bossReveal>0){AdvanceBossReveal(dt);return;}
        elapsed+=dt;
        cool=Mathf.Max(0,cool-dt);swing=Mathf.Max(0,swing-dt);noticeTime-=dt;
        stumble=Mathf.Max(0,stumble-dt);waveMessage=Mathf.Max(0,waveMessage-dt);
        blockedHandsTime=Mathf.Max(0,blockedHandsTime-dt);
        bool tourJustEnded=false;
        if(fortTour>0)
        {
            fortTour=Mathf.Max(0,fortTour-dt);
            tourJustEnded=fortTour<=0;
            if(tourJustEnded)StartSparring();
        }
        UpdatePractice(dt);TowerMotion(dt);
        if(!tourJustEnded)UpdateShieldLesson(dt);
        if(Training && firstDelivery && !Directing && !tourJustEnded &&
            (startEarly || AdvanceOpeningCountdown(dt)))
        {
            Battle();return;
        }
        if(phase==Phase.Wave)
        {
            float[] schedule=candyTown?TownWaves:waves;
            int[] enemyKinds=candyTown?TownFoes:types;
            while(next<schedule.Length && elapsed>=schedule[next])
            {
                var foe=Spawn(next%2,enemyKinds[next],false);next++;
                CueEnemyArrival(foe);
                if(milkThiefIntro!=null)return;
            }
        }
        if(candyTown)UpdateTownReward(dt);
        UpdateChildren(dt);
        if(betrayalChild!=null)return;
        UpdatePettingHand();
        petFrame=Mathf.MoveTowards(petFrame,petting!=null?1:0,dt*2.6f);
        UpdateSparks(dt);UpdateFoes(dt);UpdateShieldBreak(dt);
        UpdateGuidance();
        if(health[0]<=0 || health[1]<=0)StartEnding(false);
        else if(phase==Phase.Wave && Remaining<=0)StartBoss();
        else if(phase==Phase.Boss && bossDown)StartEnding(true);
    }
    void LateUpdate()
    {
        if(!paused){TurnSails(Time.deltaTime);if(Active||phase==Phase.Title||phase==Phase.Story)MoveCamera(false);}
    }
    void AdjustCameraDistance(float wheel)
    {
        cameraDistanceGoal=Mathf.Clamp(cameraDistanceGoal-wheel*.50f,NearCameraDistance,FarCameraDistance);
    }
    void MoveCamera(bool snap)
    {
        if(cam==null||player==null)return;
        if(phase==Phase.Title || phase==Phase.Story)
        {
            float t=Time.unscaledTime*.08f;
            cam.fieldOfView=42;cam.transform.position=V(12.7f+Mathf.Sin(t)*.7f,10.7f,-16);
            cam.transform.LookAt(V(-1.2f,1.1f,-1.4f));ClearSightline(cam.transform.position,player.position,false);return;
        }
        if(betrayalChild!=null){ShowBetrayalLesson();return;}
        if(milkThiefIntro!=null){ShowMilkThiefIntro();return;}
        if(fortTour>0){ShowCastles();return;}
        if(bossReveal>0){ShowBoss();return;}
        if(waveEntranceTime>0){ShowWaveEntrance();return;}
        if(shieldLessonTime>0){ShowShieldLesson();return;}
        if(ShowingFoodLesson){ShowFedBaby();return;}
        if(practiceTime>0 && practicePepper!=null)
        {
            cam.fieldOfView=41;cam.transform.position=practiceEye;
            cam.transform.rotation=Quaternion.LookRotation(practiceLook-practiceEye);
            ClearSightline(practiceEye,practiceLook,true);return;
        }
        Vector3 focus=player.position+V(0,1.62f+height*.72f,0);
        camFocus=snap?focus:Vector3.Lerp(camFocus,focus,1-Mathf.Exp(-17*Time.deltaTime));
        Quaternion orbit=Quaternion.Euler(pitch,yaw,0);float lens=Mathf.Lerp(60,66,Mathf.InverseLerp(3,8.2f,runSpeed));
        float smooth=1-Mathf.Exp(-13*Time.unscaledDeltaTime);
        cameraDistance=snap?cameraDistanceGoal:Mathf.Lerp(cameraDistance,cameraDistanceGoal,smooth);
        float shoulderGoal=sparring!=null?1:0;
        cameraShoulder=snap?shoulderGoal:Mathf.Lerp(cameraShoulder,shoulderGoal,smooth);
        cam.fieldOfView=snap?lens:Mathf.Lerp(cam.fieldOfView,lens,1-Mathf.Exp(-5*Time.deltaTime));
        float lift=.16f+cameraShoulder*.62f+Mathf.Max(0,cameraDistance-DefaultCameraDistance)*.18f;
        Vector3 kick=V(Mathf.Sin(shieldBump*94)*.26f,Mathf.Sin(shieldBump*110)*.18f,0)*shieldBump;
        cam.transform.position=camFocus+orbit*(V(.70f+cameraShoulder*1.24f,lift,-cameraDistance)+kick);
        cam.transform.rotation=Quaternion.LookRotation(camFocus+orbit*V(.82f-cameraShoulder*.52f,.13f,7.0f)-cam.transform.position);
        Vector3 subject=player.position+Vector3.up*(height+1.22f);
        if(petFrame>0)
        {
            float blend=Mathf.SmoothStep(0,1,petFrame);
            cam.transform.position=Vector3.Lerp(cam.transform.position,petEye,blend);
            cam.transform.rotation=Quaternion.Slerp(cam.transform.rotation,Quaternion.LookRotation(petLook-petEye),blend);
            subject=Vector3.Lerp(subject,petLook,blend);
        }
        ClearSightline(cam.transform.position,subject,true);
    }
    void MovePlayer(float dt)
    {
        float mx=Input.GetAxisRaw("Mouse X"),my=Input.GetAxisRaw("Mouse Y");
        yaw+=mx*2.1f+(Held(KeyCode.RightArrow)-Held(KeyCode.LeftArrow))*105*dt;
        pitch=Mathf.Clamp(pitch-my*1.4f+(Held(KeyCode.DownArrow)-Held(KeyCode.UpArrow))*55*dt,-12,24);
        float x=Held(KeyCode.D)-Held(KeyCode.A),z=Held(KeyCode.W)-Held(KeyCode.S);
        Vector3 dir=Quaternion.Euler(0,yaw,0)*Vector3.ClampMagnitude(V(x,0,z),1);
        if(Input.GetKeyDown(KeyCode.Space) && height<=.015f && jumpReady && placing<=0 && petting==null){vertical=6.5f;jumpReady=false;Chime(465,.055f);}
        if(!Input.GetKey(KeyCode.Space))jumpReady=true;
        vertical-=17*dt;height+=vertical*dt;if(height<=0){height=0;vertical=0;}
        Vector3 pos=player.position;
        float targetSpeed=dir.magnitude*(carrying?7.6f:8.2f);
        if(stumble>0)targetSpeed*=.30f;
        runSpeed=Mathf.MoveTowards(runSpeed,targetSpeed,dt*40);
        if(placing<=0 && petting==null)pos+=dir*dt*runSpeed;
        Avoid(ref pos,MilkPoint,1.12f);foreach(var a in Forts)Avoid(ref pos,a,1.04f);
        if(height<.80f)foreach(var f in foes)Avoid(ref pos,f.actor.position,f.boss?1.06f:.64f);
        pos.x=Mathf.Clamp(pos.x,-WalkX,WalkX);pos.z=Mathf.Clamp(pos.z,-WalkZ,WalkZ);player.position=pos;
        if(dir.sqrMagnitude>.01f && petting==null && placing<=0)player.rotation=Quaternion.Slerp(player.rotation,Quaternion.LookRotation(dir),1-Mathf.Exp(-18*dt));
        walk+=dir.magnitude*dt*(9+runSpeed*.65f);AnimateKeeper(dir.magnitude);
        float bob=dir.magnitude*.055f*Mathf.Abs(Mathf.Sin(walk));
        keeper.localPosition=V(0,height+(petting!=null?-.25f:bob)-(placing>0?.17f:0),0);
        keeper.localRotation=Quaternion.Euler(petting!=null?12:0,swing>0?Mathf.Sin(swing/.23f*Mathf.PI)*32:0,Mathf.Sin(walk)*dir.magnitude*2);
    }
    static float Held(KeyCode k)=>Input.GetKey(k)?1:0;
    static void Avoid(ref Vector3 pos,Vector3 obstacle,float radius)
    {
        Vector3 d=pos-obstacle;d.y=0;float dist=d.magnitude;
        if(dist<radius)pos+= (dist>.001f?d/dist:Vector3.back)*(radius-dist);
    }
    T Closest<T>(List<T> items,System.Func<T,Vector3> where,System.Predicate<T> allowed,float radius) where T:class
    {
        T found=null;float limit=radius*radius;
        foreach(T item in items)if(allowed(item)){float d=(where(item)-player.position).sqrMagnitude;if(d<limit){limit=d;found=item;}}
        return found;
    }
    Child Nearby(bool turned) => Closest(children,b=>b.actor.position,b=>turned?b.mood==Mood.Turned:(b.mood==Mood.Crying||b.mood==Mood.Ally)&&b.food<FullBottle-.1f,2.6f);
    FloorBottle NearbyBottle()=>Closest(bottles,b=>b.actor.position,b=>true,1.9f);
    bool AtMilk => Vector3.Distance(player.position,MilkPoint)<2.75f;
    void SetCarrying(bool val,MilkKind kind=MilkKind.Regular)
    {
        carrying=val;
        if(!val)blockedHandsTime=0;
        carriedMilk=val?kind:MilkKind.Regular;
        held.gameObject.SetActive(val && kind==MilkKind.Regular);
        heldProtein.gameObject.SetActive(val && kind==MilkKind.Protein);
    }
    void AddFloorBottle(Vector3 at,MilkKind kind=MilkKind.Regular)
    {
        bottles.Add(new FloorBottle{actor=Bottle(cast,at,1.05f,kind==MilkKind.Protein),kind=kind});
    }
    void PickUpMilk(MilkKind kind,bool fromCounter)
    {
        SetCarrying(true,kind);
        bool protein=kind==MilkKind.Protein;
        MilkSound(protein?proteinPickup:bottleClink);
        if(protein)Say(fromCounter?"プロテインミルクを受け取った！":"プロテインミルクを拾った！");
        else if(fromCounter)Say("哺乳瓶を受け取った！");
    }
    void MilkSound(AudioClip clip)
    {
        if(!muted && clip!=null){DuckMusic(.53f);sounds.PlayOneShot(clip,.66f);}
    }
    void Feed(Child b,bool fromStork=false)
    {
        bool first=!firstDelivery && phase==Phase.Prep;
        bool protein=!fromStork && carriedMilk==MilkKind.Protein;
        float foodBefore=b.food;
        StopCry(b);
        b.food=FullBottle;b.tears=0;b.mood=Mood.Ally;b.joy=2.2f;
        deliveries++;firstDelivery=true;
        if(!fromStork)SetCarrying(false);
        if(protein)
        {
            b.protein=ProteinLength;
            b.face.SetPoweredUp(true);
        }
        Fx(b.actor.position+Vector3.up,b.actor,null,1, false);
        Say(protein?Names[b.index]+"がプロテインミルクで連射！":Names[b.index]+"がごきげん！　ミルク満タン");
        MilkSound(milkGulp);
        if(first)StartPractice(b,foodBefore);
    }
    void HandleCare(float dt,bool actionPressed,bool actionHeld,bool attackPressed)
    {
        if(TryShieldLesson(attackPressed))return;
        bool inside=AtMilk;
        bool entered=inside && !wasAtMilk;wasAtMilk=inside;
        if(entered && !carrying && placing<=0 && petting==null)
        {
            if(MilkAvailable)
            {
                MilkKind kind=reward==Reward.ProteinMilk && ++milkIssued%3==0?MilkKind.Protein:MilkKind.Regular;
                PickUpMilk(kind,true);
                return;
            }
            Say("ミルクがない！　どろぼうを捕まえよう");
        }
        if(placing>0)
        {
            placing-=dt;
            if(placing<=0)
            {
                AddFloorBottle(player.position+player.forward*.45f,carriedMilk);
                SetCarrying(false);
                if(!muted)sounds.PlayOneShot(bottleSetDown,.58f);
            }
            return;
        }
        if(!carrying)
        {
            var b=Nearby(true);
            if(b!=null && actionHeld && height<.16f)
            {
                if(petting!=b && petting!=null)petting.pet=0;
                petting=b;b.pet+=dt;
                Vector3 stand=player.position-b.actor.position;stand.y=0;
                if(stand.sqrMagnitude>.1f)player.position=Vector3.MoveTowards(player.position,b.actor.position+stand.normalized*.94f,4*dt);
                if(b.pet>=PetTime)
                {
                    b.mood=Mood.Returning;b.returnTime=b.landing=0;b.joy=0;
                    b.face.SetDevil(false);PostLights[b.index].gameObject.SetActive(true);
                    Burst(b.actor.position+Vector3.up*1.55f,5,true);b.food=FullBottle;b.tears=b.pet=0;petting=null;rescues++;
                    Chime(988);Say(Names[b.index]+"が落ち着いた！　持ち場へ戻るよ");
                }
                return;
            }
            if(petting!=null){petting.pet=0;petting=null;}
            if(attackPressed)Attack();
        }
        if(carrying)
        {
            var receiver=Nearby(false);
            if(receiver!=null){Feed(receiver);return;}
            if(attackPressed && !actionPressed)
            {
                if(blockedHandsTime<1.3f)Chime(207,.11f);
                blockedHandsTime=2.8f;
            }
        }
        if(!actionPressed)return;
        if(carrying)
        {
            placing=.5f;
        }
        else
        {
            FloorBottle onFloor=NearbyBottle();
            if(onFloor!=null)
            {
                bottles.Remove(onFloor);
                Destroy(onFloor.actor.gameObject);
                PickUpMilk(onFloor.kind,false);
            }
        }
    }
    void Attack()
    {
        if(cool>0 || stumble>0)return;
        cool=.44f;swing=.23f;Chime(240,.075f);
        Vector3 start=player.position+V(0,1.0f+height,0);
        Foe nearest=Closest(foes,f=>f.actor.position,f=>f.hp>0,StrikeRange);
        Vector3 dir=nearest!=null?(nearest.actor.position-player.position).normalized:Quaternion.Euler(0,yaw,0)*Vector3.forward;
        if(dir.sqrMagnitude>.01f)player.rotation=Quaternion.LookRotation(dir);
        for(int i=-1;i<=1;i++){Vector3 d=Quaternion.Euler(0,i*24,0)*dir;Fx(start,null,start+d*2.25f,.32f,false);}
        foreach(var f in foes)
        {
            Vector3 to=f.actor.position-player.position;
            if(f.hp>0 && to.magnitude<StrikeRange && Vector3.Dot(dir,to.normalized)>.34f)
            {
                StrikeFoe(f,true);
                if(!f.boss && f!=sparring)
                {
                    bool heavy=f.kind==BroccoliKind;
                    f.windup=0;f.stagger=heavy?.18f:.34f;
                    f.actor.position+=to.normalized*(heavy?.28f:1.05f);
                }
            }
        }
    }
    Foe Spawn(int side,int type,bool boss,bool shielded=true)
    {
        bool commander=boss && type==CookieKind;
        var a=Root(commander?"クッキー将軍":boss?"王冠ピーマン":FoeNames[type],cast,Route(side,0));
        Transform body=type switch
        {
            MilkThiefKind=>MilkThief(a),
            BroccoliKind=>Broccoli(a),
            CookieKind=>commander?CookieGeneral(a):CookieSoldier(a),
            _=>Enemy(a,Vector3.zero,type,boss)
        };
        var f=new Foe{actor=a,body=body,side=side,kind=type,boss=boss,hp=commander?TownBossHealth:boss?BossHealth:FoeHP[type],speed=commander?2.05f:boss?1.75f:FoeSpeed[type],step=next*.73f};
        f.movingPart=f.body.Find(type==0?"Paw":type==1?"Plunger":type==3?"Cane":type==CookieKind?(commander?"Candy baton":"Cookie wheel"):"Stem");
        f.maxHp=f.hp;
        if(type==2 && shielded)
        {
            f.guard=boss?3:1;
            f.lid=ToyArt.PanLid(f.body);
        }
        if(boss){f.ring=MakeStompRing(a);f.stompWait=4.8f;}
        if(type==MilkThiefKind)
        {
            f.loot=Bottle(body,V(0,1.41f,-.39f),.79f);
            f.loot.gameObject.SetActive(false);
        }
        foes.Add(f);return f;
    }
    void UpdateChildren(float dt)
    {
        foreach(var b in children)
        {
            Vector3 p=b.actor.position;
            b.attack-=dt;
            if(b.protein>0)
            {
                b.protein=Mathf.Max(0,b.protein-dt);
                if(b.protein<=0)b.face.SetPoweredUp(false);
            }
            if(phase!=Phase.Prep && b.mood==Mood.Ally)
            {
                if(b.delay>0)b.delay-=dt;
                else
                {
                    b.food=Mathf.Max(0,b.food-dt);
                    if(b.food<=0)
                    {
                        b.mood=Mood.Crying;
                        b.tears=0;
                        PlayCry(b);
                        Say(Names[b.index]+"が泣き始めた！");
                    }
                }
            }
            if(b.mood==Mood.Crying)
            {
                float before=b.tears;
                b.tears+=dt;
                if(before<Grace*.5f && b.tears>=Grace*.5f)PlayCry(b);
                if(b.tears>=Grace)
                {
                    if(!betrayalLessonSeen){StartBetrayalLesson(b);return;}
                    TurnBaby(b);
                }
            }
            if(b.mood==Mood.Returning)
            {
                b.returnTime+=dt;
                Vector3 home=Posts[b.index];WalkTo(b.actor,home,3.9f,dt);
                if((b.actor.position-home).sqrMagnitude<.001f)
                {
                    b.actor.position=home;b.landing+=dt;
                    b.actor.rotation=Quaternion.Slerp(b.actor.rotation,Quaternion.Euler(0,180,0),1-Mathf.Exp(-10*dt));
                    if(b.landing>=.50f)
                    {
                        b.mood=Mood.Ally;b.joy=1;b.actor.rotation=Quaternion.Euler(0,180,0);
                        PostLights[b.index].gameObject.SetActive(false);Burst(home+Vector3.up,3,true);
                    }
                }
            }
            if(b.mood==Mood.Turned && petting!=b)
            {
                Vector3 hit=Forts[b.target];Vector3 stand=hit+Vector3.forward*1.8f;
                if(Vector3.Distance(p,stand)>1.75f)WalkTo(b.actor,stand,1.9f,dt);
                else if(b.attack<=0){b.attack=2.45f;HitTower(b.target,2,Names[b.index],true);Fx(p+Vector3.up,null,hit+Vector3.up,.65f,true,true);}
            }
            else if(b.mood==Mood.Ally && b.attack<=0)
            {
                Foe target=null;float best=HeartsRange*HeartsRange;
                foreach(var f in foes){float d=(f.actor.position-p).sqrMagnitude;if(f!=sparring&&f!=practicePepper&&f.hp>0&&d<best){best=d;target=f;}}
                if(target!=null)
                {
                    b.actor.rotation=Quaternion.LookRotation((target.actor.position-p).normalized);
                    b.attack=b.protein>0?.84f:1.35f;
                    Fx(p+Vector3.up*1.23f,target.actor,null,b.protein>0?.75f:.58f,true).receiver=target;
                }
            }
            AnimateChild(b,dt);
            float baseY=b.mood==Mood.Ally||b.mood==Mood.Crying?.28f:0;
            b.body.localPosition=V(0,baseY+.035f*Mathf.Sin(clock*3+b.index)+(b.joy>0?.23f*Mathf.Abs(Mathf.Sin(b.joy*7)):0),0);
            if(b.mood==Mood.Returning)
            {
                float leap=b.landing>0?.28f+.29f*Mathf.Sin(Mathf.Clamp01(b.landing/.50f)*Mathf.PI):.05f+.30f*Mathf.Abs(Mathf.Sin(b.returnTime*9));
                b.body.localPosition=Vector3.up*leap;
            }
            b.body.localRotation=Quaternion.Euler(b.mood==Mood.Returning&&b.landing==0?12:0,0,b.mood==Mood.Returning?Mathf.Sin(b.returnTime*9)*9:b.mood==Mood.Crying?Mathf.Sin(clock*12)*10:b.mood==Mood.Turned?Mathf.Sin(clock*7)*4:Mathf.Sin(clock*2+b.index)*2);
        }
    }
    static void WalkTo(Transform t,Vector3 target,float speed,float dt)
    {
        Vector3 d=target-t.position;d.y=0;if(d.sqrMagnitude<.0001f)return;
        t.rotation=Quaternion.Slerp(t.rotation,Quaternion.LookRotation(d),1-Mathf.Exp(-10*dt));
        t.position=Vector3.MoveTowards(t.position,target,speed*dt);
    }
    void UpdateFoes(float dt)
    {
        for(int i=foes.Count-1;i>=0;i--)
        {
            var f=foes[i];
            if(f==practicePepper)continue;
            f.guardFlash=Mathf.Max(0,f.guardFlash-dt);
            f.shieldBreakTime=Mathf.Max(0,f.shieldBreakTime-dt);
            if(f==sparring)
            {
                if(f.hp<=0){RemoveSparring(true);continue;}
                f.step+=dt*4;f.hitTime=Mathf.Max(0,f.hitTime-dt);
                if(shieldLessonTime<=0)PointAt(f.actor,player.position);
                FoePose(f,0);continue;
            }
            if(f.hp<=0)
            {
                RestoreMilk(f);
                defeats++;if(f.boss){bossDown=true;fallenKing=f.actor;f.ring.gameObject.SetActive(false);Chime(990);}
                Burst(f.actor.position+Vector3.up*.9f,3,false);
                Fx(f.actor.position+Vector3.up*.6f,null,f.actor.position+Vector3.up*1.7f,.5f,false);
                if(!f.boss)Destroy(f.actor.gameObject);
                foes.RemoveAt(i);continue;
            }
            f.step+=dt*7;f.hitTime=Mathf.Max(0,f.hitTime-dt);
            if(!f.announcedApproach && f.waypoint<4 &&
                (f.waypoint>=2 || (f.actor.position-player.position).sqrMagnitude<=144f))
                SpeakOnApproach(f);
            if(f.boss && BossStomp(f,dt))continue;
            if(f.stagger>0){f.stagger-=dt;FoePose(f,0);continue;}
            if(f.kind==MilkThiefKind){UpdateMilkThief(f,dt);continue;}
            Vector3 dest=Route(f.side,f.waypoint);
            if((f.actor.position-dest).sqrMagnitude>.22f)
            { f.windup=0;WalkTo(f.actor,dest,f.speed,dt);FoePose(f,0); }
            else if(f.waypoint<4){f.waypoint++;FoePose(f,0);}
            else
            {
                f.actor.rotation=Quaternion.LookRotation((Forts[f.side]-f.actor.position).normalized);
                if(f.windup>0)
                {
                    float before=f.windup;
                    f.windup=Mathf.Max(0,f.windup-dt);
                    FoePose(f,1-f.windup/AttackLength);
                    if(before>ImpactTime && f.windup<=ImpactTime){
                        HitTower(f.side,EnemyDamage(f),EnemyName(f),false);
                        Fx(f.actor.position+Vector3.up,null,Forts[f.side]+Vector3.up,.4f,false);
                        Chime(175+f.kind*44,.12f);
                    }
                }
                else
                {
                    FoePose(f,0);
                    f.strike-=dt;
                    if(f.strike<=0)
                    {
                        f.windup=AttackLength;
                        f.strike=f.boss?.75f:1.05f;
                        if(!f.announcedAttack)
                        {
                            f.announcedAttack=true;
                            Speak(f,f.CookieCommander?"この町はわれらのもの！":f.boss?"おうちをぺちゃんこだ！":AttackLines[f.kind]);
                        }
                    }
                }
            }
        }
    }
    Spark Fx(Vector3 start,Transform target,Vector3? end,float size,bool isHeart,bool turned=false,float lifetime=0)
    {
        var t=isHeart?Heart(cast,start,size,turned?"BA2438":Rose):Ball(cast,start.x,start.y,start.z,.13f,.13f,.13f,Honey);
        var shot=new Spark{mark=t,start=start,target=target,end=end??(target!=null?target.position+Vector3.up:start+Vector3.up),life=lifetime>0?lifetime:isHeart?.40f:.25f,heart=isHeart};
        sparks.Add(shot);return shot;
    }
    void UpdateSparks(float dt)
    {
        for(int i=sparks.Count-1;i>=0;i--)
        {
            var s=sparks[i];s.age+=dt;float k=s.age/s.life;
            if(k>=1)
            {
                if(s.receiver!=null && s.receiver.hp>0 && Active)
                {
                    if(s.receiver.guard>0)
                    {
                        s.receiver.guardFlash=.70f;s.receiver.hitWord="盾でハートを弾いた！";
                        if(s.receiver==sparring)shieldLessonBlocked=true;
                        Vector3 rebound=(s.start-s.end).normalized;
                        s.receiver=null;s.target=null;s.start=s.end;
                        s.end=s.start+rebound*2.2f+Vector3.up*.70f;
                        s.age=0;s.life=.32f;Chime(310,.12f);continue;
                    }
                    StrikeFoe(s.receiver,false);
                    if(s.receiver==practicePepper && practicePepper.hp==0)
                    {
                        Chime(990);practiceChild.joy=2;Burst(practicePepper.actor.position+Vector3.up,6,true);
                    }
                }
                Destroy(s.mark.gameObject);sparks.RemoveAt(i);continue;
            }
            if(s.receiver!=null && s.receiver.guard>0 && s.receiver.lid!=null)
                s.end=s.receiver.lid.position;
            else if(s.target!=null)s.end=s.target.position+Vector3.up*(s.receiver!=null && s.receiver.boss?1.54f:.85f);
            s.mark.position=Vector3.Lerp(s.start,s.end,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*.28f);
            s.mark.rotation=cam.transform.rotation;
        }
    }
    void Say(string msg){notice=msg;noticeTime=string.IsNullOrEmpty(msg)?0:3.3f;}
    void Chime(int hz,float volume=.15f)
    {
        if(muted||sounds==null)return;
        if(!notes.TryGetValue(hz,out AudioClip clip))
        {
            const int sr=22050,n=5200;float[] data=new float[n];
            for(int i=0;i<n;i++){float t=(float)i/sr;data[i]=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Min(1,t*150)*Mathf.Exp(-t*23)*.64f;}
            clip=AudioClip.Create("Toy bell "+hz,n,1,sr,false);clip.SetData(data,0);notes[hz]=clip;
        }
        sounds.PlayOneShot(clip,volume);
    }
}
