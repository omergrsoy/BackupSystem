# 🛡️ Enterprise Backup System (Kurumsal Dosya Yedekleme Sistemi)

Bu proje, staj kapsamında geliştirilmiş, **Client-Server (İstemci-Sunucu)** mimarisiyle çalışan profesyonel ve ölçeklenebilir bir dosya yedekleme ve yönetim sistemidir. 

Uç noktalardaki (Windows/Linux) makineler, arka planda çalışan bir Agent servisi ile merkezi sunucuya bağlanır ve gelişmiş algoritmalarla yedekleme işlemlerini güvenli bir şekilde gerçekleştirir.

## 🌟 Öne Çıkan Özellikler

Sistem, temel yedekleme işlevlerinin ötesine geçerek aşağıdaki **Enterprise (Kurumsal)** özellikleri barındırır:

* **📊 Merkezi Yönetim Paneli (Dashboard):** Tüm makinelerin çevrimiçi/çevrimdışı durumlarını, toplam makine ve yedek sayılarını anlık olarak gösteren dinamik MVC web arayüzü.
* **🧠 Gelişmiş Yedekleme Algoritmaları:** * **Tam (Full):** Hedef dizindeki tüm dosyaları yedekler.
  * **Artımlı (Incremental):** Sadece en son alınan başarılı yedekten sonra değişen dosyaları yedekler, ağ trafiğini rahatlatır.
  * **Fark (Differential):** Sadece en son alınan *Tam* yedekten bu yana değişen dosyaları yedekler.
* **🔒 Askeri Düzey Güvenlik (AES-256):** Veriler Agent makinesinde zıplenip **AES-256** ile şifrelenir (`.enc` formatı). Sunucuya şifreli olarak iletilir ve fiziksel sunucuda kırılamaz halde saklanır. Yalnızca yetkili yönetici web arayüzünden "İndir" dediğinde anında (on-the-fly) şifre çözülür.
* **📦 Parçalı Yükleme (Chunked Upload):** Devasa boyuttaki yedek dosyaları (GB'larca veri), sunucu RAM'ini şişirmemek ve zaman aşımını (timeout) engellemek için 10 MB'lık parçalar halinde sunucuya iletilir ve diskte birleştirilir.
* **🧹 Otomatik Veri Temizliği (Retention Policy):** Sunucu diskini korumak amacıyla arka planda çalışan `BackgroundService`, 7 günden eski yedekleri veritabanından ve fiziksel diskten otomatik olarak siler.
* **⏱️ Heartbeat (Düzenli Bildirim):** Agent'lar belirli aralıklarla sunucuya ping atarak hayatta olduklarını bildirir. İletişimi kopan makineler panelde "Çevrimdışı" statüsüne düşer.

## 🛠️ Kullanılan Teknolojiler

* **Sunucu / Yönetim Paneli:** ASP.NET Core MVC (.NET 8.0)
* **Agent / İstemci:** .NET Worker Service (Background Service)
* **Veritabanı:** Microsoft SQL Server (LocalDB) & Entity Framework Core (Code-First)
* **Arayüz (UI):** HTML5, CSS3 (Flexbox Mimarisi, Özel Tasarım CSS Sınıfları), Bootstrap 5 (Dropdown)
* **Sıkıştırma ve Şifreleme:** `System.IO.Compression.ZipFile`, `System.Security.Cryptography.Aes`

## ⚙️ Kurulum ve Çalıştırma
Projenin bilgisayarınızda sorunsuz çalışması için aşağıdaki adımları izleyin:
### 1. Veritabanını Ayağa Kaldırma
MVC Sunucu projesinin (`BackupSystem.Server`) veritabanını oluşturmak için **Package Manager Console** üzerinden şu komutu çalıştırın:
```powershell
Update-Database

## 2. Agent Ayarlarını Yapılandırma
BackupSystem.Agent projesi içindeki appsettings.json dosyasını açıp kendi ortamınıza göre güncelleyin:
JSON
"AgentSettings": {
    "ServerBaseUrl": "https://localhost:XXXX", // MVC Projenizin URL'si
    "MachineId": 1, // Eklediğiniz makinenin ID'si
    "TargetDirectory": "C:\\Yedekler" // Yedeklenecek test klasörünüz
}
## 3. Çoklu Başlangıç (Multiple Startup) Ayarı
Sistemin doğru çalışması için her iki projenin de aynı anda başlaması gerekir:
Solution'a (BackupSystem) sağ tıklayıp Properties (Özellikler) menüsüne girin.
Startup Project (Başlangıç Projesi) sekmesinden Multiple startup projects seçeneğini işaretleyin.
Hem Server hem de Agent projeleri için "Action" kısmını Start (Başlat) olarak ayarlayın ve kaydedin.

## 4. Başlat
F5 tuşuna basarak projeyi başlatın. Arayüzden makineleri görüntüleyebilir, yeni yedekleme istekleri (Tam, Artımlı, Fark) gönderebilir ve şifrelenmiş yedeklerinizi bilgisayarınıza çözülmüş olarak indirebilirsiniz.

Geliştirici: [Senin Adın/Soyadın]

Proje Kapsamı: Staj Programı
