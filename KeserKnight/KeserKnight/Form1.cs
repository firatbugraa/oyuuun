using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using KeserKnight.Entity;
using KeserKnight.Map;
using KeserKnight.UI;
using KeserKnight.Core;
using KeserKnight.Combat;

namespace KeserKnight
{
    public partial class Form1 : Form
    {
        // Sanal Arabellek (Virtual Canvas) Yapısı
        private Bitmap virtualCanvas;
        private Graphics canvasGraphics;
        private int targetWidth = 1920;
        private int targetHeight = 1080;

        // SOLID Sistem Referansları
        Player player;
        PhysicsEngine physicsEngine;
        AttackSystem attackSystem;
        RoomManager roomManager;
        InputManager inputManager;

        // Harita ve Arayüz Listeleri
        List<Rectangle> platforms = new List<Rectangle>();
        List<Enemy> enemies = new List<Enemy>();
        List<Gold> roomGolds = new List<Gold>();

        bool isGameOver = false;
        int totalGold = 0;
        DateTime lastF11Time = DateTime.MinValue;
        int menuSelection = 0;
        int pauseSelection = 0;

        public enum GameState { MainMenu, Playing, Paused }
        public GameState currentGameState = GameState.MainMenu;

        // Sabit Buton Koordinat Alanları
        Rectangle startButton = new Rectangle(810, 500, 300, 60);
        Rectangle exitButton = new Rectangle(810, 600, 300, 60);
        Rectangle resumeButton = new Rectangle(810, 450, 300, 60);
        Rectangle settingsButton = new Rectangle(810, 540, 300, 60);
        Rectangle mainMenuButton = new Rectangle(810, 630, 300, 60);

        // Grafik Kaynakları
        Image playerImage = Properties.Resources.anakarakter;
        Image kalpDolu = Properties.Resources.kalp_dolu;
        Image kalpBos = Properties.Resources.kalp_bos;

        void SyncLoadRoom()
        {
            GameMap.LoadRoom(roomManager.CurrentRoom, platforms, enemies, roomGolds);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                if ((DateTime.Now - lastF11Time).TotalMilliseconds < 800) return;
                lastF11Time = DateTime.Now;

                if (this.FormBorderStyle == FormBorderStyle.None) SetScreenMode(false);
                else SetScreenMode(true);
                return;
            }

            if (currentGameState == GameState.MainMenu)
            {
                inputManager.HandleMenuInput(e, ref menuSelection, () => {
                    ResetGame();
                    currentGameState = GameState.Playing;
                    this.Focus();
                });
                this.Invalidate();
                return;
            }

            if (currentGameState != GameState.Playing) return;

            inputManager.HandleGameKeyDown(e, player, attackSystem, isGameOver, () => ResetGame());
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (currentGameState == GameState.Playing)
            {
                inputManager.HandleGameKeyUp(e, player);
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            float scaleX = (float)this.ClientSize.Width / targetWidth;
            float scaleY = (float)this.ClientSize.Height / targetHeight;
            Point virtualClickPoint = new Point((int)(e.X / scaleX), (int)(e.Y / scaleY));

            if (currentGameState == GameState.MainMenu)
            {
                if (startButton.Contains(virtualClickPoint))
                {
                    ResetGame();
                    currentGameState = GameState.Playing;
                    this.Focus();
                }
                else if (exitButton.Contains(virtualClickPoint)) Application.Exit();
            }
            else if (currentGameState == GameState.Paused)
            {
                if (resumeButton.Contains(virtualClickPoint)) currentGameState = GameState.Playing;
                else if (settingsButton.Contains(virtualClickPoint)) MessageBox.Show("Ayarlar Menüsü Yakında Eklenecek Usta!", "KeserKnight Ayarlar");
                else if (mainMenuButton.Contains(virtualClickPoint)) { currentGameState = GameState.MainMenu; menuSelection = 0; }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (currentGameState != GameState.Playing) { this.Invalidate(); return; }
            if (isGameOver) return;

            player.Update();
            physicsEngine.Update(player, platforms);

            if (player.Y > 1050)
            {
                isGameOver = true;
                this.Invalidate();
                return;
            }

            attackSystem.UpdateAttackHitbox(player);
            attackSystem.CheckEnemyCollisions(player, enemies);

            bool roomChanged = roomManager.Update(player, platforms, enemies, roomGolds);
            if (roomChanged)
            {
                this.Focus();
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                enemy.Update();

                if (player.Hitbox.IntersectsWith(enemy.Hitbox))
                {
                    bool dead = player.TakeDamage();
                    if (dead) isGameOver = true;
                    else if (player.IsInvincible && player.InvincibilityTimer == 0)
                    {
                        player.VerticalVelocity = -15;
                        if (player.CurrentDirection == Player.Direction.Left) player.X += 40; else player.X -= 40;
                    }
                    break;
                }
            }

            for (int i = roomGolds.Count - 1; i >= 0; i--)
            {
                if (player.Hitbox.IntersectsWith(roomGolds[i].Hitbox))
                {
                    totalGold += roomGolds[i].Value;
                    roomGolds.RemoveAt(i);
                }
            }

            this.Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (canvasGraphics == null || virtualCanvas == null) return;

            canvasGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            canvasGraphics.PixelOffsetMode = PixelOffsetMode.Half;

            if (currentGameState == GameState.MainMenu)
            {
                MainMenuUI.Draw(canvasGraphics, menuSelection, startButton, exitButton);
            }
            else
            {
                canvasGraphics.Clear(Color.FromArgb(20, 24, 43));

                foreach (var platform in platforms) canvasGraphics.FillRectangle(Brushes.LightSlateGray, platform);

                foreach (var enemy in enemies) enemy.Draw(canvasGraphics);
                foreach (var gold in roomGolds) gold.Draw(canvasGraphics);

                if (playerImage != null)
                {
                    // drawRect genişliğini direkt player.Hitbox.Width yaparak fizikle resmi %100 eşitledik usta!
                    Rectangle drawRect = new Rectangle(
                        player.Hitbox.X,
                        player.Hitbox.Y,
                        player.Hitbox.Width, // Genişlik artık tam 80 piksel, sünme bitti!
                        player.Hitbox.Height
                    );

                    if (player.CurrentDirection == Player.Direction.Left)
                    {
                        GraphicsState state = canvasGraphics.Save();
                        canvasGraphics.TranslateTransform(drawRect.X + drawRect.Width, drawRect.Y);
                        canvasGraphics.ScaleTransform(-1, 1);
                        canvasGraphics.DrawImage(playerImage, 0, 0, drawRect.Width, drawRect.Height);
                        canvasGraphics.Restore(state);
                    }
                    else
                    {
                        canvasGraphics.DrawImage(playerImage, drawRect);
                    }
                }

                else canvasGraphics.FillRectangle(Brushes.Black, player.Hitbox);

                if (player.IsAttacking && !player.AttackHitbox.IsEmpty)
                {
                    using (SolidBrush attackBrush = new SolidBrush(Color.FromArgb(150, Color.Yellow))) canvasGraphics.FillRectangle(attackBrush, player.AttackHitbox);
                    canvasGraphics.DrawRectangle(Pens.Red, player.AttackHitbox);
                }

                HUDRenderer.Draw(canvasGraphics, roomManager.CurrentRoom, player.MaxHealth, player.CurrentHealth, totalGold, kalpDolu, kalpBos);

                if (player.IsInvincible && (player.InvincibilityTimer % 4 == 0))
                {
                    using (SolidBrush damageFilter = new SolidBrush(Color.FromArgb(100, Color.Red))) canvasGraphics.FillRectangle(damageFilter, player.Hitbox);
                }

                if (currentGameState == GameState.Paused)
                {
                    PauseMenuUI.Draw(canvasGraphics, pauseSelection, resumeButton, settingsButton, mainMenuButton);
                }

                if (isGameOver) GameOverUI.Draw(canvasGraphics);
            }

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(virtualCanvas, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
        }

        void ResetGame()
        {
            isGameOver = false;
            totalGold = 0;
            GameMap.ResetWorld();
            roomManager.Reset();
            player.Reset(300, 750);
            SyncLoadRoom();
        }

        void SetScreenMode(bool fullscreen)
        {
            if (fullscreen)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.WindowState = FormWindowState.Normal;
                this.ClientSize = new Size(1280, 720);
                this.StartPosition = FormStartPosition.CenterScreen;
            }
            this.Refresh();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool handled = inputManager.HandleProcessCmdKey(keyData, ref pauseSelection, ref currentGameState,
                () => {
                    currentGameState = GameState.Playing;
                    this.Focus();
                },
                () => {
                    currentGameState = GameState.MainMenu;
                    menuSelection = 0;
                    this.Focus();
                }
            );

            if (handled)
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public Form1()
        {
            InitializeComponent();
            GameMap.InitializeWorld();

            virtualCanvas = new Bitmap(targetWidth, targetHeight);
            canvasGraphics = Graphics.FromImage(virtualCanvas);
            SetScreenMode(true);

            // Oyuncu boyutu senin girdiğin gibi 130x130 usta
            player = new Player(300, 750, 130, 130);
            physicsEngine = new PhysicsEngine();
            attackSystem = new AttackSystem();
            roomManager = new RoomManager(targetWidth, targetHeight);
            inputManager = new InputManager();

            this.DoubleBuffered = true;
            this.MaximizeBox = false;
            this.KeyPreview = true;
            this.Focus();

            SyncLoadRoom();
        }
    }
}