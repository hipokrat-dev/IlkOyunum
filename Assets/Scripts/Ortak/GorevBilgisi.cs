public static class GorevBilgisi
{
    public static string Hedef(int no)
    {
        switch(no){
            case 1:return "Dişlerin çevresindeki 10 mikrop kolonisini köpükle temizle.";
            case 2:return "Lokmalardan kaçınarak mideye ulaş. Asidi köpükle nötrleyip çıkışı bul.";
            case 3:return "Zararlı bakterileri temizle, 5 dost koloniyi koru ve bağırsak çıkışına ulaş.";
            case 4:return "Kan akışıyla kalbe ulaş. Açılan kapaklardan doğru anda geç.";
            case 5:return "Alveolleri ve oksijen alışverişini gözlemle. Bu durakta araç otomatik ilerler.";
            case 6:return "Su, glikoz ve tuzu geri kazan; zararlı atıklardan uzak dur.";
            default:return "Dibe dal, 7 kristali köpükle erit ve açılan çıkıştan vücudu terk et.";
        }
    }
    public static string Dikkat(int no)
    {
        switch(no){
            case 1:return "Dalgaya yakalanmak canını azaltır. Boş taraftan geç veya üzerinden zıpla.";
            case 2:return "Asitte kaldıkça canın azalır. Köpük güvenli geçiş için kısa süre kazandırır.";
            case 3:return "Mavi dost bakterilere köpük atma; onları kuşatan kırmızıları hedefle.";
            case 4:return "Kapak yeşilken atıl. Akyuvarlara köpük atarak kapsülü tanıt.";
            case 5:return "Kontrol gerekmiyor. Profesörün anlatımını dinle ve çevreyi izle.";
            case 6:return "Mavi, sarı ve beyaz maddeler yararlı; kahverengi ve mor olanlardan kaçın.";
            default:return "DAL basılıyken batarsın, bırakınca yükselirsin. Her kristal 3 vuruş ister.";
        }
    }
    public static string Kontrol(int no)
    {
        bool mobil=UnityEngine.Application.isMobilePlatform || DokunmatikGirdi.Etkin;
        if(no==1)return mobil ? "SOL: SÜR  •  SAĞ: KÖPÜK  •  ZIPLA: DOKUN" : "WASD: SÜR  •  BOŞLUK: ZIPLA  •  CTRL / COMMAND: KÖPÜK";
        if(no==5)return "OTOMATİK GEZİNTİ  •  İZLE VE KEŞFET";
        if(mobil)return no==7 ? "SOL: SÜR   •   SAĞ: KÖPÜK   •   DAL: BASILI TUT" : no==6 ? "SOL ALANDA PARMAĞINI KAYDIRARAK SÜZÜL" : "SOL: SÜRÜŞ   •   SAĞ: KÖPÜK / ATIL";
        return no==7 ? "WASD: SÜR   •   BOŞLUK: KÖPÜK   •   SHIFT: DAL" : no==6 ? "WASD / OKLAR: SÜZÜL" : "WASD: SÜR   •   BOŞLUK: KÖPÜK / ATIL";
    }
}
