using UnityEngine;

public partial class MilkRun
{
    enum Tune { Garden, Defense, King }
    const float MusicMixLevel=.35f, MusicFadeSeconds=.72f;
    const string MusicPreference="MilkRun.BgmVolume";
    readonly AudioSource[] music=new AudioSource[3];
    readonly float[] musicFades=new float[3];
    int musicPercent;
    float musicDuckTime, musicLevel;

    void CreateMusic()
    {
        musicPercent=Mathf.Clamp(PlayerPrefs.GetInt(MusicPreference,60),0,100);
        for(int i=0;i<music.Length;i++)
        {
            var track=gameObject.AddComponent<AudioSource>();
            track.playOnAwake=false;
            track.loop=true;
            track.spatialBlend=0;
            track.priority=160;
            track.volume=0;
            track.clip=Resources.Load<AudioClip>("Music/"+(Tune)i);
            music[i]=track;
        }
    }

    void SetMusicVolume(int percent)
    {
        musicPercent=Mathf.Clamp(percent,0,100);
        PlayerPrefs.SetInt(MusicPreference,musicPercent);
        PlayerPrefs.Save();
    }

    void DuckMusic(float seconds)
    {
        musicDuckTime=Mathf.Max(musicDuckTime,seconds);
    }

    void MixMusic(float dt)
    {
        int selected=(int)Tune.Garden;
        if(phase==Phase.Wave)selected=(int)Tune.Defense;
        bool opening=phase==Phase.Wave && waveEntranceTime>0;
        if(opening && OpeningLength-waveEntranceTime<.59f)selected=-1;
        if(phase==Phase.Boss)selected=(int)Tune.King;
        if(phase==Phase.Victory || phase==Phase.Collapse || phase==Phase.Loss)selected=-1;

        musicDuckTime=Mathf.Max(0,musicDuckTime-dt);
        float mix=paused?.45f:(phase==Phase.Finish||phase==Phase.Rewards)?.72f:1f;
        if(musicDuckTime>0)mix*=.46f;
        // Move the music aside briefly for the bottle, lid or stomp sound.
        musicLevel=Mathf.MoveTowards(musicLevel,mix,dt*(mix<musicLevel?17f:3.6f));
        for(int i=0;i<music.Length;i++)
        {
            var track=music[i];
            if(track.clip==null)continue;
            bool wanted=i==selected;
            if(wanted && musicFades[i]==0)track.Play();
            float fadeSeconds=opening?(wanted?.12f:.16f):MusicFadeSeconds;
            musicFades[i]=Mathf.MoveTowards(musicFades[i],wanted?1:0,dt/fadeSeconds);
            track.volume=musicFades[i]*MusicMixLevel*musicLevel*(musicPercent*.01f);
            track.mute=muted;
            if(!wanted && musicFades[i]==0 && track.isPlaying)track.Stop();
        }
        sounds.mute=muted;
        crySource.mute=foeVoice.mute=muted;
    }
}
