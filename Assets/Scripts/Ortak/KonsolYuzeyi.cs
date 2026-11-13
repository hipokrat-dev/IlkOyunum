using UnityEngine;
using UnityEngine.UI;

// Ölçeklenebilir, ışık alan kabartmalı konsol yüzeyi. UI için tek mesh, ek kamera yok.
public class KonsolYuzeyi : Image
{
    public bool daire, dugme;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Rect r=GetPixelAdjustedRect();float radius=daire?Mathf.Min(r.width,r.height)*.5f:Mathf.Min(dugme?22:18,r.height*.22f);
        var shadow=r;shadow.y-=5;Shape(vh,shadow,radius,new Color(0,0,0,.6f),new Color(0,0,0,.6f));
        Shape(vh,r,radius,new Color(.50f,.59f,.64f,color.a),new Color(.075f,.10f,.13f,color.a));
        r=Inset(r,2);Shape(vh,r,radius-2,new Color(.07f,.10f,.13f,color.a),new Color(.02f,.035f,.05f,color.a));
        r=Inset(r,3);Shape(vh,r,radius-4,Color.Lerp(color,Color.white,dugme?.18f:.07f),Color.Lerp(color,Color.black,.28f));
        if(dugme){r=Inset(r,3);Shape(vh,r,radius-7,Color.Lerp(color,Color.white,.09f),Color.Lerp(color,Color.black,.15f));}
    }
    static Rect Inset(Rect r,float p){return new Rect(r.x+p,r.y+p,Mathf.Max(1,r.width-2*p),Mathf.Max(1,r.height-2*p));}
    static void Shape(VertexHelper vh,Rect r,float radius,Color top,Color bottom)
    {
        radius=Mathf.Max(0,Mathf.Min(radius,Mathf.Min(r.width,r.height)/2));int start=vh.currentVertCount;
        vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),Vector2.zero);
        const int steps=8;int count=4*(steps+1);
        for(int corner=0;corner<4;corner++){
            Vector2 c=new Vector2(corner==0||corner==3?r.xMax-radius:r.xMin+radius,corner<2?r.yMax-radius:r.yMin+radius);
            for(int k=0;k<=steps;k++){
                float a=(corner*90+k*90f/steps)*Mathf.Deg2Rad;var p=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                vh.AddVert(p,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,p.y)),Vector2.zero);
            }
        }
        for(int i=0;i<count;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public class KonsolIkon : MaskableGraphic
{
    public string tur;
    protected override void OnPopulateMesh(VertexHelper v)
    {
        v.Clear();
        switch(tur){
        case "kopuk":Circle(v,.35f,.37f,.17f);Circle(v,.63f,.43f,.22f);Circle(v,.42f,.72f,.13f);break;
        case "zipla":Line(v,.22f,.56f,.5f,.84f);Line(v,.5f,.84f,.78f,.56f);Line(v,.5f,.24f,.5f,.82f);Line(v,.20f,.16f,.80f,.16f);break;
        case "pause":Line(v,.35f,.22f,.35f,.78f,.12f);Line(v,.65f,.22f,.65f,.78f,.12f);break;
        case "map":Circle(v,.5f,.64f,.24f);Line(v,.32f,.50f,.5f,.14f);Line(v,.5f,.14f,.68f,.50f);break;
        case "play":Line(v,.32f,.20f,.72f,.50f);Line(v,.72f,.50f,.32f,.80f);Line(v,.32f,.80f,.32f,.20f);break;
        case "kart":Line(v,.24f,.18f,.24f,.82f);Line(v,.24f,.82f,.76f,.82f);Line(v,.76f,.82f,.76f,.18f);Line(v,.76f,.18f,.24f,.18f);Line(v,.36f,.60f,.64f,.60f);Line(v,.36f,.40f,.64f,.40f);break;
        case "dal":Line(v,.22f,.44f,.5f,.16f);Line(v,.5f,.16f,.78f,.44f);Line(v,.5f,.78f,.5f,.18f);break;
        default:Circle(v,.5f,.5f,.30f);break;
        }
    }
    Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.x+x*r.width,r.y+y*r.height);}
    void Line(VertexHelper v,float ax,float ay,float bx,float by,float width=.065f){var a=P(ax,ay);var b=P(bx,by);var n=new Vector2(-(b-a).y,(b-a).x).normalized*rectTransform.rect.width*width*.5f;int i=v.currentVertCount;v.AddVert(a-n,color,Vector2.zero);v.AddVert(a+n,color,Vector2.zero);v.AddVert(b+n,color,Vector2.zero);v.AddVert(b-n,color,Vector2.zero);v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);}
    void Circle(VertexHelper v,float x,float y,float radius){for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Line(v,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,.055f);}}
}
