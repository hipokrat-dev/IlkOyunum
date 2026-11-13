using System.Collections.Generic;
using UnityEngine;

public static class MineralModeli
{
    public static Mesh Olustur()
    {
        // Köşeleri düz yüzeyli altıgen mineral prizması; keskin doğal kristal yüzleri.
        var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        for(int i=0;i<6;i++){
            float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;
            Vector3 p=new Vector3(Mathf.Cos(a)*.48f,0,Mathf.Sin(a)*.48f),q=new Vector3(Mathf.Cos(b)*.48f,0,Mathf.Sin(b)*.48f);
            Vector3 topP=p+Vector3.up*1.25f,topQ=q+Vector3.up*1.25f,tip=new Vector3(.07f,1.7f,-.04f);
            Tri(vertices,triangles,uv,p,topP,topQ);Tri(vertices,triangles,uv,p,topQ,q);
            Tri(vertices,triangles,uv,topP,tip,topQ);Tri(vertices,triangles,uv,Vector3.zero,p,q);
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
    }
    static void Tri(List<Vector3> v,List<int> t,List<Vector2> uv,Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);uv.Add(Vector2.zero);uv.Add(Vector2.up);uv.Add(Vector2.one);}
}
