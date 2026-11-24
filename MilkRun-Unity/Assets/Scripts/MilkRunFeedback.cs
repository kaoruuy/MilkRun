using System.Collections.Generic;
using UnityEngine;
using static ToyArt;
using static ToyGarden;

public partial class MilkRun
{
    static readonly string[] FoeNames={"猫","注射","ピーマン","おじいちゃん","ミルクどろぼう","ブロッコリー","クッキー兵"};
    const float AttackLength=.90f, ImpactTime=.25f;
    readonly float[] hitFlash=new float[2];
    readonly string[] lastAttacker=new string[2];
    readonly bool[] petDamage=new bool[2];
    Vector3 endCam;Quaternion endLook;
    const float CelebrationLength=5.35f, CelebrationJumpPeriod=1.08f;
    static readonly Vector3 CelebrationCenter=V(0,0,-9.8f);
    static readonly Vector3[] CelebrationBabyPositions={V(-2.55f,0,.12f),V(.90f,0,-.42f),V(2.55f,0,.14f)};
    int fallen;
    Transform[] fragments;
    Vector3[] origins;
    class ShieldBit
    {
        public Transform actor;
        public Vector3 origin, velocity, size, spin;
        public float age, life;
    }
    readonly List<ShieldBit> shieldBits=new List<ShieldBit>();
    AudioClip shieldCrack;
    float shieldBump;

    static AudioClip CreateShieldCrack()
    {
        const int rate=22050;
        var samples=new float[rate/3];
        var noise=new System.Random(207);
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)rate;
            float grit=(float)(noise.NextDouble()*2-1);
            float metal=Mathf.Sin(t*5900)+.55f*Mathf.Sin(t*8830);
            float knock=Mathf.Sin(t*1120)*.39f*Mathf.Exp(-t*41);
            float snap=grit*.59f*Mathf.Exp(-t*87)+metal*.20f*Mathf.Exp(-t*21);
            float scatter=t>.029f?grit*.23f*Mathf.Exp(-(t-.029f)*74):0;
            samples[i]=Mathf.Clamp((knock+snap+scatter)*Mathf.Min(1,t*2200),-1,1);
        }
        var clip=AudioClip.Create("Shield cracking",samples.Length,1,rate,false);
        clip.SetData(samples,0);
        return clip;
    }

    void ShowShieldBreak(Foe f)
    {
        f.guardFlash=0;
        f.shieldBreakTime=.85f;
        shieldBump=.17f;
        if(!muted)sounds.PlayOneShot(shieldCrack,.48f);

        Vector3 center=f.lid.position;
        float scale=f.boss?1.65f:1;
        for(int i=0;i<12;i++)
        {
            bool glitter=i>=8;
            float angle=i*2.39996f;
            Vector3 radial=f.lid.right*Mathf.Cos(angle)+f.lid.up*Mathf.Sin(angle);
            Vector3 start=center+radial*.23f*scale;
            Vector3 size=(glitter?V(.085f,.28f,.085f):V(.25f+i%3*.055f,.16f,.11f))*scale;
            string color=glitter?White:i%3==0?"8D5040":i%3==1?"CB7552":"FFD693";
            Transform piece=Box(cast,start.x,start.y,start.z,size.x,size.y,size.z,color);
            piece.rotation=Quaternion.AngleAxis(i*31,f.lid.forward)*f.lid.rotation;
            shieldBits.Add(new ShieldBit
            {
                actor=piece, origin=start, size=size,
                velocity=radial*(glitter?4.8f:2.6f)+Vector3.up*2.1f+f.lid.forward*(1.15f+i%4*.42f),
                spin=V(240+i*38,210-i*22,325),
                life=glitter?.24f:.61f+i%3*.08f
            });
        }
    }

    void ResetShieldBreak()
    {
        shieldBits.Clear();
        shieldBump=0;
    }

    void UpdateShieldBreak(float dt)
    {
        shieldBump=Mathf.Max(0,shieldBump-dt);
        for(int i=shieldBits.Count-1;i>=0;i--)
        {
            var bit=shieldBits[i];
            bit.age+=dt;
            if(bit.age>=bit.life)
            {
                Destroy(bit.actor.gameObject);
                shieldBits.RemoveAt(i);
                continue;
            }
            var point=bit.origin+bit.velocity*bit.age-Vector3.up*(6.7f*bit.age*bit.age);
            bit.actor.position=V(point.x,Mathf.Max(.08f,point.y),point.z);
            bit.actor.Rotate(bit.spin*dt);
            bit.actor.localScale=bit.size*Mathf.InverseLerp(bit.life,bit.life*.65f,bit.age);
        }
    }
    void AnimateChild(Child b,float dt)
    {
        b.joy=Mathf.Max(0,b.joy-dt);
        BabyExpression.Look look;
        if(petting==b)
        {
            PointAt(b.actor,player.position);PointAt(player,b.actor.position);
            look=b.pet>.7f?BabyExpression.Look.Soothing:BabyExpression.Look.Upset;
        }
        else if(b.mood==Mood.Turned)look=BabyExpression.Look.Upset;
        else if(b.mood==Mood.Crying)look=BabyExpression.Look.Crying;
        else if(b.joy>0||b.mood==Mood.Returning)look=BabyExpression.Look.Happy;
        else look=BabyExpression.Look.Calm;
        b.face.Pose(look,clock+.36f*b.index,b.pet/PetTime);
        if(petting==b && Mathf.FloorToInt(b.pet*1.6f)!=Mathf.FloorToInt((b.pet-dt)*1.6f))
        {
            Vector3 start=b.actor.position+V(.20f,1.65f,0);
            Fx(start,null,start+V(.22f,1.25f,0),.33f+.06f*b.pet,true);Chime(550+70*Mathf.FloorToInt(b.pet),.065f);
        }
    }
    void FoePose(Foe f,float t)
    {
        float hop=0,pitchAngle=0,roll=0,push=0;
        Vector3 scale=Vector3.one*(f.boss?2.1f:1);
        if(t<=0){hop=.045f+.08f*Mathf.Abs(Mathf.Sin(f.step));roll=Mathf.Sin(f.step)*5;}
        else
        {
            float wind=Mathf.Sin(Mathf.PI*Mathf.Min(t/.60f,1));
            float hit=Mathf.Sin(Mathf.PI*Mathf.Clamp01((t-.60f)/.28f));
            switch(f.kind)
            {
                case 0: hop=.34f*wind;push=.44f*hit;pitchAngle=-18*wind+23*hit;
                    if(f.movingPart!=null)f.movingPart.localPosition=V(.35f,.58f+.29f*wind,.34f+.46f*hit);break;
                case 1: pitchAngle=-40*wind-80*hit;push=.77f*hit;hop=.17f*wind;break;
                case 2: hop=1.03f*wind;push=.3f*hit;scale=Vector3.Scale(scale,V(1+.22f*hit,1-.28f*hit,1+.22f*hit));break;
                case 3: roll=10*wind-14*hit;push=.27f*hit;pitchAngle=19*hit;
                    if(f.movingPart!=null){f.movingPart.localRotation=Quaternion.Euler(-55*wind+66*hit,0,0);f.movingPart.localPosition=V(.46f,.52f+.34f*wind,.12f+.37f*hit);}break;
                case BroccoliKind: pitchAngle=-25*wind+35*hit;push=.31f*hit;hop=.27f*hit;break;
                case CookieKind: pitchAngle=-19*wind+48*hit;push=.52f*hit;hop=.35f*wind;break;
            }
        }
        if(f.kind==CookieKind)
            f.movingPart.localRotation=f.CookieCommander?Quaternion.Euler(-pitchAngle*1.8f,0,24+roll):Quaternion.Euler(0,0,-f.step*37f);
        if(t<=0 && f.movingPart!=null)
        {
            if(f.kind==0)f.movingPart.localPosition=V(.35f,.58f,.34f);
            if(f.kind==3){f.movingPart.localRotation=Quaternion.identity;f.movingPart.localPosition=V(.46f,.52f,.12f);}
        }
        float recoil=Mathf.Clamp01((f.hitTime-.47f)/.18f);
        f.body.localPosition=V(0,hop+.08f*recoil,push-.09f*recoil);
        f.body.localScale=Vector3.Scale(scale,V(1+.07f*recoil,1-.07f*recoil,1+.07f*recoil));
        f.body.localRotation=Quaternion.Euler(pitchAngle,0,roll);
    }
    void ResetTowers()
    {
        for(int i=0;i<2;i++)
        {
            FortModels[i].position=Forts[i];FortModels[i].rotation=Quaternion.identity;FortModels[i].localScale=Vector3.one;
            hitFlash[i]=0;lastAttacker[i]="";petDamage[i]=false;
        }
        fragments=null;origins=null;
    }
    void HitTower(int side,int damage,string who,bool turned)
    {
        if(health[side]<=0)return;
        health[side]=Mathf.Max(0,health[side]-damage);hitFlash[side]=.75f;
        lastAttacker[side]=who;petDamage[side]=turned;
        if(health[side]==0)fallen=side;
        Vector3 point=Forts[side]+Vector3.up*1.35f;Burst(point,3,false);
    }
    void TowerMotion(float dt)
    {
        for(int i=0;i<2;i++)
        {
            hitFlash[i]=Mathf.Max(0,hitFlash[i]-dt);
            float k=hitFlash[i]/.75f;
            FortModels[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(k*18)*k*5.8f);
        }
    }
    void Burst(Vector3 point,int count,bool hearts)
    {
        for(int i=0;i<count;i++)
        {
            float a=i*2.39996f;Vector3 end=point+V(Mathf.Cos(a)*1.25f,.65f+.16f*(i%3),Mathf.Sin(a)*1.25f);
            Fx(point,null,end,hearts?.31f:.23f,hearts);
        }
    }
    void StartEnding(bool win)
    {
        endCam=cam.transform.position;endLook=cam.transform.rotation;
        phase=win?Phase.Victory:Phase.Collapse;elapsed=0;petting=null;placing=0;CursorMode(false);
        foreach(var a in trail)a.gameObject.SetActive(false);
        if(win)
        {
            foreach(var b in children)if(b.mood==Mood.Ally){b.joy=4;Burst(b.actor.position+Vector3.up*1.2f,6,true);}
            Chime(784,.25f);return;
        }
        var tower=FortModels[fallen];fragments=new Transform[10];origins=new Vector3[10];
        for(int i=0;i<fragments.Length;i++)
        {
            float a=i*2.39996f;Vector3 start=Forts[fallen]+V(Mathf.Cos(a)*.49f,.85f+(i%3)*.39f,Mathf.Sin(a)*.49f);
            var piece=Box(cast,start.x,start.y,start.z,.40f,.31f,.36f,i%3==0?White:(fallen==0?Rose:Blue));
            fragments[i]=piece;origins[i]=start;piece.localScale=Vector3.zero;
        }
        Chime(247,.24f);
    }
    void AdvanceEnding(float dt)
    {
        float was=elapsed;elapsed+=dt;clock+=dt;
        UpdateSparks(dt);UpdateShieldBreak(dt);TurnSails(dt);
        bool win=phase==Phase.Victory;
        Vector3 point=win?(Forts[0]+Forts[1])*.5f:Forts[fallen];
        Vector3 stop=win?point+V(2.5f,11,-19):point+V(fallen==0?4.2f:-4.2f,3.4f,-5.9f);
        float ease=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.85f));
        cam.transform.position=Vector3.Lerp(endCam,stop,ease);
        cam.transform.rotation=Quaternion.Slerp(endLook,Quaternion.LookRotation(point+V(0,win?1.6f:.88f,win?4:0)-stop),ease);
        ClearSightline(cam.transform.position,point,false);
        if(win)
        {
            if(was<.45f && elapsed>=.45f)Chime(988,.23f);
            if(was<.90f && elapsed>=.90f){Chime(1175,.20f);foreach(var t in Forts)Burst(t+Vector3.up*2.2f,8,true);}
            if(fallenKing!=null)
            {
                float fall=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.85f));
                fallenKing.localRotation=Quaternion.Euler(0,180,78*fall);
                fallenKing.localScale=Vector3.one*Mathf.Lerp(1,.13f,Mathf.Clamp01((elapsed-.45f)/.8f));
                if(elapsed<1.25f)
                {
                    Vector3 at=fallenKing.position+V(0,1.15f,0);
                    cam.transform.position=at+V(3.2f,1.5f,-5.5f);cam.transform.LookAt(at);
                }
            }
            if(elapsed>=2.9f)
            {
                if(candyTown)StartCelebration();
                else Ending(Phase.Rewards);
            }
            return;
        }
        float k=Mathf.Clamp01((elapsed-.68f)/1.10f);
        var tower=FortModels[fallen];
        tower.rotation=Quaternion.Euler(8*k,0,(fallen==0?-1:1)*(Mathf.Sin(elapsed*24)*(1-k)*4+65*k*k));
        tower.position=Forts[fallen]-Vector3.up*(.74f*k*k);tower.localScale=V(1,1-.5f*k,1);
        if(was<.84f&&elapsed>=.84f){Chime(130,.30f);Burst(point+Vector3.up,10,false);}
        for(int i=0;i<fragments.Length;i++)
        {
            float a=i*2.39996f;
            fragments[i].localScale=k>0?V(.40f,.31f,.36f):Vector3.zero;
            Vector3 off=V(Mathf.Cos(a)*k*2.5f,Mathf.Sin(k*Mathf.PI)*1.5f-k*(origins[i].y-.19f),Mathf.Sin(a)*k*2.5f);
            fragments[i].position=origins[i]+off;fragments[i].rotation=Quaternion.Euler(k*i*32,k*80,k*125);
        }
        if(elapsed>=3.2f)Ending(Phase.Loss);
    }

    void StartCelebration()
    {
        phase=Phase.Celebration;
        elapsed=0;
        ResetCareSounds();
        foreach(var foe in foes)foe.actor.gameObject.SetActive(false);
        if(fallenKing!=null)fallenKing.gameObject.SetActive(false);
        if(stork!=null)stork.gameObject.SetActive(false);
        MilkStock.gameObject.SetActive(true);
        SetCarrying(false);
        UpdatePettingHand();

        Vector3 audience=CelebrationCenter+Vector3.back*9;
        player.position=CelebrationCenter+V(-.83f,0,.58f);
        PointAt(player,audience);
        keeper.localRotation=Quaternion.identity;
        foreach(var baby in children)
        {
            baby.actor.position=CelebrationCenter+CelebrationBabyPositions[baby.index];
            PointAt(baby.actor,audience);
            baby.body.localScale=Vector3.one;
            baby.face.SetPoweredUp(false);
            baby.face.SetDevil(false);
        }
        AdvanceCelebration(0);
    }

    static float CelebrationHop(float age)
    {
        float rise=Mathf.Clamp01(Mathf.Repeat(Mathf.Max(0,age),CelebrationJumpPeriod)/.68f);
        return 4*rise*(1-rise);
    }

    void AdvanceCelebration(float dt)
    {
        float before=elapsed;elapsed+=dt;clock+=dt;
        float cameraMove=Mathf.SmoothStep(0,1,elapsed/CelebrationLength);
        float distance=Mathf.Max(8.2f,10.8f/cam.aspect)+Mathf.Lerp(.78f,0,cameraMove);
        Vector3 focus=CelebrationCenter+Vector3.up*1.70f;
        Vector3 eye=CelebrationCenter+V(Mathf.Lerp(.70f,-.45f,cameraMove),3.18f,-distance);
        cam.fieldOfView=37;
        cam.transform.position=eye;
        cam.transform.LookAt(focus);
        ClearSightline(eye,focus,false);

        float hop=CelebrationHop(elapsed-.20f);
        keeper.localPosition=Vector3.up*(.93f*hop);
        leftArm.localRotation=Quaternion.Euler(-18,0,-22-117*hop);
        handR.localRotation=Quaternion.Euler(-18,0,22+117*hop);
        leftLeg.localRotation=Quaternion.Euler(18*hop,0,-7*hop);
        rightLeg.localRotation=Quaternion.Euler(-23*hop,0,8*hop);
        foreach(var baby in children)
        {
            float bounce=CelebrationHop(elapsed-.22f-baby.index*.055f);
            baby.body.localPosition=Vector3.up*(.025f+.78f*bounce);
            baby.body.localRotation=Quaternion.Euler(0,0,(baby.index-1)*7*bounce);
            baby.face.Pose(BabyExpression.Look.Happy,clock+baby.index*.36f,0);
        }

        const float firstCheer=.46f, secondCheer=firstCheer+2*CelebrationJumpPeriod;
        if((before<firstCheer && elapsed>=firstCheer) || (before<secondCheer && elapsed>=secondCheer))
        {
            foreach(var baby in children)Burst(baby.actor.position+Vector3.up*1.48f,4,true);
            Chime(1046,.15f);
        }
        UpdateSparks(dt);
        UpdateShieldBreak(dt);
        if(elapsed>=CelebrationLength)Ending(Phase.Finish);
    }
}
