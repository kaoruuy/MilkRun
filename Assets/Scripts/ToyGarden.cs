using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static ToyArt;

public static class ToyGarden
{
    static readonly Vector3[] GardenPosts={V(-11.4f,0,-4.6f),V(12.4f,0,-10.1f),V(0,0,9.3f)};
    static readonly Vector3[] GardenForts={V(-13,0,-12.6f),V(13,0,-15.1f)};
    static readonly Vector3[] CandyPosts={V(-13.1f,0,-2.7f),V(12.7f,0,-5.75f),V(0,0,8.2f)};
    static readonly Vector3[] CandyForts={V(-13.6f,0,-13f),V(13.5f,0,-14.15f)};
    static bool candyTown;
    public static Vector3 MilkPoint => candyTown?V(0,0,-5.95f):V(0,0,-8.5f);
    public static Vector3[] Posts => candyTown?CandyPosts:GardenPosts;
    public static Vector3[] Forts => candyTown?CandyForts:GardenForts;
    public static Transform MilkStock {get;private set;}
    public static readonly string[] Accents={Rose,Mint,Honey};
    public static readonly string[] Names={"モモ","ミント","プリン"};
    public static readonly string[] FortNames={"西のおうち","東のおうち"};

    public static readonly Transform[] FortModels=new Transform[2];
    public static readonly Transform[] PostLights=new Transform[3];
    public const string FortTeal="246C70";
    static Transform windmill;
    public const float WalkX=23.8f, WalkZ=21.8f;
    public static float RoadWidth => candyTown?2.48f:2.08f;

    // Distinct silhouettes remain visible across the garden, even when a baby is off screen.
    static void Landmarks(Transform root)
    {
        var apple=Root("Apple tree",root,Posts[0]+V(-2.4f,0,1.3f));
        Disc(apple,0,1.6f,0,.48f,3.2f,Wood);
        Ball(apple,0,3.4f,0,2.7f,2.7f,2.35f,"83B693");
        for(int i=0;i<7;i++){float a=i*2.39f;Ball(apple,Mathf.Cos(a)*1.10f,3.1f+.50f*Mathf.Sin(a*2),Mathf.Sin(a)*1.02f,.51f,.53f,.51f,Rose);}
        var mill=Root("Mint windmill",root,Posts[1]+V(2.7f,0,1.9f));
        Box(mill,0,1.95f,0,.44f,3.9f,.48f,Wood);
        windmill=Root("Sails",mill,V(0,3.7f,-.35f));
        for(int i=0;i<4;i++){var sail=Root("Sail",windmill,Vector3.zero);sail.localRotation=Quaternion.Euler(0,0,90*i);Box(sail,.20f,.80f,0,.47f,1.43f,.10f,i%2==0?Mint:White);}
        Ball(mill,0,3.7f,-.45f,.43f,.43f,.25f,Honey);
        var arch=Root("Honey arch",root,Posts[2]+V(1.3f,0,2));
        for(int side=-1;side<=1;side+=2){Box(arch,side*1.65f,1.4f,0,.39f,2.8f,.42f,Honey);var beam=Box(arch,side*.85f,3.17f,0,2.05f,.34f,.44f,White);beam.localRotation=Quaternion.Euler(0,0,-side*28);}
        Disc(arch,0,2.83f,0,.70f,.49f,Honey);Ball(arch,0,2.53f,0,.24f,.24f,.24f,Wood);
        for(int j=0;j<3;j++)
        {
            var a=Posts[j]; var c=Accents[j];
            for(int i=0;i<5;i++){float t=i*1.2566f;Ball(root,a.x+Mathf.Cos(t)*1.55f,.12f,a.z+Mathf.Sin(t)*1.55f,.35f,.19f,.35f,c);}
        }
    }
    public static void TurnSails(float dt){if(windmill!=null)windmill.Rotate(0,0,26*dt);}
    class Building
    {
        public Renderer[] parts;
        public Bounds bounds;
        public float visible=1;
    }
    static readonly List<Building> buildings=new List<Building>();
    static MaterialPropertyBlock visibility;
    static void RememberBuilding(Transform root)
    {
        var parts=root.GetComponentsInChildren<Renderer>();
        var bounds=parts[0].bounds;
        foreach(var renderer in parts)bounds.Encapsulate(renderer.bounds);
        buildings.Add(new Building{parts=parts,bounds=bounds});
    }
    public static void ClearSightline(Vector3 camera,Vector3 playerChest,bool playing)
    {
        Vector3 delta=camera-playerChest;
        Ray ray=new Ray(playerChest,delta.normalized);
        foreach(var building in buildings)
        {
            bool blocks=playing && building.bounds.IntersectRay(ray,out float distance) && distance<delta.magnitude+.15f;
            float target=blocks?0:1;
            if(building.visible==target)continue;
            building.visible=Mathf.MoveTowards(building.visible,target,Time.unscaledDeltaTime*10);
            if(visibility==null)visibility=new MaterialPropertyBlock();
            visibility.SetFloat("_Visibility",building.visible);
            foreach(var renderer in building.parts)renderer.SetPropertyBlock(visibility);
        }
    }

    public static Vector3 Route(int side,int point)
    {
        float s=side==0?-1f:1f;
        if(candyTown)
        {
            switch(point)
            {
                case 0:return V(s*18.4f,0,side==0?16.1f:12.65f);
                case 1:return V(s*13.9f,0,side==0?10.1f:8.25f);
                case 2:return V(s*6.9f,0,side==0?4.5f:2.25f);
                case 3:return V(s*10.8f,0,-6.6f);
            }
        }
        switch(point)
        {
            case 0:return V(s*16.3f,0,side==0?16f:12.1f);
            case 1:return V(s*10.9f,0,side==0?10.3f:9.5f);
            case 2:return V(s*4.7f,0,4.4f);
            case 3:return V(s*9.9f,0,-5.3f);
            default:return Forts[side]+V(0,0,1.15f);
        }
    }
    static Transform Strip(Transform r,Vector3 a,Vector3 b,float width,float height,string color)
    {
        Vector3 mid=(a+b)*.5f,delta=b-a;
        var t=Box(r,mid.x,mid.y,mid.z,width,height,delta.magnitude,color);
        t.localRotation=Quaternion.LookRotation(delta);return t;
    }
    static void Road(Transform r,int side)
    {
        for(int i=0;i<4;i++)
        {
            Vector3 a=Route(side,i),b=Route(side,i+1);
            string surface=candyTown?"D59BAD":"D8BC91";
            Strip(r,a+Vector3.up*.028f,b+Vector3.up*.028f,RoadWidth,.044f,surface);
            Disc(r,b.x,.03f,b.z,RoadWidth,.05f,surface);
            int count=Mathf.RoundToInt((b-a).magnitude/1.35f);
            for(int j=0;j<count;j++)
            {
                Vector3 m=Vector3.Lerp(a,b,(j+.35f)/count);
                var dash=Box(r,m.x,.064f,m.z,.13f,.015f,.47f,"F6E4BB");dash.localRotation=Quaternion.LookRotation(b-a);
            }
        }
        var g=Route(side,0);string col=side==0?Rose:Blue;
        for(int s=-1;s<=1;s+=2)
        {
            Box(r,g.x+s*1.47f,1.25f,g.z,.36f,2.50f,.38f,col);
            for(int i=0;i<3;i++)Box(r,g.x+s*1.47f,i*.63f+.38f,g.z,.37f,.21f,.39f,White);
        }
        Box(r,g.x,2.57f,g.z,3.25f,.36f,.50f,col);
        for(int i=-1;i<=1;i++)Ball(r,g.x+i*.8f,2.6f,g.z-.25f,.16f,.16f,.10f,Honey);
    }
    static void Fort(Transform r,int i)
    {
        var a=Root(FortNames[i],r,Forts[i]);
        string wall=i==0?"FFE2C1":"D8EFDD",roof=i==0?"C85E76":"4E8DB6";
        Disc(a,0,.08f,0,3.2f,.16f,"ADA883");
        Box(a,0,.22f,0,2.51f,.34f,2.06f,Wood);
        Box(a,0,1.23f,0,2.48f,1.82f,1.91f,wall);
        for(int step=0;step<5;step++)
            Box(a,0,2.18f+step*.155f,0,2.36f-step*.44f,.20f,1.91f,wall);
        Box(a,.72f,2.80f,.35f,.36f,.94f,.42f,Wood);
        Box(a,.72f,3.29f,.35f,.45f,.16f,.50f,White);
        for(int side=-1;side<=1;side+=2)
        {
            var slope=Box(a,side*.72f,2.55f,0,1.76f,.21f,2.33f,roof);
            slope.localRotation=Quaternion.Euler(0,0,-side*30);
            var window=Root("Side window",a,V(side*1.25f,1.39f,0));
            window.localRotation=Quaternion.Euler(0,-side*90,0);
            HouseWindow(window,Vector3.zero);
            HouseWindow(a,V(side*.76f,1.39f,-.97f));
            Box(a,0,.85f,side*.98f,.69f,1.22f,.10f,roof);
            Ball(a,.21f,.85f,side*1.04f,.11f,.11f,.10f,Honey);
            Box(a,0,.19f,side*1.16f,.94f,.16f,.45f,"E1A981");
        }
        Disc(a,-.56f,3.31f,0,.10f,1.45f,Wood);
        Box(a,.03f,3.72f,0,1.18f,.65f,.08f,FortTeal);
        Shield(a,V(.03f,3.72f,-.07f),.46f,White);
        Shield(a,V(.03f,3.72f,.07f),.46f,White);
        Ball(a,-.56f,4.08f,0,.17f,.17f,.17f,Honey);
        FortModels[i]=a;RememberBuilding(a);
    }
    static void HouseWindow(Transform parent,Vector3 at)
    {
        Box(parent,at.x,at.y,at.z,.56f,.59f,.07f,Wood);
        Box(parent,at.x,at.y,at.z-.043f,.42f,.45f,.045f,"81BDCE");
        Box(parent,at.x,at.y,at.z-.07f,.063f,.45f,.025f,White);
        Box(parent,at.x,at.y,at.z-.07f,.42f,.06f,.025f,White);
    }
    static void Kiosk(Transform r)
    {
        var p=Root("Milk bar",r,MilkPoint);
        Box(p,0,.12f,0,2.88f,.22f,1.74f,Wood);
        Box(p,0,.57f,0,2.4f,.9f,1.28f,"F0B994");
        for(int i=-2;i<=2;i++)Box(p,i*.41f,.57f,-.66f,.19f,.84f,.04f,White);
        Box(p,0,1.04f,0,2.66f,.13f,1.45f,Mint);
        for(int x=-1;x<=1;x+=2)Box(p,x*1.14f,1.66f,.43f,.13f,1.28f,.13f,Wood);
        for(int i=0;i<7;i++){var b=Box(p,(i-3)*.40f,2.31f,0,.406f,.17f,1.79f,i%2==0?Rose:White);b.localRotation=Quaternion.Euler(-8,0,0);}
        MilkStock=Root("Bottles on the counter",p,Vector3.zero);
        for(int i=-1;i<=1;i++)Bottle(MilkStock,V(i*.43f,1.1f,-.08f),.78f);
        Bottle(p,V(0,2.44f,.1f),.79f);
        Disc(p,0,.02f,0,3.6f,.07f,"E7C69A");
        RememberBuilding(p);
    }
    static void Tree(Transform r,float x,float z,float h,int variety)
    {
        var t=Root("Felt tree",r,V(x,0,z));t.localScale=Vector3.one*h;
        Disc(t,0,.82f,0,.43f,1.65f,Wood);
        Ball(t,0,2.28f,0,2.18f,2.75f,1.98f,variety==0?"83B894":variety==1?"B5CA8B":"E2AC9B");
        Ball(t,-.56f,2.60f,-.52f,.85f,1.00f,.77f,variety==0?"A6CFA6":variety==1?"CDD89B":"F2C8AB");
    }
    public static Transform Create(bool candy)
    {
        candyTown=candy;
        buildings.Clear();
        windmill=null;
        var root=Root(candy?"Candy Town":"Toy Garden",null,Vector3.zero);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.78f,.79f,.80f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=candy?new Color(.96f,.85f,.90f):new Color(.84f,.91f,.90f);
        RenderSettings.fogStartDistance=34;RenderSettings.fogEndDistance=84;
        var light=Root("Afternoon sunlight",root,V(0,12,0)).gameObject.AddComponent<Light>();
        light.type=LightType.Directional;light.color=new Color(1f,.95f,.86f);light.intensity=1.11f;
        light.transform.rotation=Quaternion.Euler(47,-38,0);light.shadows=LightShadows.Soft;light.shadowStrength=.45f;light.shadowBias=.04f;
        QualitySettings.shadowDistance=48;QualitySettings.vSyncCount=1;Application.targetFrameRate=60;
        Box(root,0,-1.08f,0,195,.28f,195,candy?"C1A5D1":"A5C9B8");
        Box(root,0,-.55f,0,WalkX*2+3.3f,.76f,46.5f,"C99570");
        Box(root,0,-.18f,0,WalkX*2+3f,.28f,46.2f,"F1DAAA");
        Box(root,0,-.042f,0,WalkX*2+2.6f,.08f,45.85f,candy?"E8BFAB":"AFCC9E");
        for(int lane=0;lane<2;lane++){Road(root,lane);Fort(root,lane);}
        Kiosk(root);
        if(candy)CandyLandmarks(root);
        else Landmarks(root);
        for(int i=0;i<Posts.Length;i++)
        {
            Vector3 a=Posts[i];Disc(root,a.x,.08f,a.z,2.0f,.15f,Wood);Disc(root,a.x,.18f,a.z,1.92f,.12f,Accents[i]);
            Disc(root,a.x,.248f,a.z,1.65f,.06f,White);
            var lightRing=Root("Home lights",root,a);PostLights[i]=lightRing;
            for(int k=0;k<12;k++){float angle=k*Mathf.PI/6;Ball(lightRing,1.17f*Mathf.Cos(angle),.20f,1.17f*Mathf.Sin(angle),.16f,.16f,.16f,Accents[i]);}
            lightRing.gameObject.SetActive(false);
        }
        // A low fence frames the playable board without obstructing the follow camera.
        for(int side=-1;side<=1;side+=2)
        {
            float fenceX=WalkX+1;
            for(int j=-7;j<=7;j++) {float z=j*3.13f;Box(root,side*fenceX,.45f,z,.19f,.94f,.24f,White);}
            Box(root,side*fenceX,.53f,0,.14f,.16f,44.1f,"EAD0A8");
            for(int j=-7;j<=7;j++){float x=j*fenceX/7;Box(root,x,.42f,side*22.6f,.22f,.87f,.2f,White);}
            Box(root,0,.47f,side*22.6f,fenceX*2,.14f,.14f,"EAD0A8");
            for(int k=0;k<8;k++)
            {
                float x=side*(fenceX+2.5f+(k%2)*2.8f),z=-22+k*6.5f;
                if(candy)Lollipop(root,V(x,0,z),k%2==0?Rose:Mint,2.8f+(k%3)*.5f);
                else Tree(root,x,z,.92f+(k%3)*.21f,(k+1)%3);
            }
        }
        for(int i=0;i<10;i++)
        {
            float x=-23+i*5.1f,z=26+(i%2)*3;
            if(candy)Lollipop(root,V(x,0,z),i%2==0?Honey:Rose,3.7f+(i%3)*.45f);
            else Tree(root,x,z,.95f+(i%3)*.23f,i%3);
        }
        for(int i=0;i<6;i++)Ball(root,-45+i*18,-2,38+(i%2)*6,28,13+(i%3)*2,19,candy?(i%2==0?"DBA7BC":"E6C5D2"):(i%2==0?"AAC9A9":"C7D9B8"));
        for(int i=0;i<42;i++)
        {
            float s=i%2==0?-1:1;float x=s*(WalkX-5.1f+Mathf.Repeat(i*2.71f,4.3f));float z=-20.7f+Mathf.Repeat(i*5.17f,41.0f);
            if(candy)Ball(root,x,.13f,z,.58f,.29f,.53f,i%3==0?Rose:i%3==1?Mint:Honey);
            else if(i%4==0){Ball(root,x,.23f,z,.83f,.53f,.74f,"85B889");Ball(root,x+.33f,.15f,z+.12f,.63f,.39f,.54f,"A0C592");}
            else {for(int j=0;j<4;j++){float a=j*1.5708f;Ball(root,x+Mathf.Cos(a)*.12f,.12f,z+Mathf.Sin(a)*.12f,.22f,.14f,.22f,i%3==0?Rose:White);}Ball(root,x,.16f,z,.16f,.14f,.16f,Honey);}
        }
        return root;
    }
    static void CandyLandmarks(Transform root)
    {
        for(int i=0;i<Posts.Length;i++)
        {
            var at=Posts[i]+V(i==1?2.55f:-2.45f,0,1.35f);
            var cake=Root("Cupcake",root,at);
            Disc(cake,0,.58f,0,1.67f,1.12f,"BB816A");
            for(int rib=0;rib<12;rib++)
            {
                float angle=rib*Mathf.PI/6;
                Ball(cake,.77f*Mathf.Cos(angle),.54f,.77f*Mathf.Sin(angle),.19f,.99f,.19f,"DCA583");
            }
            Ball(cake,0,1.16f,0,1.94f,.80f,1.84f,Accents[i]);
            Ball(cake,0,1.68f,0,1.44f,.75f,1.35f,Accents[i]);
            Ball(cake,0,2.26f,0,.59f,.57f,.58f,"BF4868");
            Box(cake,.16f,2.56f,.01f,.09f,.42f,.09f,Leaf).localRotation=Quaternion.Euler(0,0,-27);
            RememberBuilding(cake);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Lollipop(root,V(side*4.2f,0,-7.7f),Rose,2.3f);
            Lollipop(root,V(side*21,0,4.5f),Honey,3.4f);
            Vector3 at=Route(side<0?0:1,1);
            Vector3 fork=Route(side<0?0:1,2);
            for(int k=1;k<10;k++)
            {
                var crumb=Vector3.Lerp(fork,MilkPoint+V(0,0,1.35f),k*.10f);
                Disc(root,crumb.x,.036f,crumb.z,.28f,.038f,"D2A680");
            }
            for(int k=0;k<3;k++)
                Ball(root,at.x+side*(1.9f+k*.48f),.22f,at.z+.7f*k,.70f,.42f,.66f,Accents[k]);
        }
    }
    static void Lollipop(Transform root,Vector3 at,string color,float height)
    {
        var stand=Root("Lollipop",root,at);
        Box(stand,0,height*.42f,0,.15f,height*.84f,.16f,White);
        Ball(stand,0,height,0,1.53f,1.54f,.53f,color);
        Ball(stand,.16f,height+.23f,-.24f,.49f,.21f,.08f,White);
    }
}
