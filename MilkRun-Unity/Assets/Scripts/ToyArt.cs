using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// All shapes are local to the parent. Actor faces point along +Z.
public static class ToyArt
{
    public const string Ink="283448", White="FFF1D8", Rose="F4758E", Mint="63C5B2", Honey="F4BC60", Blue="80AFDB", Wood="BC8067", Skin="F6C5A7", Leaf="71AC83";
    public const string ProteinPurple="9872C7";
    static readonly Dictionary<string, Material> paints = new Dictionary<string, Material>();
    static Mesh heart;
    public static Material Paint(string hex)
    {
        if (paints.TryGetValue(hex,out var material)) return material;
        material = new Material(Resources.Load<Shader>("ToyPaint"));
        ColorUtility.TryParseHtmlString("#"+hex,out Color color);
        material.color=color;
        material.enableInstancing=true; paints[hex]=material; return material;
    }
    public static Transform Root(string name,Transform parent,Vector3 pos)
    {
        var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=pos; return t;
    }
    public static Transform Shape(PrimitiveType shape,Transform parent,Vector3 pos,Vector3 scale,string color,string name="Toy")
    {
        var g=GameObject.CreatePrimitive(shape); g.name=name;
        g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale;
        var c=g.GetComponent<Collider>(); if(c!=null) { c.enabled=false; Object.Destroy(c); }
        var r=g.GetComponent<Renderer>(); r.sharedMaterial=Paint(color);
        return g.transform;
    }
    public static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    public static Transform Ball(Transform p,float x,float y,float z,float a,float b,float c,string col) =>
        Shape(PrimitiveType.Sphere,p,V(x,y,z),V(a,b,c),col);
    public static Transform Box(Transform p,float x,float y,float z,float a,float b,float c,string col) =>
        Shape(PrimitiveType.Cube,p,V(x,y,z),V(a,b,c),col);
    public static Transform Disc(Transform p,float x,float y,float z,float diameter,float height,string col) =>
        Shape(PrimitiveType.Cylinder,p,V(x,y,z),V(diameter,height*.5f,diameter),col);
    static Mesh shield, horn;
    static Transform Carving(string name,Transform parent,Vector3 pos,Mesh mesh,string color)
    {
        var t=Root(name,parent,pos);
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Paint(color);
        return t;
    }

    // The same pointed badge appears on the castle walls, flag and HUD.
    public static Transform Shield(Transform p,Vector3 at,float size,string color)
    {
        if(shield==null)
        {
            var rim=new[]{V(-.48f,.50f,0),V(.48f,.50f,0),V(.45f,-.05f,0),V(.28f,-.35f,0),V(0,-.56f,0),V(-.28f,-.35f,0),V(-.45f,-.05f,0)};
            int n=rim.Length;var points=new List<Vector3>();var triangles=new List<int>();
            for(int face=0;face<2;face++)
                foreach(var v in rim)points.Add(v+Vector3.forward*(face==0?-.055f:.055f));
            for(int i=1;i<n-1;i++)triangles.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});
            for(int i=0;i<n;i++){int end=(i+1)%n;triangles.AddRange(new[]{i,n+i,n+end,i,n+end,end});}
            shield=new Mesh{name="Fort shield"};shield.SetVertices(points);shield.SetTriangles(triangles,0);shield.RecalculateNormals();shield.RecalculateBounds();
        }
        var badge=Carving("Fort shield",p,at,shield,color);badge.localScale=Vector3.one*size;return badge;
    }

    public static Transform Horn(Transform p,int side)
    {
        if(horn==null)
        {
            const int count=10;var points=new List<Vector3>();var triangles=new List<int>();
            for(int band=0;band<4;band++)
            {
                float t=band/4f,r=.225f*(1-t);
                for(int i=0;i<count;i++){float a=i*2*Mathf.PI/count;points.Add(V(.21f*t*t+r*Mathf.Cos(a),.63f*t,-.09f*t+r*Mathf.Sin(a)));}
            }
            int tip=points.Count;points.Add(V(.21f,.63f,-.09f));
            for(int i=0;i<count;i++)
            {
                int j=(i+1)%count;
                for(int band=0;band<3;band++){int a=band*count+i,b=band*count+j;triangles.AddRange(new[]{a,a+count,b,b,a+count,b+count});}
                triangles.AddRange(new[]{3*count+i,tip,3*count+j});
            }
            horn=new Mesh{name="Curled horn"};horn.SetVertices(points);horn.SetTriangles(triangles,0);horn.RecalculateNormals();horn.RecalculateBounds();
        }
        var h=Carving("Curled horn",p,V(side*.37f,1.79f,.07f),horn,"6D1529");
        h.localRotation=Quaternion.Euler(0,side<0?180:0,-side*12);return h;
    }

    public static Transform Heart(Transform p,Vector3 pos,float size,string color)
    {
        if(heart==null)
        {
            const int n=32; var v=new List<Vector3>(); var tri=new List<int>();
            for(int side=0;side<2;side++)
            {
                v.Add(V(0,0,side==0?.14f:-.14f));
                for(int i=0;i<n;i++)
                {
                    float a=i*2*Mathf.PI/n;
                    v.Add(V(Mathf.Pow(Mathf.Sin(a),3)*.55f,
                        (13*Mathf.Cos(a)-5*Mathf.Cos(2*a)-2*Mathf.Cos(3*a)-Mathf.Cos(4*a)+2)/29,
                        side==0?.1f:-.1f));
                }
            }
            for(int i=0;i<n;i++)
            {
                int a=1+i,b=1+(i+1)%n,d=n+1;
                tri.AddRange(new[]{0,b,a,d,d+a,d+b,a,b,d+b,a,d+b,d+a});
            }
            heart=new Mesh { name="Carved heart" }; heart.SetVertices(v);heart.SetTriangles(tri,0);heart.RecalculateNormals();heart.RecalculateBounds();
        }
        var t=Root("Heart",p,pos);t.localScale=Vector3.one*size;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=heart;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Paint(color);return t;
    }
    public static void Face(Transform p,float y,float z,float width,float unit)
    {
        foreach(int s in new[]{-1,1})
        {
            Ball(p,s*width,y,z,.083f*unit,.112f*unit,.052f*unit,Ink);
            Ball(p,s*width*1.65f,y-.09f*unit,z-.025f*unit,.13f*unit,.066f*unit,.045f*unit,"EB8F88");
        }
    }
    public static Transform Bottle(Transform p,Vector3 at,float scale,bool protein=false)
    {
        var r=Root(protein?"Protein milk bottle":"Milk bottle",p,at);r.localScale=Vector3.one*scale;
        Ball(r,0,.35f,0,.44f,.69f,.44f,"FFFEED");
        Disc(r,0,.62f,0,.42f,.13f,protein?ProteinPurple:Mint);Disc(r,0,.71f,0,.29f,.08f,Honey);
        Ball(r,0,.79f,0,.14f,.2f,.14f,"FFCF9F");
        if(protein)
        {
            Disc(r,0,.35f,0,.46f,.205f,ProteinPurple);
            for(int side=-1;side<=1;side+=2)
            {
                Box(r,0,.35f,side*.225f,.15f,.039f,.025f,White);
                Box(r,0,.35f,side*.225f,.038f,.15f,.025f,White);
            }
        }
        else for(int i=0;i<3;i++) Box(r,0,.23f+.09f*i,.214f,.12f-i*.02f,.026f,.012f,Blue);
        return r;
    }
    public static void Rattle(Transform p,Vector3 grip)
    {
        var r=Root("ガラガラ",p,grip);
        r.localRotation=Quaternion.Euler(-17,0,42);
        Ball(r,0,.04f,0,.12f,.38f,.12f,Mint);
        Ball(r,0,-.15f,0,.18f,.13f,.18f,Honey);
        Ball(r,0,.30f,0,.42f,.43f,.42f,Rose);
        Disc(r,0,.30f,0,.43f,.085f,Honey);
        Ball(r,0,.43f,-.13f,.13f,.09f,.09f,White);
    }
    public static Transform Baby(Transform p,Vector3 pos,string color,int index)
    {
        var r=Root("Baby "+(index+1),p,pos);
        Ball(r,0,.50f,0,.98f,.77f,.84f,color);
        Ball(r,0,.74f,.26f,.68f,.49f,.37f,White);
        Heart(r,V(0,.76f,.48f),.22f,color);
        Ball(r,0,1.30f,0,1.26f,1.12f,1.09f,Skin);
        for(int s=-1;s<=1;s+=2)
        {
            Ball(r,s*.59f,1.24f,0,.23f,.31f,.23f,Skin);
            Ball(r,s*.48f,.64f,.07f,.35f,.45f,.32f,Skin).name=s<0?"Arm L":"Arm R";
            Ball(r,s*.33f,.23f,.30f,.43f,.34f,.48f,color);
        }
        Ball(r,0,1.72f,-.04f,1.08f,.39f,.99f,color);
        if(index==0) {Ball(r,-.2f,1.98f,0,.3f,.46f,.25f,color);Ball(r,.2f,1.98f,0,.3f,.46f,.25f,color);}
        else if(index==1) {Ball(r,-.4f,1.88f,0,.37f,.36f,.26f,color);Ball(r,.4f,1.88f,0,.37f,.36f,.26f,color);}
        else {Ball(r,0,1.99f,-.04f,.35f,.36f,.32f,Honey);}
        BabyExpression.MakeFaces(r,color);
        return r;
    }
    public static Transform Keeper(Transform p,Vector3 pos)
    {
        var r=Root("Childcare teacher",p,pos);
        const string apron="73C6AC", shirt="D6B8E6", hair="63434C";
        Ball(r,0,1.0f,0,.78f,.94f,.57f,shirt);
        Ball(r,0,.84f,.23f,.88f,.76f,.38f,apron);
        Box(r,0,1.14f,.245f,.65f,.41f,.13f,apron);
        Box(r,0,.75f,.36f,.48f,.23f,.09f,"F5B69B");
        Heart(r,V(.02f,.762f,.42f),.14f,"C66D75");
        Box(r,-.19f,1.24f,.328f,.16f,.091f,.055f,White);
        Ball(r,0,1.70f,0,.76f,.76f,.67f,Skin);Face(r,1.72f,.315f,.145f,.7f);
        Ball(r,0,1.80f,-.17f,.79f,.69f,.54f,hair);
        Ball(r,0,2.0f,.04f,.76f,.26f,.66f,hair);
        Ball(r,-.19f,1.95f,.27f,.42f,.24f,.17f,hair).localRotation=Quaternion.Euler(0,0,24);
        Ball(r,0,1.78f,-.43f,.29f,.12f,.29f,Rose);
        Ball(r,0,1.60f,-.43f,.33f,.40f,.31f,hair);
        for(int s=-1;s<=1;s+=2)
        {
            var arm=Root(s<0?"Arm L":"Arm R",r,V(s*.43f,1.31f,0));
            Ball(arm,0,-.12f,0,.28f,.42f,.29f,shirt);
            Box(arm,0,-.237f,.035f,.25f,.105f,.25f,White);
            Ball(arm,0,-.36f,.05f,.23f,.24f,.24f,Skin);
            var leg=Root(s<0?"Leg L":"Leg R",r,V(s*.20f,.70f,0));
            Ball(leg,0,-.21f,0,.29f,.56f,.30f,"556584");
            Ball(leg,0,-.52f,.06f,.34f,.27f,.47f,White);
            Box(r,s*.21f,1.35f,.23f,.12f,.09f,.20f,apron);
        }
        return r;
    }
    public static Transform Enemy(Transform p,Vector3 pos,int type,bool boss)
    {
        var r=Root(boss?"Jumbo pepper":new[]{"Cat","Syringe","Pepper","Grandpa"}[type],p,pos);
        if(type==0)
        {
            Ball(r,0,.53f,0,.78f,.85f,.67f,Honey);Ball(r,0,.98f,.15f,.92f,.73f,.68f,"E9A770");
            for(int s=-1;s<=1;s+=2){var ear=Box(r,s*.29f,1.30f,.10f,.25f,.32f,.22f,"D88964");ear.localRotation=Quaternion.Euler(0,0,-s*23);}
            Ball(r,.35f,.58f,.34f,.24f,.30f,.26f,White).name="Paw";Face(r,1.02f,.47f,.17f,.84f);Ball(r,0,.90f,.5f,.12f,.07f,.06f,Ink);
            Ball(r,.37f,.62f,-.26f,.19f,.69f,.19f,Honey).localRotation=Quaternion.Euler(-20,0,-26);
        }
        else if(type==1)
        {
            var body=Root("Plunger",r,V(0,.76f,0));body.localRotation=Quaternion.Euler(0,0,-18);
            Disc(body,0,0,0,.55f,.74f,"ADCFE3");Disc(body,0,.37f,0,.71f,.09f,White);
            Disc(body,0,.55f,0,.16f,.28f,Blue);Disc(body,0,.72f,0,.46f,.09f,Rose);
            Disc(body,0,-.48f,0,.10f,.21f,Blue);Face(body,0,.27f,.12f,.7f);
            for(int i=0;i<3;i++) Box(body,-.15f,-.18f+i*.14f,.24f,.10f,.035f,.04f,White);
        }
        else if(type==2)
        {
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Ball(r,Mathf.Cos(a)*.18f,.72f,Mathf.Sin(a)*.17f,.64f,1.04f,.61f,i%2==0?"64B578":"80C881");}
            Ball(r,0,1.24f,0,.19f,.33f,.21f,"44806A").localRotation=Quaternion.Euler(0,0,-22);
            Face(r,.83f,.45f,.16f,.9f);
            if(boss)
            {
                Ball(r,0,.64f,-.37f,1.10f,.82f,.48f,"AA3C58");
                Disc(r,0,1.40f,0,.82f,.22f,Honey);Disc(r,0,1.34f,0,.84f,.07f,"C27B4E");
                for(int i=0;i<5;i++)
                {
                    float a=i*1.2566f,x=Mathf.Cos(a)*.32f,z=Mathf.Sin(a)*.32f;
                    Ball(r,x,1.58f,z,.15f,.37f,.15f,Honey);Ball(r,x,1.78f,z,.14f,.14f,.14f,Rose);
                }
                for(int s=-1;s<=1;s+=2)Box(r,s*.16f,.96f,.47f,.21f,.055f,.075f,Ink).localRotation=Quaternion.Euler(0,0,s*18);
            }
        }
        else
        {
            Ball(r,0,.55f,0,.80f,.83f,.67f,"A092BE");Ball(r,0,1.10f,0,.78f,.73f,.68f,Skin);
            for(int s=-1;s<=1;s+=2){Ball(r,s*.34f,1.22f,-.04f,.19f,.33f,.40f,"E5E3DC");Ball(r,s*.165f,1.13f,.32f,.24f,.19f,.065f,Ink);Ball(r,s*.165f,1.13f,.355f,.17f,.13f,.05f,"AAC8C9");}
            Ball(r,0,.98f,.32f,.33f,.12f,.09f,White);Box(r,.46f,.52f,.12f,.08f,1.06f,.09f,Wood).name="Cane";
        }
        for(int s=-1;s<=1;s+=2) Ball(r,s*.21f,.15f,.08f,.32f,.22f,.37f,Ink);
        r.localScale=Vector3.one*(boss?2.1f:1f);return r;
    }
    public static Transform Broccoli(Transform parent)
    {
        var r=Root("Broccoli",parent,Vector3.zero);
        Ball(r,0,.63f,0,.87f,1.02f,.71f,"8DBB78");
        Ball(r,0,1.05f,.04f,.90f,.73f,.82f,"78A261");
        for(int side=-1;side<=1;side+=2)
        {
            Ball(r,side*.42f,.49f,.14f,.36f,.42f,.38f,"7BAD75");
            Ball(r,side*.29f,.15f,.04f,.43f,.30f,.44f,"53815B");
            Ball(r,side*.50f,1.49f,-.05f,.86f,.77f,.79f,"4A9267");
            Ball(r,side*.27f,1.78f,.18f,.69f,.66f,.64f,"67AE72");
        }
        Ball(r,0,1.75f,-.18f,.94f,.85f,.78f,"4E9D68");
        Ball(r,0,1.91f,.20f,.64f,.52f,.62f,"80BF83");
        Face(r,1.13f,.44f,.18f,1.13f);
        for(int side=-1;side<=1;side+=2)
            Box(r,side*.18f,1.23f,.47f,.18f,.042f,.05f,Ink).localRotation=Quaternion.Euler(0,0,side*21);
        return r;
    }

    public static Transform CookieSoldier(Transform parent)
    {
        var r=Root("Cookie soldier",parent,Vector3.zero);
        var wheel=Root("Cookie wheel",r,V(0,.70f,0));
        Ball(wheel,0,0,0,1.36f,1.36f,.47f,"B87752");
        Ball(wheel,0,0,.055f,1.27f,1.28f,.44f,"E6A763");
        for(int i=0;i<9;i++)
        {
            float angle=i*2*Mathf.PI/9;
            Ball(wheel,.49f*Mathf.Cos(angle),.49f*Mathf.Sin(angle),.203f,.15f,.14f,.11f,i%3==0?"965B4F":"794C40");
        }
        Face(wheel,.093f,.295f,.166f,1.05f);
        Ball(wheel,0,-.08f,.299f,.15f,.060f,.05f,Ink);
        Disc(wheel,0,.44f,.11f,.21f,.27f,Rose);
        return r;
    }

    public static Transform CookieGeneral(Transform parent)
    {
        var r=Root("Cookie general",parent,Vector3.zero);
        const string coat="884F65", pastry="E2A36A", chocolate="82523F";
        Ball(r,0,.61f,0,1.27f,.93f,.74f,coat);
        Ball(r,0,.70f,-.37f,1.43f,.87f,.39f,"652F59");
        Ball(r,0,1.16f,.087f,1.22f,1.14f,.49f,chocolate);
        Ball(r,0,1.17f,.153f,1.12f,1.06f,.44f,pastry);
        for(int side=-1;side<=1;side+=2)
        {
            Ball(r,side*.45f,1.16f,.28f,.14f,.12f,.13f,chocolate);
            Ball(r,side*.35f,.96f,.314f,.15f,.14f,.10f,chocolate);
            Ball(r,side*.35f,.18f,.16f,.46f,.35f,.50f,chocolate);
            Disc(r,side*.52f,.85f,.036f,.32f,.15f,Honey);
            Box(r,side*.19f,1.34f,.365f,.24f,.07f,.065f,chocolate).localRotation=Quaternion.Euler(0,0,side*19);
        }
        Face(r,1.21f,.37f,.19f,1.04f);
        Box(r,0,.64f,.38f,.54f,.12f,.065f,Honey).localRotation=Quaternion.Euler(0,0,-36);
        Disc(r,0,1.66f,.03f,1.06f,.087f,chocolate);
        Disc(r,0,1.79f,.00f,.94f,.29f,coat);
        Disc(r,0,1.73f,.00f,.96f,.079f,Honey);
        Ball(r,.18f,1.99f,-.01f,.29f,.42f,.20f,White).localRotation=Quaternion.Euler(0,0,-35);
        var baton=Root("Candy baton",r,V(.60f,.59f,.19f));
        Disc(baton,0,.22f,0,.11f,.54f,"D49461");
        Ball(baton,0,.55f,0,.31f,.31f,.31f,Rose);
        Disc(baton,0,.55f,0,.325f,.069f,White);
        r.localScale=Vector3.one*2.1f;
        return r;
    }

    public static Transform MilkThief(Transform parent)
    {
        var thief=Root("Masked milk thief",parent,Vector3.zero);
        Ball(thief,0,.56f,0,.80f,.90f,.71f,"846A9D");
        Ball(thief,0,1.18f,.10f,.98f,.76f,.76f,"B49BC6");
        for(int side=-1;side<=1;side+=2)
        {
            Ball(thief,side*.35f,1.58f,.08f,.28f,.33f,.24f,"846A9D");
            Ball(thief,side*.36f,1.60f,.18f,.16f,.19f,.09f,"F1B8B3");
            Ball(thief,side*.23f,1.25f,.437f,.38f,.27f,.105f,Ink);
            Ball(thief,side*.21f,1.26f,.496f,.09f,.13f,.066f,White);
            Ball(thief,side*.21f,.17f,.15f,.29f,.24f,.34f,Ink);
        }
        Ball(thief,0,1.055f,.435f,.30f,.19f,.15f,White);
        Ball(thief,0,1.10f,.518f,.14f,.092f,.092f,Ink);
        var tail=Root("Striped tail",thief,V(.32f,.45f,-.43f));
        tail.localRotation=Quaternion.Euler(-30,0,-49);
        Ball(tail,0,.19f,0,.29f,.68f,.27f,"846A9D");
        for(int i=0;i<3;i++)Ball(tail,0,.06f+i*.18f,0,.30f,.10f,.28f,Ink);
        Box(thief,0,.68f,-.39f,.47f,.52f,.26f,Wood);
        return thief;
    }
    public static Transform PanLid(Transform body)
    {
        var lid=Root("Copper pan lid",body,V(.26f,.43f,.52f));
        Ball(lid,0,0,0,1.12f,1.06f,.22f,"854B43");
        Ball(lid,0,0,.07f,1.01f,.95f,.20f,"F0B173");
        Ball(lid,0,0,.15f,.70f,.65f,.12f,"CD7953");
        for(int k=0;k<6;k++)
        {
            float a=k*Mathf.PI/3f;
            Ball(lid,.435f*Mathf.Cos(a),.397f*Mathf.Sin(a),.15f,.082f,.082f,.054f,White);
        }
        Box(lid,0,0,.24f,.34f,.11f,.12f,Ink);
        return lid;
    }
}
