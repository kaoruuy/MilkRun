using UnityEngine;
using static ToyGarden;

public partial class MilkRun
{
    static readonly Color MapLawn=Hex("4A6652"), MapRoad=Hex("F0D0A0"), MapRoadEdge=Hex("705638");
    static readonly Color MapEnemy=Hex("FF897A"), MapTurned=Hex("DF5958"), FaceColor=Hex("FFDDAD");

    static Vector2 MapPoint(Rect bounds,Vector3 world)
    {
        return new Vector2(bounds.center.x+world.x*bounds.width/(2*WalkX),
            bounds.center.y-world.z*bounds.height/(2*WalkZ));
    }

    static void MapLine(Vector2 from,Vector2 to,Color color,float width)
    {
        Matrix4x4 original=GUI.matrix;
        Vector2 delta=to-from;
        Quaternion heading=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        GUI.matrix=original*Matrix4x4.TRS(new Vector3(from.x,from.y,0),heading,Vector3.one);
        Plate(0,-width*.5f,delta.magnitude,width,color);
        GUI.matrix=original;
    }

    static void MapDot(Vector2 point,float diameter,Color color)
    {
        float radius=diameter*.5f;
        Plate(point.x-radius,point.y-radius,diameter,diameter,color,radius);
    }

    void DrawStorkMapMarker(Vector2 at,float size)
    {
        float half=size*.5f;
        MapDot(at,size,Navy);
        MapDot(at,size-3,Aqua);
        Caption(at.x-half,at.y-half,size,size,"鳥",size>=22?14:11,Navy,true,TextAnchor.MiddleCenter);
    }

    void DrawRoadOnMap(Rect bounds,int side)
    {
        float roadWidth=RoadWidth*bounds.width/(2*WalkX);
        // The same waypoints and width drive the garden roads and enemy movement.
        for(int layer=0;layer<2;layer++)
        {
            Color color=layer==0?MapRoadEdge:MapRoad;
            float width=layer==0?roadWidth+3:roadWidth;
            for(int point=0;point<4;point++)
            {
                Vector2 from=MapPoint(bounds,Route(side,point));
                Vector2 to=MapPoint(bounds,Route(side,point+1));
                MapLine(from,to,color,width);
                MapDot(from,width,color);
                if(point==3)MapDot(to,width,color);
            }
        }
        for(int point=0;point<4;point+=2)
        {
            Vector2 from=MapPoint(bounds,Route(side,point));
            Vector2 to=MapPoint(bounds,Route(side,point+1));
            Vector2 dir=(to-from).normalized;
            Vector2 across=new Vector2(-dir.y,dir.x);
            Vector2 tip=Vector2.Lerp(from,to,.48f);
            float size=mapExpanded?7:4.5f;
            MapLine(tip-dir*size+across*size,tip,MapRoadEdge,2);
            MapLine(tip-dir*size-across*size,tip,MapRoadEdge,2);
        }
        Vector2 entrance=MapPoint(bounds,Route(side,0));
        Caption(entrance.x-35,entrance.y-24,70,19,side==0?"西の入口":"東の入口",
            mapExpanded?13:11,Paper,true,TextAnchor.MiddleCenter);
    }

    void DrawBabyOnMap(Vector2 point,Child baby)
    {
        bool crying=baby.mood==Mood.Crying;
        bool turned=baby.mood==Mood.Turned;
        float unit=mapExpanded?1.45f:1f;
        float line=2.2f*unit;
        Color rim=crying?Hex("A6DFFE"):BabyColor(baby);

        if(turned)
        {
            MapLine(point+new Vector2(-13,-15)*unit,point+new Vector2(-8,-7)*unit,MapTurned,5*unit);
            MapLine(point+new Vector2(13,-15)*unit,point+new Vector2(8,-7)*unit,MapTurned,5*unit);
        }
        MapDot(point,28*unit,Navy);
        MapDot(point,24*unit,rim);
        MapDot(point,19*unit,turned?MapTurned:FaceColor);
        for(int side=-1;side<=1;side+=2)
        {
            Vector2 eye=point+new Vector2(side*4,-2)*unit;
            MapDot(eye,3*unit,Navy);
            if(crying)MapDot(eye+new Vector2(side*2,5)*unit,3.4f*unit,Hex("4A9FD4"));
            if(turned)
                MapLine(eye+new Vector2(side*2,-3.2f)*unit,eye+new Vector2(-side*2,-.5f)*unit,Navy,line);
        }
        if(crying)
            MapDot(point+new Vector2(0,4)*unit,4.4f*unit,Navy);
        else
        {
            float edge=turned?6:2, middle=turned?3.4f:5.3f;
            Vector2 mouth=point+Vector2.up*middle*unit;
            MapLine(point+new Vector2(-3.7f,edge)*unit,mouth,Navy,line);
            MapLine(mouth,point+new Vector2(3.7f,edge)*unit,Navy,line);
        }
        if(mapExpanded)
            Caption(point.x-39,point.y+20,78,20,Names[baby.index],13,Paper,true,TextAnchor.MiddleCenter);
    }

    void DrawHouseOnMap(Rect bounds,int side,bool underAttack)
    {
        Vector2 at=MapPoint(bounds,Forts[side]);
        Color house=underAttack?Danger:Paper;
        if(underAttack)
            MapDot(at,mapExpanded?42:34,new Color(1f,.49f,.43f,.35f));
        Plate(at.x-13,at.y-10,26,21,Navy,4);
        Plate(at.x-7,at.y-2,14,9,house,1);
        MapLine(at+new Vector2(-9,0),at+new Vector2(0,-7),house,2.4f);
        MapLine(at+new Vector2(0,-7),at+new Vector2(9,0),house,2.4f);
        Plate(at.x-1.8f,at.y+2,3.6f,5,Navy,1);
        Caption(at.x-55,at.y+11,110,19,underAttack?FortNames[side]+" 被弾":FortNames[side],
            mapExpanded?14:11,underAttack?Danger:Paper,true,TextAnchor.MiddleCenter);
    }

    void TacticalMap()
    {
        float extraLegend=stork!=null?19:0;
        float width=mapExpanded?Mathf.Min(560,(hh-270-2*extraLegend)*WalkX/WalkZ):252;
        float height=width*WalkZ/WalkX;
        float left=mapExpanded?(ww-width)*.5f:ww-width-31;
        float top=mapExpanded?(hh-height)*.5f:228;
        var field=new Rect(left,top,width,height);
        var bounds=new Rect(left+12,top+13,width-24,height-28);
        bool underAttack=hitFlash[0]>.04f || hitFlash[1]>.04f;
        Color panel=underAttack?Hex("693C42"):Navy;
        Color ground=candyTown?Hex("765B6C"):MapLawn;
        Color lawn=underAttack?Color.Lerp(ground,Hex("965651"),.58f):ground;

        Plate(left-10,top-36,width+20,height+73+extraLegend,panel,14);
        Plate(left,top,width,height,lawn,10);
        Caption(left+3,top-33,width-95,29,mapExpanded?"全体マップ ／ 矢印は敵の進む方向":"全体マップ",
            mapExpanded?17:15,Paper,true);
        Plate(left+width-76,top-29,73,22,Gold,8);
        Caption(left+width-76,top-29,73,22,mapExpanded?"M  閉じる":"M  拡大",13,Navy,true,TextAnchor.MiddleCenter);

        for(int side=0;side<2;side++)DrawRoadOnMap(bounds,side);

        Vector2 milk=MapPoint(bounds,MilkPoint);
        float milkSize=mapExpanded?37:28;
        if(candyTown && mapExpanded)
        {
            for(int side=0;side<2;side++)
            {
                Vector2 fork=MapPoint(bounds,Route(side,2));
                for(int step=1;step<8;step++)
                    MapDot(Vector2.Lerp(fork,milk,step/8f),4,Gold);
            }
        }
        MapDot(milk,milkSize,MilkAvailable?Paper:Pink);
        Art(milk.x-milkSize*.5f,milk.y-milkSize*.5f,milkSize,milkSize,milkIcon);
        Caption(milk.x-53,milk.y+milkSize*.5f,106,20,MilkAvailable?"ミルク補給所":"ミルク盗難中",mapExpanded?15:11,
            MilkAvailable?Paper:Pink,true,TextAnchor.MiddleCenter);

        for(int side=0;side<2;side++)DrawHouseOnMap(bounds,side,hitFlash[side]>.04f);
        foreach(var baby in children)DrawBabyOnMap(MapPoint(bounds,baby.actor.position),baby);

        foreach(var foe in foes)
        {
            if(foe==sparring || foe==practicePepper || foe.hp<=0)continue;
            Vector2 at=MapPoint(bounds,foe.actor.position);
            float size=mapExpanded?13:9;
            if(foe.boss)
            {
                MapDot(at,size+12,Navy);
                Art(at.x-size*.5f-5,at.y-size*.5f-5,size+10,size+10,BossArt(foe));
            }
            else if(foe.guard>0)
            {
                Plate(at.x-8,at.y-8,16,16,MapRoadEdge,4);
                Caption(at.x-8,at.y-9,16,17,"盾",11,Gold,true,TextAnchor.MiddleCenter);
            }
            else if(foe.kind==MilkThiefKind)
            {
                MapDot(at,size+10,Navy);
                Caption(at.x-12,at.y-9,24,18,"盗",12,foe==milkCarrier?Pink:Gold,true,TextAnchor.MiddleCenter);
            }
            else
            {
                MapDot(at,size+4,Navy);
                MapDot(at,size,MapEnemy);
            }
        }

        Vector2 self=MapPoint(bounds,player.position);
        Plate(self.x-5,self.y-5,10,10,Navy,2);
        Plate(self.x-3.5f,self.y-3.5f,7,7,Aqua,2);

        if(stork!=null)
        {
            Vector2 position=MapPoint(bounds,stork.position);
            Vector2 marker=position+new Vector2(-12,-14)*(mapExpanded?1.25f:1);
            MapLine(marker,position,Navy,2.5f);
            MapDot(position,4,Aqua);
            DrawStorkMapMarker(marker,mapExpanded?24:19);
        }

        float legend=field.yMax+7;
        MapDot(new Vector2(left+12,legend+9),8,MapEnemy);
        Caption(left+21,legend,46,18,"敵",12,Paper,true);
        Caption(left+56,legend,73,18,"盾  盾持ち",12,Gold,true);
        Plate(left+138,legend+5,8,8,Aqua,2);
        Caption(left+152,legend,87,18,candyTown?"自分   盗  どろぼう":"自分",12,Paper,true);
        if(mapExpanded)
            Caption(left+249,legend,width-255,18,"赤く光るおうちは攻撃を受けています",13,underAttack?Danger:Paper,true);
        if(stork!=null)
        {
            DrawStorkMapMarker(new Vector2(left+12,legend+28),17);
            Caption(left+26,legend+19,100,18,StorkName,12,Paper,true);
        }
    }
}
