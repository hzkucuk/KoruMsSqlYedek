# 📊 Modül Stabilite Haritası

> Son güncelleme: 2025-07-20 — v0.57.0  
> Derecelendirme: 🟢 Stabil | 🟡 Yeni / Test Edilmeli | 🔴 Deneysel / Eksik

---

## Core Katmanı (`KoruMsSqlYedek.Core`)

| Modül | Durum | Açıklama |
|-------|-------|----------|
| Interfaces (15 adet) | 🟢 Stabil | Kontrat katmanı, nadiren değişir |
| Models / Enums | 🟢 Stabil | BackupPlan, BackupResult, ConfigModels sabit yapıda |
| Events (BackupActivityEvent) | 🟡 Yeni | v0.53.0'da HasFileBackup eklendi, genişlemeye açık |
| IPC (PipeProtocol) | 🟢 Stabil | v0.57.0 test kapsamı eklendi (18 PipeProtocol + 20 CancellationRegistry testi) |
| Helpers | 🟢 Stabil | PasswordProtector, PathSanitizer — değişim az |

---

## Engine Katmanı (`KoruMsSqlYedek.Engine`)

### Yedekleme Pipeline'ı

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| SqlBackupService | 🟢 Stabil | v0.44.0 | SMO tabanlı, Full/Diff/Log destekli, doğrulanmış |
| BackupChainValidator | 🟢 Stabil | v0.44.0 | Zincir bütünlüğü kontrolü |
| BackupJobExecutor | 🟢 Stabil | v0.57.0 | Cancel cleanup + 8 yeni test ile doğrulandı |
| SevenZipCompressionService | 🟢 Stabil | v0.52.0 | Şifreli sıkıştırma, ara dosya temizliği |

### Dosya Yedekleme

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| FileBackupService | 🟢 Stabil | v0.52.0 | Klasör yedekleme, v0.43.0 kök neden düzeltmesi |
| VssSnapshotService | 🟡 Yeni | v0.47.0 | AlphaVSS entegrasyonu, kilitli dosya desteği |

### Bulut Yükleme

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| CloudUploadOrchestrator | 🟢 Stabil | v0.48.0 | Çoklu hedef, PlanId izolasyonu |
| GoogleDriveProvider | 🟢 Stabil | v0.44.1 | OAuth2, klasör oluşturma düzeltmesi |
| OneDriveProvider | 🟢 Stabil | v0.46.0 | MSAL tabanlı, büyük dosya yükleme |
| FtpSftpProvider | 🟢 Stabil | v0.46.0 | SSH.NET tabanlı |
| LocalNetworkProvider | 🟢 Stabil | v0.46.0 | UNC path + credential desteği |
| UploadStateManager | 🟢 Stabil | v0.44.1 | Resume desteği |
| CloudProviderFactory | 🟢 Stabil | v0.46.0 | Provider çözümleme |

### Zamanlama & Yaşam Döngüsü

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| QuartzSchedulerService | 🟢 Stabil | v0.46.0 | Quartz.NET, eşzamanlı yedekleme |
| RetentionCleanupService | 🟢 Stabil | v0.46.0 | Politika tabanlı temizlik |
| PlanManager | 🟢 Stabil | v0.42.9 | JSON plan yönetimi |
| AppSettingsManager | 🟢 Stabil | v0.42.9 | Uygulama ayarları |
| BackupHistoryManager | 🟢 Stabil | v0.42.9 | Geçmiş kaydı |

### Bildirim & Raporlama

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| EmailNotificationService | 🟡 Yeni | v0.46.0 | MailKit tabanlı, şablon desteği eksik |
| ReportingService | 🟡 Yeni | v0.46.0 | Temel rapor, detay eksik |

---

## Win Katmanı (`KoruMsSqlYedek.Win`)

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| MainWindow (Log + Grid) | 🟡 Dikkat | v0.53.1 | 4 sorumluluk (buffer+UI+renk+ilerleme), kırılganlık riski |
| PlanProgressTracker | 🟢 Stabil | v0.53.0 | SQL/File/VSS ağırlık modeli |
| TrayApplicationContext | 🟢 Stabil | v0.51.0 | Tray ikonu, sidebar, servis kontrolü |
| PlanEditForm | 🟢 Stabil | v0.46.0 | Plan düzenleme |
| RestoreDialog | 🟢 Stabil | v0.57.0 | .7z desteği, lokalizasyon, iptal UX tamamlandı |
| ManualBackupDialog | 🟢 Stabil | v0.46.0 | Elle tetikleme |
| CloudTargetEditDialog | 🟢 Stabil | v0.46.0 | Bulut hedef düzenleme |
| SmtpProfileEditDialog | 🟢 Stabil | v0.46.0 | SMTP profil düzenleme |
| FileBackupSourceEditDialog | 🟢 Stabil | v0.43.0 | Dosya kaynak düzenleme |
| Theme System (20+ kontrol) | 🟢 Stabil | v0.51.0 | Dark/Light, terminal renk şeması |
| ServicePipeClient | 🟡 Yeni | v0.54.0 | ACL düzeltmesi sonrası, üretim testi gerekli |

---

## Service Katmanı (`KoruMsSqlYedek.Service`)

| Modül | Durum | Son Değişiklik | Açıklama |
|-------|-------|----------------|----------|
| BackupWindowsService | 🟢 Stabil | v0.46.0 | Windows Service host |
| ServicePipeServer | 🟡 Yeni | v0.54.0 | PipeSecurity ACL fix, üretim testi gerekli |

---

## Test Katmanı (`KoruMsSqlYedek.Tests`)

| Modül | Test Sayısı | Kapsam | Açıklama |
|-------|-------------|--------|----------|
| PlanManagerTests | ✅ | CRUD | Plan yönetimi |
| AppSettingsTests | ✅ | Okuma/Yazma | Ayar yönetimi |
| BackupHistoryTests | ✅ | Kayıt | Geçmiş kaydı |
| BackupChainValidatorTests | ✅ | Doğrulama | Zincir bütünlüğü |
| CloudUploadOrchestratorTests | ✅ | Çoklu hedef | Orkestrasyon |
| GoogleDriveProviderTests | ✅ | Upload/Auth | Google Drive |
| OneDriveProviderTests | ✅ | Upload/Auth | OneDrive |
| FtpSftpProviderTests | ✅ | Upload | FTP/SFTP |
| LocalNetworkProviderTests | ✅ | Upload | Ağ paylaşımı |
| CloudProviderFactoryTests | ✅ | Çözümleme | Provider factory |
| FileBackupServiceTests | ✅ | Kopyalama | Dosya yedekleme |
| EmailNotificationTests | ✅ | Gönderim | E-posta |
| ReportingServiceTests | ✅ | Rapor | Raporlama |
| RetentionCleanupTests | ✅ | Temizlik | Saklama politikası |
| BackupJobExecutorTests | ✅ | Pipeline + Cancel | Yedekleme akışı + iptal/temizlik (8 yeni) |
| PasswordProtectorTests | ✅ | Şifreleme | Parola koruma |
| PipeProtocolTests | ✅ | IPC Seri/Deseri | 18 mesaj türü, roundtrip, kenar durumu |
| BackupCancellationRegistryTests | ✅ | İptal Kayıt | 20 test — Register/Cancel/Unregister, thread safety |

> **Eksik test alanları:** VSS Snapshot, Restore Dialog, Progress Tracker

---

## Özet İstatistikler

| Metrik | Değer |
|--------|-------|
| Toplam proje | 5 |
| Toplam kaynak dosya | ~120 |
| Toplam test dosyası | 19 |
| 🟢 Stabil modül | 31 |
| 🟡 Yeni / Test edilmeli | 6 |
| 🔴 Deneysel / Eksik | 0 |
| Son sürüm | v0.55.0 |
| İlk izlenen sürüm | v0.42.9 |
