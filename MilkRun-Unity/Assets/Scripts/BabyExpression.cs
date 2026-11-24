using UnityEngine;
using static ToyArt;

public sealed class BabyExpression : MonoBehaviour
{
    public enum Look { Calm, Crying, Upset, Soothing, Happy }
    readonly Transform[] poses=new Transform[5];
    Transform leftArm,rightArm;
    int shown=-1;
    Renderer[] bodyParts;
    Material[] friendlyPaint,devilPaint,poweredPaint;
    Transform devil,powerUp;
    Transform[] glints;
    bool poweredUp;
    public bool IsDevil {get; private set;}

    void BuildLooks(Transform baby,string accent)
    {
        // Capture the body before adding the expressions so the eyes and mouth stay legible.
        bodyParts=baby.GetComponentsInChildren<Renderer>();
        friendlyPaint=new Material[bodyParts.Length];devilPaint=new Material[bodyParts.Length];
        poweredPaint=new Material[bodyParts.Length];
        Material accentPaint=Paint(accent), proteinPaint=Paint(ProteinPurple);
        Material skinPaint=Paint(Skin), whitePaint=Paint(White);
        for(int i=0;i<bodyParts.Length;i++)
        {
            Material original=bodyParts[i].sharedMaterial;friendlyPaint[i]=original;
            poweredPaint[i]=original==accentPaint?proteinPaint:original;
            string red=original==skinPaint?"F23D45":original==whitePaint?"FF6A57":original==accentPaint?"BF2538":"D92A35";
            devilPaint[i]=Paint(red);
        }
        devil=Root("Devil horns and tail",baby,Vector3.zero);
        for(int side=-1;side<=1;side+=2)Horn(devil,side);
        var tail=Root("Curled tail",devil,V(0,.43f,-.39f));
        Ball(tail,.20f,.02f,-.16f,.53f,.14f,.19f,"B82138").localRotation=Quaternion.Euler(0,28,-14);
        Ball(tail,.47f,.16f,-.26f,.15f,.43f,.16f,"BB263C").localRotation=Quaternion.Euler(0,0,-27);
        var tip=Heart(tail,V(.55f,.39f,-.25f),.33f,"86152F");tip.localRotation=Quaternion.Euler(0,0,35);
        devil.gameObject.SetActive(false);
    }

    public void SetDevil(bool active)
    {
        if(IsDevil==active)return;
        IsDevil=active;devil.gameObject.SetActive(active);
        ApplyBodyPaint();
    }
    public void SetPoweredUp(bool active)
    {
        if(poweredUp==active)return;
        poweredUp=active;
        if(active && powerUp==null)
        {
            powerUp=Root("Protein milk sparkles",transform,Vector3.zero);
            glints=new Transform[3];
            for(int i=0;i<glints.Length;i++)
            {
                var glint=Root("Gold sparkle",powerUp,Vector3.zero);
                Ball(glint,0,0,0,.09f,.31f,.09f,Honey);
                Ball(glint,0,0,0,.26f,.085f,.085f,"FFDE93");
                Ball(glint,0,0,0,.085f,.085f,.26f,"FFDE93");
                glints[i]=glint;
            }
            MoveGlints(0);
        }
        ApplyBodyPaint();
    }
    void ApplyBodyPaint()
    {
        bool showPower=poweredUp && !IsDevil;
        if(powerUp!=null)powerUp.gameObject.SetActive(showPower);
        Material[] palette=IsDevil?devilPaint:showPower?poweredPaint:friendlyPaint;
        for(int i=0;i<bodyParts.Length;i++)bodyParts[i].sharedMaterial=palette[i];
    }
    void MoveGlints(float time)
    {
        for(int i=0;i<glints.Length;i++)
        {
            float angle=time*2.8f+i*2*Mathf.PI/glints.Length;
            float shimmer=Mathf.Sin(time*6+i*2);
            glints[i].localPosition=V(.93f*Mathf.Cos(angle),1.33f+i*.26f+shimmer*.12f,.81f*Mathf.Sin(angle));
            glints[i].localRotation=Quaternion.Euler(0,time*100+i*40,0);
            glints[i].localScale=Vector3.one*(.84f+shimmer*.16f);
        }
    }
    public static void MakeFaces(Transform baby,string color)
    {
        var face=baby.gameObject.AddComponent<BabyExpression>();
        face.BuildLooks(baby,color);
        face.leftArm=baby.Find("Arm L");face.rightArm=baby.Find("Arm R");
        for(int n=0;n<5;n++)
        {
            var look=(Look)n;var p=Root(look.ToString(),baby,V(0,1.35f,.508f));face.poses[n]=p;
            bool smile=look==Look.Happy || look==Look.Soothing;
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*.205f;
                if(smile)
                {
                    for(int k=-1;k<=1;k++)Ball(p,x+k*.045f,.012f-Mathf.Abs(k)*.03f,.025f,.086f,.047f,.075f,Ink);
                }
                else
                {
                    Ball(p,x,0,.028f,.105f,.145f,.072f,Ink);Ball(p,x-.018f,.027f,.066f,.033f,.036f,.021f,White);
                    if(look!=Look.Calm)Ball(p,x,.118f,.005f,.20f,.043f,.061f,Ink).localRotation=Quaternion.Euler(0,0,side*(look==Look.Upset?28:-23));
                    if(look==Look.Crying)Ball(p,x*1.21f,-.12f,.038f,.083f,.19f,.07f,"75C9EC");
                }
                Ball(p,x*1.64f,-.095f,-.022f,.17f,.088f,.07f,smile?Rose:"EFAB9A");
            }
            if(smile)
            {
                for(int k=-2;k<=2;k++)Ball(p,k*.041f,-.172f+Mathf.Abs(k)*.019f,.045f,.075f,.044f,.056f,"9B5659");
            }
            else if(look==Look.Crying||look==Look.Upset)Ball(p,0,-.17f,.055f,.17f,look==Look.Crying?.19f:.065f,.077f,"955568");
            else {Ball(p,0,-.19f,.04f,.24f,.15f,.1f,White);Ball(p,0,-.19f,.105f,.13f,.11f,.11f,color);}
            p.gameObject.SetActive(n==0);
        }
    }
    public void Pose(Look look,float time,float petProgress)
    {
        int idx=(int)look;
        if(shown!=idx){for(int i=0;i<poses.Length;i++)poses[i].gameObject.SetActive(i==idx);shown=idx;}
        float wave=Mathf.Sin(time*(look==Look.Happy?12f:6f));
        float lift=look==Look.Soothing?30+34*petProgress:look==Look.Happy?63+22*wave:look==Look.Crying?35+24*wave:5;
        leftArm.localRotation=Quaternion.Euler(-lift*.6f,0,-lift);rightArm.localRotation=Quaternion.Euler(-lift*.6f,0,lift);
        if(look==Look.Calm)poses[idx].localScale=new Vector3(1,Mathf.Repeat(time,3.8f)<.11f?.22f:1,1);
        if(poweredUp && !IsDevil)MoveGlints(time);
    }
}
