using UnityEngine;

public partial class MilkRun
{
    static readonly string[] AttackLines={"ひっかくニャ！","チクッといくよ！","この一撃でどうだ！","どっこいしょ！","ミルクはいただき！","根っこでドーン！","転がって体当たり！"};
    static readonly string[] ApproachLines={"おうちへ行くニャ！","注射の時間だよ！","盾なら任せろ！","どれ、行くかのう。","ミルク屋台はどっちだ？","押されても進むぞ！","ころころ、先を急ごう！"};
    AudioSource crySource, foeVoice;
    AudioClip bottleSetDown, babyCry, foeCall, proteinPickup;
    Child cryingVoice;

    void CreateCareSounds()
    {
        crySource=gameObject.AddComponent<AudioSource>();
        foeVoice=gameObject.AddComponent<AudioSource>();
        crySource.playOnAwake=foeVoice.playOnAwake=false;
        bottleSetDown=MakeSound("Bottle set down",.27f,t=>
        {
            float tap=Mathf.Sin(t*1390)*.56f*Mathf.Exp(-t*54);
            float glass=(Mathf.Sin(t*7450)+.4f*Mathf.Sin(t*10250))*.24f*Mathf.Exp(-t*41);
            float bounce=t<.073f?0:Mathf.Sin((t-.073f)*4130)*.25f*Mathf.Exp(-(t-.073f)*55);
            return (tap+glass+bounce)*Mathf.Min(1,t*850);
        });
        proteinPickup=MakeSound("Protein milk pickup",.43f,t=>
        {
            float note=t<.085f?0:t<.17f?1:2;
            float pitch=note==0?660:note==1?830:1108;
            float time=t-note*.085f;
            float ring=Mathf.Sin(time*pitch*2*Mathf.PI)+.25f*Mathf.Sin(time*pitch*4*Mathf.PI);
            return ring*.48f*Mathf.Min(1,time*440)*Mathf.Exp(-time*14);
        });

        float phaseAngle=0;
        babyCry=MakeSound("Toy baby cry",1.22f,t=>
        {
            bool second=t>=.63f;
            float syllableTime=second?t-.63f:t;
            float length=second?.52f:.54f;
            float u=Mathf.Clamp01(syllableTime/length);
            float vibrato=17*Mathf.Sin(syllableTime*52)+8*Mathf.Sin(syllableTime*86);
            float pitch=(second?492:541)-118*u+53*Mathf.Sin(3.8f*u)+vibrato;
            phaseAngle+=pitch*Mathf.PI*2/22050;
            float envelope=Mathf.Sqrt(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)))*(1-.11f*Mathf.Sin(syllableTime*77));
            float vowel=.46f*Mathf.Sin(phaseAngle)+.31f*Mathf.Sin(2*phaseAngle)+.18f*Mathf.Sin(4*phaseAngle)+.10f*Mathf.Sin(5*phaseAngle);
            return envelope*vowel*(second?.73f:1);
        });
        phaseAngle=0;
        foeCall=MakeSound("Toy chatter",.73f,t=>
        {
            float syllable=t/.18f;
            float u=Mathf.Repeat(syllable,1);
            float bend=Mathf.Sin(Mathf.PI*u);
            phaseAngle+=(255+Mathf.Sin(Mathf.Floor(syllable)*2)*44+bend*35)*Mathf.PI*2/22050;
            return bend*Mathf.Clamp01((.73f-t)*55)*(.44f*Mathf.Sin(phaseAngle)+.26f*Mathf.Sin(2*phaseAngle)+.10f*Mathf.Sin(5*phaseAngle));
        });
    }

    static AudioClip MakeSound(string name,float length,System.Func<float,float> sample)
    {
        const int rate=22050;
        var data=new float[Mathf.CeilToInt(rate*length)];
        for(int i=0;i<data.Length;i++)data[i]=Mathf.Clamp(sample(i/(float)rate),-1,1);
        var clip=AudioClip.Create(name,data.Length,1,rate,false);
        clip.SetData(data,0);
        return clip;
    }

    void PlayCry(Child baby)
    {
        if(muted || crySource.isPlaying)return;
        cryingVoice=baby;
        crySource.clip=babyCry;
        crySource.pitch=.94f+baby.index*.055f;
        crySource.volume=.39f;
        crySource.Play();
        DuckMusic(.40f);
    }

    void StopCry(Child baby)
    {
        if(cryingVoice!=baby)return;
        crySource.Stop();
        cryingVoice=null;
    }

    void Speak(Foe foe,string line)
    {
        foe.line=line;
        foe.lineUntil=clock+1.65f;
        if(muted || foeVoice.isPlaying)return;
        foeVoice.clip=foeCall;
        foeVoice.pitch=foe.boss?.68f:foe.kind==0?1.28f:foe.kind==1?1.12f:foe.kind==3?.82f:1;
        foeVoice.volume=.34f;
        foeVoice.Play();
        DuckMusic(.30f);
    }

    void SpeakOnApproach(Foe foe)
    {
        foe.announcedApproach=true;
        Speak(foe,foe.CookieCommander?"全軍、クッキーの旗に続け！":foe.boss?"王冠ピーマンのお通りだ！":ApproachLines[foe.kind]);
    }

    void ResetCareSounds()
    {
        crySource.Stop();
        foeVoice.Stop();
        cryingVoice=null;
    }
}
