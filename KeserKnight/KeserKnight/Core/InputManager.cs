using System;
using System.Windows.Forms;
using KeserKnight.Entity;
using KeserKnight.Combat;

namespace KeserKnight.Core
{
    public class InputManager
    {
        public void HandleMenuInput(KeyEventArgs e, ref int menuSelection, Action onStartGame)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) menuSelection = 1;
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) menuSelection = 0;

            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                if (menuSelection == 0) onStartGame?.Invoke();
                else if (menuSelection == 1) Application.Exit();
            }
        }

        public void HandleGameKeyDown(KeyEventArgs e, Player player, AttackSystem attackSystem, bool isGameOver, Action onResetGame)
        {
            if (isGameOver && e.KeyCode == Keys.R)
            {
                onResetGame?.Invoke();
                return;
            }

            if (isGameOver) return;

            if (e.KeyCode == Keys.A) player.MoveLeft = true;
            if (e.KeyCode == Keys.D) player.MoveRight = true;

            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.W)
            {
                if (!player.IsJumping)
                {
                    player.VerticalVelocity = player.JumpPower;
                    player.IsJumping = true;
                }
            }

            if (e.KeyCode == Keys.L) attackSystem.HandleAttackInput(player);
        }

        public void HandleGameKeyUp(KeyEventArgs e, Player player)
        {
            if (e.KeyCode == Keys.A) player.MoveLeft = false;
            if (e.KeyCode == Keys.D) player.MoveRight = false;
        }

        // WinForms'un tuşları yutmasını engelleyen ProcessCmdKey Mantığı
        public bool HandleProcessCmdKey(Keys keyData, ref int pauseSelection, ref string gameStateStr, Action resumeAction, Action mainMenuAction)
        {
            // 1. Oyun oynanırken ESC basılırsa oyunu durdur
            if (gameStateStr == "Playing" && keyData == Keys.Escape)
            {
                gameStateStr = "Paused";
                pauseSelection = 0;
                return true;
            }

            // 2. Oyun zaten duraklatılmışsa (ESC Menüsündeyken)
            else if (gameStateStr == "Paused")
            {
                if (keyData == Keys.Escape)
                {
                    resumeAction?.Invoke();
                    gameStateStr = "Playing";
                    return true;
                }

                // Yukarı Taşıma (W veya Yukarı Ok)
                if (keyData == Keys.W || keyData == Keys.Up)
                {
                    pauseSelection = (pauseSelection - 1 + 3) % 3;
                    return true;
                }

                // Aşağı Taşıma (S... Veya Aşağı Ok)
                if (keyData == Keys.S || keyData == Keys.Down)
                {
                    pauseSelection = (pauseSelection + 1) % 3;
                    return true;
                }

                // --- ENTER VE SPACE TETİKLEME MOTORU ---
                if (keyData == Keys.Enter || keyData == Keys.Space)
                {
                    if (pauseSelection == 0) // DEVAM ET
                    {
                        resumeAction?.Invoke();
                        gameStateStr = "Playing";
                    }
                    else if (pauseSelection == 1) // AYARLAR
                    {
                        MessageBox.Show("Ayarlar Menüsü Yakında Eklenecek Usta!", "KeserKnight Ayarlar");
                    }
                    else if (pauseSelection == 2) // ANA MENÜ
                    {
                        mainMenuAction?.Invoke();
                        gameStateStr = "MainMenu";
                    }
                    return true; // Tuşu başarıyla işledik, Windows başka yere odaklanmasın usta
                }
            }
            return false;
        }
    }
}