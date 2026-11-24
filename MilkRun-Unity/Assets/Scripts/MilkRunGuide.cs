using UnityEngine;
using static ToyGarden;
using static ToyArt;

public partial class MilkRun
{
    enum TaskKind { Milk, Deliver, Practice, Sparring, Ready }
    struct Objective
    {
        public TaskKind kind;
        public Vector3 at;
        public string title, place;
        public Objective(TaskKind kind,Vector3 at,string title,string place)
        {this.kind=kind;this.at=at;this.title=title;this.place=place;}
    }
    Objective objective;
    readonly Transform[] trail=new Transform[6];
    static readonly string[] LandNames={"りんごの木","風車","黄色いアーチ"};
    bool wasAtMilk;
    float runSpeed,practiceTime,blockedHandsTime;
    Transform leftArm,handR,leftLeg,rightLeg;
    Foe practicePepper;
    Child practiceChild;
    const float FoodLessonLength=2.04f, HeartFlight=.42f, PracticeFallLength=.54f, PracticeResultLength=1.65f;
    const float LaunchAt=HeartFlight+PracticeResultLength, PracticeLength=FoodLessonLength+.08f+LaunchAt;
    float lessonFoodBefore;
    bool ShowingFoodLesson => practiceTime>PracticeLength-FoodLessonLength;
    Vector3 practiceEye,practiceLook,petEye,petLook;
    float petFrame;
    bool Training => phase==Phase.Prep;
    Foe sparring;
    const float OpeningCountdownLength=5f;
    float openingCountdown;
    const float ShieldLessonLength=3.4f;
    float shieldLessonTime;
    bool shieldLessonBlocked, shieldLessonSeen;
    Vector3 sparringEntryPosition;
    Transform petArm,upperSleeve,forearm,palm;


    void ResetPresentation()
    {
        wasAtMilk=false;runSpeed=practiceTime=petFrame=blockedHandsTime=0;practicePepper=null;practiceChild=null;
        sparring=null;openingCountdown=0;
        shieldLessonTime=0;shieldLessonBlocked=shieldLessonSeen=false;
        ResetShieldBreak();
        ResetTowers();
        foreach(var post in PostLights)post.gameObject.SetActive(false);
        for(int i=0;i<trail.Length;i++)
        {
            var arrow=Root("Destination chevron",cast,Vector3.zero);trail[i]=arrow;
            for(int side=-1;side<=1;side+=2)
            {
                var shadow=Box(arrow,side*.15f,.01f,-.02f,.16f,.035f,.51f,Ink);shadow.localRotation=Quaternion.Euler(0,-side*44,0);
                var mark=Box(arrow,side*.15f,.04f,0,.13f,.035f,.46f,Honey);mark.localRotation=shadow.localRotation;
            }
            arrow.gameObject.SetActive(false);
        }
        objective=new Objective(TaskKind.Milk,MilkPoint,"屋台に近づいて、ミルクを取ろう","ミルク屋台");
    }
    void RigKeeper()
    {
        leftArm=keeper.Find("Arm L");handR=keeper.Find("Arm R");
        leftLeg=keeper.Find("Leg L");rightLeg=keeper.Find("Leg R");
        petArm=Root("Stroking arm",keeper,Vector3.zero);
        upperSleeve=Ball(petArm,0,0,0,.28f,.4f,.28f,"D6B8E6");
        forearm=Ball(petArm,0,0,0,.21f,.4f,.21f,Skin);
        palm=Root("Open hand",petArm,Vector3.zero);
        Ball(palm,0,0,0,.29f,.16f,.30f,Skin);
        Ball(palm,-.135f,.015f,-.035f,.12f,.13f,.17f,Skin);
        petArm.gameObject.SetActive(false);
    }
    void AnimateKeeper(float moving)
    {
        if(petting!=null)
        {
            leftLeg.localRotation=Quaternion.Euler(-36,0,-8);
            rightLeg.localRotation=Quaternion.Euler(51,0,8);
            leftArm.localRotation=Quaternion.Euler(-50,0,-12);
            return;
        }
        float stride=Mathf.Sin(walk)*moving*Mathf.Min(1,runSpeed/4);
        leftLeg.localRotation=Quaternion.Euler(stride*33,0,0);
        rightLeg.localRotation=Quaternion.Euler(-stride*33,0,0);
        leftArm.localRotation=RattleEquipped
            ?Quaternion.Euler(swing>0?-85:-18-stride*15,0,-16)
            :Quaternion.Euler(-stride*28,0,0);
        handR.localRotation=Quaternion.Euler(carrying?-40:swing>0 && !RattleEquipped?-85:stride*28,0,0);
    }
    static void PlaceLimb(Transform piece,Vector3 start,Vector3 end,float width)
    {
        piece.position=(start+end)*.5f;
        piece.rotation=Quaternion.FromToRotation(Vector3.up,end-start);
        piece.localScale=V(width,(end-start).magnitude+width*.45f,width);
    }

    void UpdatePettingHand()
    {
        bool active=petting!=null;
        if(petArm.gameObject.activeSelf!=active)
        {petArm.gameObject.SetActive(active);handR.gameObject.SetActive(!active);}
        if(!active)return;
        petLook=(player.position+petting.actor.position)*.5f+Vector3.up*1.23f;
        petEye=petLook+player.right*3.4f-player.forward*1.35f+Vector3.up*.82f;
        float phaseOfStroke=petting.pet*Mathf.PI*2*1.14f;
        Transform baby=petting.body;
        Vector3 shoulder=handR.position;
        Vector3 contact=baby.TransformPoint(V(.17f+.25f*Mathf.Sin(phaseOfStroke),
            1.87f+.09f*(1-Mathf.Cos(phaseOfStroke)),.24f+.055f*Mathf.Cos(phaseOfStroke)));
        Vector3 elbow=Vector3.Lerp(shoulder,contact,.54f)+player.right*.25f-Vector3.up*.19f;
        PlaceLimb(upperSleeve,shoulder,elbow,.27f);PlaceLimb(forearm,elbow,contact,.19f);
        palm.position=contact;palm.rotation=player.rotation*Quaternion.Euler(-8,0,12*Mathf.Cos(phaseOfStroke));
    }

    void StartPractice(Child b,float foodBefore)
    {
        practiceChild=b;practiceTime=PracticeLength;lessonFoodBefore=foodBefore;
        b.joy=FoodLessonLength+.3f;
        // Each lane has a clear strip facing south. Stage the shot in that strip
        // regardless of which side the player approached from.
        float side=b.index==0?1:-1;
        Vector3 at=Posts[b.index]+V(side*3.60f,0,.20f);
        practicePepper=Spawn(0,2,false,shielded:false);
        practicePepper.actor.position=at;
        practicePepper.hp=practicePepper.maxHp=2;
        practiceLook=(b.actor.position+at)*.5f+Vector3.up*1.12f;
        practiceEye=practiceLook+V(0,1.38f,-6.25f);
        PointAt(practicePepper.actor,practiceEye);
        PointAt(b.actor,b.actor.position+Vector3.back);keeper.gameObject.SetActive(false);
        Say("");
    }
    void UpdatePractice(float dt)
    {
        if(practicePepper==null)return;
        float prev=practiceTime;practiceTime=Mathf.Max(0,practiceTime-dt);
        if(prev>PracticeLength-FoodLessonLength && !ShowingFoodLesson)
            PointAt(practiceChild.actor,practicePepper.actor.position);
        if(prev>LaunchAt&&practiceTime<=LaunchAt)
        {
            Fx(practiceChild.actor.position+Vector3.up*1.44f,practicePepper.actor,null,.86f,true,false,HeartFlight).receiver=practicePepper;
            Chime(840,.09f);
        }
        if(practicePepper.hp<=0)
        {
            float fall=Mathf.SmoothStep(0,1,Mathf.InverseLerp(PracticeResultLength,PracticeResultLength-PracticeFallLength,practiceTime));
            practicePepper.body.localRotation=Quaternion.Euler(-82*fall,0,0);
        }
        if(practiceTime<=0)
        {
            foes.Remove(practicePepper);Destroy(practicePepper.actor.gameObject);practicePepper=null;
            keeper.gameObject.SetActive(true);fortTour=FortTourLength;Chime(660);
        }
    }
    static void PointAt(Transform actor,Vector3 point)
    {
        Vector3 delta=point-actor.position;delta.y=0;
        if(delta.sqrMagnitude>.001f)actor.rotation=Quaternion.LookRotation(delta);
    }

    void StartSparring()
    {
        float side=practiceChild.index==0?1:-1;
        sparring=Spawn(0,2,false);
        sparring.actor.name="盾持ちピーマン";
        sparring.actor.position=Posts[practiceChild.index]+V(side*3.60f,0,.20f);
        sparring.hp=sparring.maxHp=3;
        sparringEntryPosition=player.position;
        PointAt(sparring.actor,player.position);
        PointAt(player,sparring.actor.position);
        yaw=player.eulerAngles.y;pitch=6;
        Say("");
    }

    bool TryShieldLesson(bool attackPressed)
    {
        if(shieldLessonSeen || !Training || !SparringInReach || carrying || placing>0)return false;
        bool approached=(player.position-sparringEntryPosition).sqrMagnitude>.35f*.35f;
        if(!approached && !attackPressed)return false;

        shieldLessonSeen=true;
        shieldLessonTime=ShieldLessonLength;
        keeper.gameObject.SetActive(false);
        PointAt(practiceChild.actor,sparring.actor.position);
        PointAt(sparring.actor,Vector3.Lerp(practiceChild.actor.position,practiceEye,.65f));
        return true;
    }

    void UpdateShieldLesson(float dt)
    {
        if(shieldLessonTime<=0)return;
        float previous=shieldLessonTime;
        shieldLessonTime=Mathf.Max(0,shieldLessonTime-dt);
        const float shootAt=ShieldLessonLength-.56f;
        if(previous>shootAt && shieldLessonTime<=shootAt)
        {
            Vector3 start=practiceChild.actor.position+Vector3.up*1.24f;
            Fx(start,sparring.actor,null,.79f,true,false,.55f).receiver=sparring;
            Chime(840,.09f);
        }
        if(shieldLessonTime>0)return;
        keeper.gameObject.SetActive(true);
        PointAt(sparring.actor,player.position);
        PointAt(player,sparring.actor.position);
        yaw=player.eulerAngles.y;pitch=6;
        MoveCamera(true);Say("");
    }

    void RemoveSparring(bool completed)
    {
        if(sparring==null)return;
        if(completed)
        {
            openingCountdown=OpeningCountdownLength;
            Burst(sparring.actor.position+Vector3.up,5,false);Chime(988,.19f);
            Say("");
        }
        foes.Remove(sparring);Destroy(sparring.actor.gameObject);sparring=null;
    }

    bool AdvanceOpeningCountdown(float dt)
    {
        if(openingCountdown<=0)return false;
        int before=Mathf.CeilToInt(openingCountdown);
        openingCountdown=Mathf.Max(0,openingCountdown-dt);
        if(openingCountdown<=.0001f)return true;
        if(Mathf.CeilToInt(openingCountdown)<before && before<=4)
            Chime(560+(4-before)*110,.10f);
        return false;
    }

    bool SparringInReach => sparring!=null &&
        (sparring.actor.position-player.position).sqrMagnitude<StrikeRange*StrikeRange;

    Objective ChooseObjective()
    {
        if(practicePepper!=null)return new Objective(TaskKind.Practice,practicePepper.actor.position,"盾なしピーマンにハートを当てるよ","ピーマン");
        if(sparring!=null)
        {
            string hint=!shieldLessonSeen?"盾持ちピーマンに近づいて、盾の働きを見よう":
                carrying?"哺乳瓶を置いて、攻撃の準備をしよう":
                sparring.guard>0?"盾はハートを防ぐ。自分で割ってみよう！":"盾が割れた！　今度は本体を攻撃しよう";
            return new Objective(TaskKind.Sparring,sparring.actor.position,hint,"盾持ちピーマン");
        }
        Child hungry=children[0];
        if(firstDelivery)
            foreach(var b in children)
                if(b.food<hungry.food)hungry=b;
        if(firstDelivery && hungry.food>=FullBottle-.1f)
            return new Objective(TaskKind.Ready,Route(0,0),"準備完了！　Tab で敵を呼べるよ","西の入口");
        if(!carrying)return new Objective(TaskKind.Milk,MilkPoint,"屋台に近づいて、ミルクを取ろう","ミルク屋台");
        return new Objective(TaskKind.Deliver,hungry.actor.position,Names[hungry.index]+"に近づいて、ミルクを届けよう",Names[hungry.index]+"・"+LandNames[hungry.index]);
    }
    void UpdateGuidance()
    {
        if(!Training)return;
        objective=ChooseObjective();
        Vector3 to=objective.at-player.position;to.y=0;float length=to.magnitude;
        bool visible=!Directing && !SparringInReach && objective.kind!=TaskKind.Practice && objective.kind!=TaskKind.Ready && Active;
        Vector3 dir=to.normalized;
        for(int i=0;i<trail.Length;i++)
        {
            float d=1.15f+i*1.35f+Mathf.Repeat(clock*.9f,1)*.32f;
            bool show=visible && d<length-.65f;
            var mark=trail[i];mark.gameObject.SetActive(show);
            if(!show)continue;
            mark.position=player.position+dir*d+Vector3.up*.12f;mark.rotation=Quaternion.LookRotation(dir);
        }
    }
}
