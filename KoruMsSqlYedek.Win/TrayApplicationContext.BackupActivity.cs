using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win
{
    partial class TrayApplicationContext
    {
        #region Backup Activity

        private void OnPipeConnectionChanged(object sender, bool connected)
        {
            // Arka plan thread'inden gelebilir — UI thread'e aktar
            if (SynchronizationContext.Current != _uiContext)
            {
                _uiContext.Post(_ => OnPipeConnectionChanged(sender, connected), null);
                return;
            }

            if (connected)
            {
                UpdateTrayStatus(TrayIconStatus.Idle, Res.Get("Tray_Tooltip"));
                Theme.ModernToast.Success(
                    Res.Get("Tray_ServiceConnectionTitle"),
                    Res.Get("Tray_ServiceConnected"));
                Log.Information("Servis pipe bağlandı.");
            }
            else
            {
                UpdateTrayStatus(TrayIconStatus.Disconnected, Res.Get("Tray_TooltipDisconnected"));
                ShowBalloonTip(
                    Res.Get("Tray_ServiceConnectionTitle"),
                    Res.Get("Tray_ServiceDisconnected"),
                    ToolTipIcon.Warning, 3000);
                Log.Warning("Servis pipe bağlantısı kesildi.");
            }
        }

        private void OnBackupActivityChanged(object sender, BackupActivityEventArgs e)
        {
            // Arka plan thread'inden gelebilir — UI thread'e aktar
            if (SynchronizationContext.Current != _uiContext)
            {
                _uiContext.Post(_ => OnBackupActivityChanged(sender, e), null);
                return;
            }

            switch (e.ActivityType)
            {
                case BackupActivityType.Started:
                    StartTrayAnimation(Res.Format("Tray_BackupRunning", e.PlanName));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupStartedTitle"),
                            Res.Format("Toast_BackupStartedMessage", e.PlanName),
                            ToolTipIcon.Info);
                    break;

                case BackupActivityType.Completed:
                    StopTrayAnimation(TrayIconStatus.Success,
                        Res.Format("Tray_BackupCompleted", e.PlanName));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupCompletedTitle"),
                            Res.Format("Toast_BackupCompletedMessage", e.PlanName),
                            ToolTipIcon.Info);
                    break;

                case BackupActivityType.Failed:
                    StopTrayAnimation(TrayIconStatus.Error,
                        Res.Format("Tray_BackupFailed", e.PlanName));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupFailedTitle"),
                            Res.Format("Toast_BackupFailedMessage", e.PlanName),
                            ToolTipIcon.Error);
                    break;

                case BackupActivityType.Cancelled:
                    StopTrayAnimation(TrayIconStatus.Idle, Res.Get("Tray_Tooltip"));
                    if (e.ToastEnabled)
                        ShowBalloonTip(
                            Res.Get("Toast_BackupCancelledTitle"),
                            Res.Format("Toast_BackupCancelledMessage", e.PlanName),
                            ToolTipIcon.Warning);
                    break;
            }
        }

        private void OnAnimTimerTick(object sender, EventArgs e)
        {
            if (_animFrames == null) return;
            _animFrameIndex = (_animFrameIndex + 1) % _animFrames.Length;
            _notifyIcon.Icon = _animFrames[_animFrameIndex];
        }

        private void OnCompletionTimerTick(object sender, EventArgs e)
        {
            if (_completionFrames == null) return;
            _completionFrameIndex++;

            if (_completionFrameIndex >= _completionFrames.Length)
            {
                // Animasyon tamamlandı — idle'a dön
                StopCompletionAnimation();
                return;
            }

            _notifyIcon.Icon = _completionFrames[_completionFrameIndex];
        }

        private void StartTrayAnimation(string tooltipText)
        {
            if (_isAnimating) return;

            // Tamamlanma animasyonu çalışıyorsa önce durdur
            if (_isCompletionAnimating)
                StopCompletionAnimation();

            _animFrames = SymbolIconHelper.ExtractGifFrames("CloudSync.gif");
            _animFrameIndex = 0;
            _isAnimating = true;

            if (tooltipText != null)
                _notifyIcon.Text = tooltipText.Length > 63
                    ? tooltipText.Substring(0, 63)
                    : tooltipText;

            _notifyIcon.Icon = _animFrames[0];
            _animTimer.Start();
        }

        private void StopTrayAnimation(TrayIconStatus finalStatus, string tooltipText)
        {
            if (!_isAnimating) return;
            _animTimer.Stop();
            _isAnimating = false;

            var localFrames = _animFrames;
            int lastIndex = _animFrameIndex;
            _animFrames = null;

            // Başarılı tamamlandıysa kısa check-mark animasyonu göster
            if (finalStatus == TrayIconStatus.Success)
            {
                StartCompletionAnimation(tooltipText);
            }
            else
            {
                UpdateTrayStatus(finalStatus, tooltipText);
            }

            // Eski kareleri temizle
            DisposeFrames(localFrames, lastIndex);
        }

        /// <summary>
        /// Check-mark GIF animasyonunu başlatır; bitince idle ikona döner.
        /// </summary>
        private void StartCompletionAnimation(string tooltipText)
        {
            _completionFrames = SymbolIconHelper.ExtractGifFrames("CheckMark.gif");
            _completionFrameIndex = 0;
            _isCompletionAnimating = true;

            if (tooltipText != null)
                _notifyIcon.Text = tooltipText.Length > 63
                    ? tooltipText.Substring(0, 63)
                    : tooltipText;

            _notifyIcon.Icon = _completionFrames[0];
            _completionTimer.Start();
        }

        private void StopCompletionAnimation()
        {
            _completionTimer.Stop();
            _isCompletionAnimating = false;

            var localFrames = _completionFrames;
            int lastIndex = _completionFrameIndex;
            _completionFrames = null;

            // Idle ikona geri dön
            UpdateTrayStatus(TrayIconStatus.Idle, Res.Get("Tray_Tooltip"));

            DisposeFrames(localFrames, lastIndex);
        }

        private static void DisposeFrames(Icon[] frames, int skipIndex)
        {
            if (frames == null) return;
            for (int i = 0; i < frames.Length; i++)
            {
                if (i == skipIndex) continue;
                try { frames[i].Dispose(); } catch { }
            }
        }

        #endregion
    }
}
