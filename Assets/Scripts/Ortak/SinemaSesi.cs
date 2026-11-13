using UnityEngine;
public static class SinemaSesi
{
    // Hafif ortam + iki kamera geçişi. Konuşma ve müzik içermez.
    public static AudioClip Uret(int bolum)
    {
        const int hz=22050;var data=new float[hz*5];var random=new System.Random(120+bolum);float low=0,phase=0;
        for(int i=0;i<data.Length;i++){
            float t=(float)i/hz,noise=(float)random.NextDouble()*2-1;low+=.065f*(noise-low);
            float fade=Mathf.Clamp01(t/.25f)*Mathf.Clamp01((5-t)/.55f);
            float whoosh=(Mathf.Exp(-Mathf.Pow((t-.48f)*5,2))+Mathf.Exp(-Mathf.Pow((t-3.25f)*6,2)))*low*.8f;
            float sound=0;
            if(bolum==4){float beat=t%1.05f;float env=Mathf.Exp(-beat*22)+.65f*Mathf.Exp(-Mathf.Abs(beat-.19f)*35);sound=Mathf.Sin(t*2*Mathf.PI*58)*env*.22f;}
            else if(bolum==5)sound=low*(.16f+.26f*Mathf.Sin(t*Mathf.PI*.65f)*Mathf.Sin(t*Mathf.PI*.65f));
            else {float pulse=t%(.58f+bolum*.045f);phase+=2*Mathf.PI*Mathf.Lerp(900,160,Mathf.Clamp01(pulse*7))/hz;sound=Mathf.Sin(phase)*Mathf.Exp(-pulse*26)*.085f+low*.14f;}
            data[i]=(sound+whoosh)*fade;
        }
        var clip=AudioClip.Create("BolgeOrtami_"+bolum,data.Length,1,hz,false);clip.SetData(data,0);return clip;
    }
}
