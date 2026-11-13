using UnityEditor;
[InitializeOnLoad]
public static class MobilTestDogrulama
{
    static MobilTestDogrulama(){if(SessionState.GetBool("MobilRegression",false))MobilKalite.TestMobil=true;}
    public static void Dokunmatik(){SessionState.SetBool("MobilRegression",true);MobilKalite.TestMobil=true;MobilVarlikHazirligi.Profil();BolumTesti.Dokunmatik();}
    public static void Bagirsak(){SessionState.SetBool("MobilRegression",true);MobilKalite.TestMobil=true;MobilVarlikHazirligi.Profil();BolumTesti.Bagirsak();}
    public static void Agiz(){SessionState.SetBool("MobilRegression",true);MobilKalite.TestMobil=true;MobilVarlikHazirligi.Profil();BolumTesti.Agiz();}
}
