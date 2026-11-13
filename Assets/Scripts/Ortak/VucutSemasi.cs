using UnityEngine;
using UnityEngine.UI;

// Ölçekten bağımsız, hafif vektör şema. Koordinatlar önden görülen insan vücuduna aittir.
[RequireComponent(typeof(CanvasRenderer))]
public class VucutSemasi : MaskableGraphic
{
    public int aktif;
    public static readonly Vector2[] Konum = {Vector2.zero,new Vector2(.5f,.864f),new Vector2(.57f,.555f),new Vector2(.5f,.447f),new Vector2(.535f,.655f),new Vector2(.395f,.665f),new Vector2(.39f,.51f),new Vector2(.5f,.355f)};
    Color doku=new Color(.10f,.21f,.27f,1),cizgi=new Color(.26f,.43f,.49f,1);
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        // Siluet: baş, boyun, gövde ve eklemleri yuvarlatılmış uzuvlar.
        Oval(vh,new Vector2(.5f,.88f),new Vector2(.075f,.092f),doku);
        Hat(vh,new[]{new Vector2(.5f,.80f),new Vector2(.5f,.755f)},.075f,doku);
        Dolgu(vh,new[]{new Vector2(.31f,.755f),new Vector2(.43f,.78f),new Vector2(.57f,.78f),new Vector2(.69f,.755f),new Vector2(.64f,.60f),new Vector2(.60f,.43f),new Vector2(.635f,.34f),new Vector2(.365f,.34f),new Vector2(.40f,.43f),new Vector2(.36f,.60f)},doku);
        for(int side=-1;side<=1;side+=2){
            Hat(vh,new[]{new Vector2(.5f+side*.17f,.73f),new Vector2(.5f+side*.23f,.59f),new Vector2(.5f+side*.29f,.43f)},.065f,doku);
            Oval(vh,new Vector2(.5f+side*.29f,.41f),new Vector2(.03f,.045f),doku);
            Hat(vh,new[]{new Vector2(.5f+side*.085f,.355f),new Vector2(.5f+side*.09f,.21f),new Vector2(.5f+side*.105f,.07f)},.085f,doku);
            Oval(vh,new Vector2(.5f+side*.115f,.055f),new Vector2(.06f,.025f),doku);
        }
        // Kılavuz çizgiler, omurga ve kaburgalar.
        Hat(vh,new[]{new Vector2(.5f,.76f),new Vector2(.5f,.38f)},.003f,cizgi);
        for(int i=0;i<4;i++)for(int s=-1;s<=1;s+=2)Hat(vh,new[]{new Vector2(.5f,.72f-i*.034f),new Vector2(.5f+s*.09f,.71f-i*.034f),new Vector2(.5f+s*.13f,.68f-i*.029f)},.002f,cizgi);
        // Ağız ve yemek borusu.
        Hat(vh,new[]{new Vector2(.468f,.864f),new Vector2(.532f,.864f)},.012f,Renk(1));
        Hat(vh,new[]{new Vector2(.5f,.85f),new Vector2(.5f,.72f),new Vector2(.51f,.59f),new Vector2(.55f,.575f)},.014f,Renk(2));
        // Sağ ve sol akciğer, kalp (vücudun solunda, ekranda sağda).
        Oval(vh,new Vector2(.405f,.665f),new Vector2(.065f,.088f),Renk(5));
        Oval(vh,new Vector2(.595f,.665f),new Vector2(.065f,.088f),Renk(5));
        Hat(vh,new[]{new Vector2(.5f,.75f),new Vector2(.5f,.71f),new Vector2(.41f,.65f)},.008f,cizgi);
        Hat(vh,new[]{new Vector2(.5f,.71f),new Vector2(.59f,.65f)},.008f,cizgi);
        Dolgu(vh,new[]{new Vector2(.535f,.682f),new Vector2(.56f,.69f),new Vector2(.58f,.673f),new Vector2(.571f,.65f),new Vector2(.535f,.616f),new Vector2(.505f,.653f),new Vector2(.505f,.672f),new Vector2(.52f,.69f)},Renk(4));
        // Midenin kıvrımlı kesesi.
        Oval(vh,new Vector2(.56f,.557f),new Vector2(.062f,.045f),Renk(2));
        Hat(vh,new[]{new Vector2(.59f,.55f),new Vector2(.56f,.52f),new Vector2(.51f,.52f)},.018f,Renk(2));
        // İki böbrek ve mesaneye uzanan üreterler.
        for(int s=-1;s<=1;s+=2){
            Oval(vh,new Vector2(.5f+s*.115f,.51f),new Vector2(.028f,.045f),Renk(6));
            Oval(vh,new Vector2(.5f+s*.095f,.515f),new Vector2(.013f,.021f),doku);
            Hat(vh,new[]{new Vector2(.5f+s*.1f,.49f),new Vector2(.5f+s*.08f,.42f),new Vector2(.5f,.355f)},.006f,Renk(6));
        }
        // Bağırsak kıvrımları.
        Hat(vh,new[]{new Vector2(.42f,.40f),new Vector2(.40f,.49f),new Vector2(.43f,.505f),new Vector2(.58f,.505f),new Vector2(.60f,.48f),new Vector2(.58f,.395f),new Vector2(.51f,.38f)},.018f,Renk(3));
        for(int i=0;i<3;i++)Hat(vh,new[]{new Vector2(.44f,.477f-i*.026f),new Vector2(.55f,.477f-i*.026f),new Vector2(.565f,.465f-i*.026f),new Vector2(.44f,.465f-i*.026f)},.009f,Renk(3));
        Oval(vh,Konum[7],new Vector2(.04f,.028f),Renk(7));
        Hat(vh,new[]{new Vector2(.5f,.335f),new Vector2(.5f,.31f)},.008f,Renk(7));
    }
    Color Renk(int no)=>no==aktif ? PremiumTema.Altin : new Color(.25f,.61f,.62f,1);
    Vector3 P(Vector2 p){var r=rectTransform.rect;return new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height);}
    void Dolgu(VertexHelper v,Vector2[] p,Color c){Vector2 center=Vector2.zero;foreach(var q in p)center+=q;center/=p.Length;int n=v.currentVertCount;v.AddVert(P(center),c,Vector2.zero);foreach(var q in p)v.AddVert(P(q),c,Vector2.zero);for(int i=0;i<p.Length;i++)v.AddTriangle(n,n+1+i,n+1+(i+1)%p.Length);}
    void Oval(VertexHelper v,Vector2 p,Vector2 r,Color c){var points=new Vector2[40];for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;points[i]=p+new Vector2(Mathf.Cos(a)*r.x,Mathf.Sin(a)*r.y);}Dolgu(v,points,c);}
    void Hat(VertexHelper v,Vector2[] p,float width,Color c){for(int i=0;i<p.Length-1;i++){Vector2 d=(p[i+1]-p[i]).normalized;Vector2 n=new Vector2(-d.y,d.x)*width*.5f;Dolgu(v,new[]{p[i]+n,p[i+1]+n,p[i+1]-n,p[i]-n},c);}foreach(var q in p)Oval(v,q,Vector2.one*width*.5f,c);}
}
