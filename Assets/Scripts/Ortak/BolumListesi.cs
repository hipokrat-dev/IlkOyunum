// Oyundaki bölümlerin sırası (ana menü ve bölümler arası geçiş için).
public static class BolumListesi
{
    public readonly struct Bolum
    {
        public readonly int no;
        public readonly string ad, sahne;
        public readonly bool puanli;

        public Bolum(int no, string ad, string sahne, bool puanli = true)
        {
            this.no = no;
            this.ad = ad;
            this.sahne = sahne;
            this.puanli = puanli;
        }
    }

    public static readonly Bolum[] Tumu =
    {
        new Bolum(1, "Ağız", "Agiz"),
        new Bolum(2, "Yemek Borusu\nve Mide", "YemekBorusuMide"),
        new Bolum(3, "Bağırsak", "Bagirsak"),
        new Bolum(4, "Kan Damarları\nve Kalp", "KanKalp"),
        new Bolum(5, "Akciğer\n(gezinti)", "Akciger", false),
        new Bolum(6, "Böbrek", "Bobrek"),
        new Bolum(7, "Mesane\nve Çıkış", "Mesane"),
    };
}
