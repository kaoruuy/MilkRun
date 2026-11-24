using UnityEngine;
using static ToyArt;

public partial class MilkRun
{
    const int OpeningArtLayer=29;
    const int CoverArt=4;
    readonly RenderTexture[] openingArt=new RenderTexture[5];
    static readonly string[] StoryLines={
        "おもちゃの庭で、赤ちゃんたちはお昼寝中。",
        "そこへ王冠ピーマンがおうちを狙ってやってきた！",
        "保育士さんがミルクを届けると、赤ちゃんはごきげん。",
        "赤ちゃんがハートを飛ばして、ピーマンをこうげき！"
    };
    int storyPanel;

    void AdvanceStory()
    {
        if(storyPanel==StoryLines.Length-1){Begin();return;}
        storyPanel++;
        Chime(660+storyPanel*70,.08f);
    }

    void CreateOpeningArt()
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        var camera=Root("Picture-book camera",transform,Vector3.zero).gameObject.AddComponent<Camera>();
        camera.enabled=false;
        camera.clearFlags=CameraClearFlags.SolidColor;
        camera.cullingMask=1<<OpeningArtLayer;
        camera.nearClipPlane=.15f;
        camera.farClipPlane=34f;
        camera.useOcclusionCulling=false;
        camera.orthographic=true;
        Vector3 stage=V(440,0,440);
        for(int i=0;i<openingArt.Length;i++)
        {
            bool cover=i==CoverArt;
            Transform group=Root("Picture-book scene",null,stage);
            BuildOpeningScene(group,i);
            foreach(var piece in group.GetComponentsInChildren<Transform>(true))piece.gameObject.layer=OpeningArtLayer;
            var picture=new RenderTexture(cover?720:648,cover?772:256,24,RenderTextureFormat.ARGB32);
            picture.name=cover?"Title garden":"Story panel "+(i+1);
            camera.backgroundColor=Hex(i==1?"F6C5AC":i==2?"FFE5BC":"D5E9DF");
            camera.orthographicSize=cover?2.47f:i==1?1.78f:1.51f;
            camera.transform.position=stage+(cover?V(.56f,3.28f,8.2f):V(.20f,2.97f,8.8f));
            camera.transform.LookAt(stage+V(0,cover?1.33f:i==1?1.47f:1.22f,0));
            camera.targetTexture=picture;
            camera.Render();
            openingArt[i]=picture;
            group.gameObject.SetActive(false);
            Destroy(group.gameObject);
        }
        camera.targetTexture=null;
        Destroy(camera.gameObject);
    }

    void ReleaseOpeningArt()
    {
        foreach(var picture in openingArt)
        {
            if(picture==null)continue;
            picture.Release();
            Destroy(picture);
        }
    }

    static void BuildOpeningScene(Transform scene,int panel)
    {
        Box(scene,0,-.15f,.8f,23,.30f,15,panel==1?"C6B479":panel==2?"E9C88F":"A8C58B");
        Disc(scene,0,.026f,.25f,5.7f,.052f,panel==1?"DEC291":panel==2?"F1D5A5":"D7DFAD");
        for(int side=-1;side<=1;side+=2)
        {
            OpeningCottage(scene,V(side*2.45f,0,-3.22f),side<0?"C87A88":"748BAF");
            for(int i=0;i<3;i++)
            {
                float x=side*(2.2f+i*1.2f);
                Box(scene,x,.44f,-4.8f,.17f,.90f,.17f,Wood);
                Box(scene,x+side*.44f,.45f,-4.81f,1,.14f,.12f,"E0AD8E");
            }
            float treeX=side*(panel==CoverArt?2.66f:3.86f);
            Box(scene,treeX,.83f,-1.62f,.17f,1.64f,.18f,Wood);
            Ball(scene,treeX,1.95f,-1.62f,1.36f,1.36f,1.18f,side<0?"EAB289":"C5D299");
            Ball(scene,treeX-.23f,2.26f,-1.52f,.54f,.49f,.45f,side<0?"F4BC98":"D8DFAB");
        }
        for(int i=0;i<7;i++)
        {
            float x=(i-3)*.82f;
            float z=i%2==0?1.95f:-.49f;
            Disc(scene,x,.03f,z,.30f,.035f,i%3==0?Rose:White);
            Ball(scene,x,.066f,z,.09f,.075f,.09f,Honey);
        }
        switch(panel)
        {
            case CoverArt:
                var owner=OpeningKeeper(scene,V(0,0,-.16f),-10);
                owner.localScale=Vector3.one*1.28f;
                OpeningBaby(scene,0,V(-1.36f,.10f,1.08f),22);
                OpeningBaby(scene,1,V(1.38f,.10f,1.13f),-15);
                OpeningBaby(scene,2,V(0,.03f,1.95f),6);
                Enemy(scene,V(2.02f,0,-1.90f),2,false).localRotation=Quaternion.Euler(0,-25,0);
                Heart(scene,V(-.8f,2.31f,1.16f),.43f,Rose);
                Heart(scene,V(1.06f,2.43f,.9f),.32f,Honey);
                break;
            case 0:
                for(int i=0;i<3;i++)
                {
                    var baby=OpeningBaby(scene,i,V((i-1)*1.55f,0,.29f+(i%2)*.26f),(1-i)*13);
                    baby.GetComponent<BabyExpression>().Pose(BabyExpression.Look.Soothing,1,.9f);
                    baby.localRotation*=Quaternion.Euler(0,0,(1-i)*9);
                }
                Disc(scene,0,.058f,.52f,4.72f,.04f,"ECBDAB");
                break;
            case 1:
                var king=Enemy(scene,V(0,0,.02f),2,true);
                king.localRotation=Quaternion.Euler(0,-5,0);
                king.localScale=Vector3.one*1.72f;
                PanLid(Enemy(scene,V(-1.68f,0,.67f),2,false));
                Enemy(scene,V(1.81f,0,.39f),0,false).localRotation=Quaternion.Euler(0,23,0);
                break;
            case 2:
                var keeper=OpeningKeeper(scene,V(1.18f,0,.34f),-42);
                keeper.localRotation*=Quaternion.Euler(11,0,-7);
                OpeningBaby(scene,2,V(-.87f,.06f,.74f),33);
                Bottle(scene,V(-.07f,.88f,.87f),1.5f).localRotation=Quaternion.Euler(0,15,-27);
                Heart(scene,V(-1.63f,1.53f,.85f),.38f,Rose);
                break;
            case 3:
                OpeningKeeper(scene,V(-.84f,0,.20f),38);
                OpeningBaby(scene,0,V(-2.18f,.04f,.87f),40);
                OpeningBaby(scene,2,V(.26f,.08f,.68f),88);
                Enemy(scene,V(2.25f,0,.40f),2,false).localRotation=Quaternion.Euler(0,-42,-18);
                Heart(scene,V(.91f,.99f,.87f),.24f,Rose);
                Heart(scene,V(1.36f,1.10f,.88f),.36f,Rose);
                Heart(scene,V(1.83f,1.20f,.89f),.48f,Rose);
                for(int ray=-1;ray<=1;ray++)
                    Box(scene,2.08f,1.23f+ray*.23f,1.02f,.27f,.055f,.055f,Honey).localRotation=Quaternion.Euler(0,0,ray*35);
                break;
        }
    }

    static Transform OpeningBaby(Transform group,int index,Vector3 at,float facing)
    {
        var baby=Baby(group,at,ToyGarden.Accents[index],index);
        baby.localRotation=Quaternion.Euler(0,facing,0);
        baby.GetComponent<BabyExpression>().Pose(BabyExpression.Look.Happy,1,0);
        return baby;
    }

    static Transform OpeningKeeper(Transform group,Vector3 at,float facing)
    {
        var teacher=Keeper(group,at);
        teacher.localRotation=Quaternion.Euler(0,facing,0);
        var arm=teacher.Find("Arm R");
        arm.localRotation=Quaternion.Euler(-37,0,-10);
        Bottle(arm,V(0,-.42f,.20f),.93f).localRotation=Quaternion.Euler(43,0,0);
        return teacher;
    }

    static void OpeningCottage(Transform group,Vector3 at,string roof)
    {
        Transform house=Root("Paper cottage",group,at);
        Box(house,0,.64f,0,1.57f,1.28f,1.10f,"F3DDC0");
        for(int side=-1;side<=1;side+=2)
        {
            Box(house,side*.44f,1.30f,0,1.11f,.21f,1.41f,roof).localRotation=Quaternion.Euler(0,0,-side*31);
            Box(house,side*.45f,.84f,.56f,.38f,.36f,.08f,"87ABB6");
        }
        Box(house,0,.38f,.56f,.32f,.76f,.10f,Wood);
        Heart(house,V(0,1.07f,.604f),.21f,roof);
    }
}
