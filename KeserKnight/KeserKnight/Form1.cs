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
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private Bitmap virtualCanvas;
        private Graphics canvasGraphics;
        private int targetWidth = 1920;
        private int targetHeight = 1080;

        Player player;
        PhysicsEngine physicsEngine;
        AttackSystem attackSystem;
        RoomManager roomManager;
        InputManager inputManager;

        List<Rectangle> platforms = new List<Rectangle>();
        List<Enemy> enemies = new List<Enemy>();
        List<Gold> roomGolds = new List<Gold>();
        List<BreakableBlock> breakableBlocks = new List<BreakableBlock>();
        List<TimedBlock> timedBlocks = new List<TimedBlock>();
        List<MovingPlatform> movingPlatforms = new List<MovingPlatform>();
        List<Rectangle> roomLadders = new List<Rectangle>();

        CheckpointTorch checkpointTorch;
        GuardianGriffon miniBoss;

        public static List<Rectangle> GlobalPlatforms;
        bool isGameOver = false;
        int totalGold = 0;
        DateTime lastF11Time = DateTime.MinValue;
        int menuSelection = 0;
        int pauseSelection = 0;
        bool isGodMode = false;
        bool isFlyMode = false;

        public enum GameState { MainMenu, Playing, Paused }
        public GameState currentGameState = GameState.MainMenu;

        Rectangle startButton = new Rectangle(810, 500, 300, 60);
        Rectangle exitButton = new Rectangle(810, 600, 300, 60);
        Rectangle resumeButton = new Rectangle(810, 450, 300, 60);
        Rectangle settingsButton = new Rectangle(810, 540, 300, 60);
        Rectangle mainMenuButton = new Rectangle(810, 630, 300, 60);

        Image playerImage = Properties.Resources.anakarakter;
        Image kalpDolu = Properties.Resources.kalp_dolu;
        Image kalpBos = Properties.Resources.kalp_bos;

        void SyncLoadRoom()
        {
            List<Rectangle> loadedLadders = new List<Rectangle>();
            GameMap.LoadRoom(roomManager.CurrentRoom, platforms, enemies, roomGolds, breakableBlocks, timedBlocks, movingPlatforms, out loadedLadders);
            roomLadders.Clear();
            if (loadedLadders != null) roomLadders.AddRange(loadedLadders);

            //  MEŞALE KONTROLÜ: Hem Oda 9'un hem de Oda 11'in başına meşale çak 
            if (roomManager.CurrentRoom == 9)
                checkpointTorch = new CheckpointTorch(200, 770, 35, 80);
            else if (roomManager.CurrentRoom == 11)
                checkpointTorch = new CheckpointTorch(350, 570, 35, 80);
            else
                checkpointTorch = null;

            miniBoss = GameMap.GetBossInstance(roomManager.CurrentRoom);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                if ((DateTime.Now - lastF11Time).TotalMilliseconds < 800) return;
                lastF11Time = DateTime.Now;
                if (this.FormBorderStyle == FormBorderStyle.None) SetScreenMode(false); else SetScreenMode(true);
                return;
            }
            if (e.KeyCode == Keys.G) { if (currentGameState == GameState.Playing) { isGodMode = !isGodMode; UpdateWindowTitle(); } return; }
            if (e.KeyCode == Keys.H) { if (currentGameState == GameState.Playing) { isFlyMode = !isFlyMode; player.VerticalVelocity = 0; UpdateWindowTitle(); } return; }
            if (currentGameState == GameState.MainMenu) { inputManager.HandleMenuInput(e, ref menuSelection, () => { ResetGame(); currentGameState = GameState.Playing; this.Focus(); }); this.Invalidate(); return; }
            if (currentGameState != GameState.Playing) return;
            inputManager.HandleGameKeyDown(e, player, attackSystem, isGameOver, () => ResetGame());
        }

        private void UpdateWindowTitle()
        {
            string title = "KeserKnight";
            if (isGodMode) title += " - [ÖLÜMSÜZLÜK AKTİF]";
            if (isFlyMode) title += " - [UÇMA AKTİF]";
            this.Text = title;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e) { if (currentGameState == GameState.Playing) inputManager.HandleGameKeyUp(e, player); }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            float scaleX = (float)this.ClientSize.Width / targetWidth; float scaleY = (float)this.ClientSize.Height / targetHeight;
            Point virtualClickPoint = new Point((int)(e.X / scaleX), (int)(e.Y / scaleY));
            if (currentGameState == GameState.MainMenu) { if (startButton.Contains(virtualClickPoint)) { ResetGame(); currentGameState = GameState.Playing; this.Focus(); } else if (exitButton.Contains(virtualClickPoint)) Application.Exit(); }
            else if (currentGameState == GameState.Paused) { if (resumeButton.Contains(virtualClickPoint)) currentGameState = GameState.Playing; else if (settingsButton.Contains(virtualClickPoint)) MessageBox.Show("Ayarlar Menüsü Yakında Eklenecek Usta!", "KeserKnight Ayarlar"); else if (mainMenuButton.Contains(virtualClickPoint)) { currentGameState = GameState.MainMenu; menuSelection = 0; } }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (currentGameState != GameState.Playing) { this.Invalidate(); return; }
            if (isGameOver) return;

            if (isFlyMode)
            {
                int flySpeed = 12;
                if ((GetAsyncKeyState((int)Keys.W) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.Up) & 0x8000) != 0) player.Y -= flySpeed;
                if ((GetAsyncKeyState((int)Keys.S) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.Down) & 0x8000) != 0) player.Y += flySpeed;
                if ((GetAsyncKeyState((int)Keys.A) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.Left) & 0x8000) != 0) player.X -= flySpeed;
                if ((GetAsyncKeyState((int)Keys.D) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.Right) & 0x8000) != 0) player.X += flySpeed;
                player.VerticalVelocity = 0;

                List<Rectangle> tempLadders;
                if (roomManager.Update(player, platforms, enemies, roomGolds, breakableBlocks, timedBlocks, movingPlatforms, out tempLadders)) { roomLadders = tempLadders; HandleRoomTransitionAnimation(); }
                foreach (var tb in timedBlocks) tb.Update(player);
                if (checkpointTorch != null) checkpointTorch.Update();
                this.Invalidate(); return;
            }

            bool isClimbing = false; bool isOnLadder = false;
            foreach (var ladder in roomLadders) { if (player.Hitbox.IntersectsWith(ladder)) { isOnLadder = true; break; } }
            if (isOnLadder)
            {
                bool upPressed = (GetAsyncKeyState((int)Keys.W) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.Up) & 0x8000) != 0;
                bool downPressed = (GetAsyncKeyState((int)Keys.S) & 0x8000) != 0 || (GetAsyncKeyState((int)Keys.Down) & 0x8000) != 0;
                if (upPressed) { player.Y -= 7; player.VerticalVelocity = 0; isClimbing = true; } else if (downPressed) { player.Y += 7; player.VerticalVelocity = 0; } else if (player.VerticalVelocity > 0) player.VerticalVelocity = 0;
            }

            List<Rectangle> tempPhysicalPlatforms = new List<Rectangle>();
            if (!isClimbing)
            {
                tempPhysicalPlatforms.AddRange(platforms);
                foreach (var block in breakableBlocks) { if (!block.IsBroken) tempPhysicalPlatforms.Add(block.Hitbox); }
                foreach (var tBlock in timedBlocks) { if (tBlock.IsActive) tempPhysicalPlatforms.Add(tBlock.Hitbox); }
                foreach (var mp in movingPlatforms) tempPhysicalPlatforms.Add(mp.Hitbox);
            }
            else { foreach (var platform in platforms) { if (platform.Y >= player.Hitbox.Bottom - 5) tempPhysicalPlatforms.Add(platform); } }
            Form1.GlobalPlatforms = tempPhysicalPlatforms;

            player.Update(); physicsEngine.Update(player, tempPhysicalPlatforms);

            if (player.Y > 1020) { if (isGodMode) { player.X = 150; player.Y = 100; player.VerticalVelocity = 0; } else { isGameOver = true; this.Invalidate(); return; } }

            foreach (var tBlock in timedBlocks) tBlock.Update(player);
            foreach (var mp in movingPlatforms) mp.Update(player);
            if (checkpointTorch != null) checkpointTorch.Update();

            attackSystem.UpdateAttackHitbox(player);

            if (player.IsAttacking && !player.AttackHitbox.IsEmpty)
            {
                int facing = (player.CurrentDirection == Player.Direction.Right) ? 1 : -1;
                for (int b = breakableBlocks.Count - 1; b >= 0; b--) { var block = breakableBlocks[b]; if (!block.IsBroken && player.AttackHitbox.IntersectsWith(block.Hitbox)) { block.TakeDamage(10); player.VerticalVelocity = -15; if (player.CurrentDirection == Player.Direction.Left) player.X += 40; else player.X -= 40; break; } }
                foreach (var enemy in enemies) { if (!enemy.IsDead && player.AttackHitbox.IntersectsWith(enemy.Hitbox)) { enemy.TakeDamage(10, facing); player.VerticalVelocity = -15; if (player.CurrentDirection == Player.Direction.Left) player.X += 40; else player.X -= 40; break; } }
            }

            if (miniBoss != null)
            {
                miniBoss.Update(player);
                if (!miniBoss.IsDead)
                {
                    if (player.IsAttacking && !player.AttackHitbox.IsEmpty && player.AttackHitbox.IntersectsWith(miniBoss.Hitbox)) { miniBoss.TakeDamage(10); player.VerticalVelocity = -15; if (player.CurrentDirection == Player.Direction.Left) player.X += 40; else player.X -= 40; }
                    if (miniBoss.IsSwiping && !miniBoss.SwipeHitbox.IsEmpty && miniBoss.SwipeHitbox.IntersectsWith(player.Hitbox) && !isGodMode) { if (player.TakeDamage()) isGameOver = true; }
                    foreach (var proj in miniBoss.Projectiles) { if (proj.Hitbox.IntersectsWith(player.Hitbox) && !isGodMode) { if (player.TakeDamage()) isGameOver = true; break; } }
                }
                else if (miniBoss.SequenceFinished && platforms.Count > 0)
                {
                    for (int p = platforms.Count - 1; p >= 0; p--) { if (platforms[p].X == 1750 && platforms[p].Width == 170) { platforms.RemoveAt(p); break; } }
                    Random prizeRand = new Random(); for (int i = 0; i < 5; i++) roomGolds.Add(new Gold(miniBoss.Hitbox.X + prizeRand.Next(-100, 100), miniBoss.Hitbox.Y + prizeRand.Next(50, 200), (i % 2 == 0) ? 50 : 10, (i % 2 == 0) ? Color.Cyan : Color.Gold)); miniBoss = null;
                }
            }

            List<Rectangle> newLadders;
            bool roomChanged = roomManager.Update(player, platforms, enemies, roomGolds, breakableBlocks, timedBlocks, movingPlatforms, out newLadders);
            if (roomChanged) { roomLadders = newLadders; HandleRoomTransitionAnimation(); }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i]; enemy.Update(platforms, player);
                if (enemy.IsDead && enemy.HurtTimer >= 85) { enemies.RemoveAt(i); continue; }
                if (enemy.IsEnemyAttacking && !enemy.EnemyAttackHitbox.IsEmpty && enemy.EnemyAttackHitbox.IntersectsWith(player.Hitbox)) { if (isGodMode) break; if (player.TakeDamage()) isGameOver = true; else if (player.IsInvincible && player.InvincibilityTimer == 0) { player.VerticalVelocity = -9; if (player.CurrentDirection == Player.Direction.Left) player.X += 40; else player.X -= 40; } break; }
            }

            for (int i = roomGolds.Count - 1; i >= 0; i--) { if (player.Hitbox.IntersectsWith(roomGolds[i].Hitbox)) { totalGold += roomGolds[i].Value; roomGolds.RemoveAt(i); } }
            this.Invalidate();
        }

        void HandleRoomTransitionAnimation()
        {
            Bitmap oldRoomImg = new Bitmap(virtualCanvas); SyncLoadRoom();
            PaintEventArgs fakePaint = new PaintEventArgs(this.CreateGraphics(), this.ClientRectangle); Form1_Paint(this, fakePaint);
            Bitmap newRoomImg = new Bitmap(virtualCanvas);
            int scrollSpeed = 60; bool scrollLeft = (player.X < targetWidth / 2);

            for (int offset = 0; offset <= targetWidth; offset += scrollSpeed)
            {
                using (Graphics g = Graphics.FromImage(virtualCanvas))
                {
                    g.Clear(Color.FromArgb(20, 24, 43));
                    if (scrollLeft) { g.DrawImage(oldRoomImg, -offset, 0); g.DrawImage(newRoomImg, targetWidth - offset, 0); }
                    else { g.DrawImage(oldRoomImg, offset, 0); g.DrawImage(newRoomImg, -targetWidth + offset, 0); }
                }
                using (Graphics formGraphics = this.CreateGraphics()) { formGraphics.InterpolationMode = InterpolationMode.NearestNeighbor; formGraphics.DrawImage(virtualCanvas, 0, 0, this.ClientSize.Width, this.ClientSize.Height); }
                System.Threading.Thread.Sleep(1);
            }
            oldRoomImg.Dispose(); newRoomImg.Dispose(); this.Focus();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (canvasGraphics == null || virtualCanvas == null) return;
            canvasGraphics.SmoothingMode = SmoothingMode.AntiAlias; canvasGraphics.InterpolationMode = InterpolationMode.NearestNeighbor; canvasGraphics.PixelOffsetMode = PixelOffsetMode.Half;

            if (currentGameState == GameState.MainMenu) { MainMenuUI.Draw(canvasGraphics, menuSelection, startButton, exitButton); }
            else
            {
                Color bgTone = (roomManager.CurrentRoom == 11) ? Color.FromArgb(24, 85, 40) : ((roomManager.CurrentRoom == 9 || roomManager.CurrentRoom == 10) ? Color.FromArgb(12, 16, 26) : Color.FromArgb(20, 24, 43));
                canvasGraphics.Clear(bgTone);

                if (roomManager.CurrentRoom == 9 || roomManager.CurrentRoom == 10)
                {
                    using (SolidBrush waterBrush = new SolidBrush(Color.FromArgb(45, 40, 120, 220)))
                    using (Pen splashPen = new Pen(Color.FromArgb(90, 255, 255, 255), 2f))
                    {
                        if (roomManager.CurrentRoom == 9) { canvasGraphics.FillRectangle(waterBrush, 450, 100, 1050, 980); } // Ortadaki koca şelale boşluğu usta
                        else { canvasGraphics.FillRectangle(waterBrush, 310, 100, 150, 900); canvasGraphics.FillRectangle(waterBrush, 1150, 100, 400, 900); }
                        int startW = (roomManager.CurrentRoom == 9) ? 600 : 330;
                        for (int wy = 140; wy < 900; wy += 90) { canvasGraphics.DrawArc(splashPen, startW, wy, 45, 20, 0, -180); if (roomManager.CurrentRoom == 10) canvasGraphics.DrawArc(splashPen, 1200, wy + 30, 50, 20, 0, -180); }
                    }
                }

                Brush platformBrush = (roomManager.CurrentRoom == 11) ? Brushes.Goldenrod : Brushes.LightSlateGray;
                foreach (var platform in platforms) canvasGraphics.FillRectangle(platformBrush, platform);
                foreach (var gold in roomGolds) gold.Draw(canvasGraphics);
                foreach (var block in breakableBlocks) block.Draw(canvasGraphics);

                //  ZAMANLI BLOKLARI ÇİZDİR usta
                foreach (var tBlock in timedBlocks) tBlock.Draw(canvasGraphics);

                foreach (var mp in movingPlatforms) canvasGraphics.FillRectangle(Brushes.ForestGreen, mp.Hitbox);

                if (roomManager.CurrentRoom == 7) // Sadece Oda 7'de diken kalsın usta
                {
                    using (Pen spikePen = new Pen(Color.FromArgb(190, 200, 210), 3f))
                    {
                        int spikeY = 1020; int spikeWidth = 35; int spikeHeight = 45;
                        for (int sx = 0; sx < targetWidth; sx += spikeWidth) { Point[] spikePoints = { new Point(sx, spikeY), new Point(sx + (spikeWidth / 2), spikeY - spikeHeight), new Point(sx + spikeWidth, spikeY) }; canvasGraphics.FillPolygon(Brushes.SlateGray, spikePoints); canvasGraphics.DrawPolygon(spikePen, spikePoints); }
                    }
                }

                foreach (var ladder in roomLadders) { using (Pen ladderPen = new Pen(Color.FromArgb(230, 180, 40), 5f)) { canvasGraphics.DrawLine(ladderPen, ladder.X, ladder.Y, ladder.X, ladder.Bottom); canvasGraphics.DrawLine(ladderPen, ladder.Right, ladder.Y, ladder.Right, ladder.Bottom); for (int ly = ladder.Y; ly <= ladder.Bottom; ly += 25) canvasGraphics.DrawLine(ladderPen, ladder.X, ly, ladder.Right, ly); } }
                if (checkpointTorch != null) checkpointTorch.Draw(canvasGraphics);

                foreach (var enemy in enemies) enemy.Draw(canvasGraphics);
                if (miniBoss != null) miniBoss.Draw(canvasGraphics);

                if (playerImage != null)
                {
                    Rectangle drawRect = new Rectangle(player.Hitbox.X, player.Hitbox.Y, player.Hitbox.Width, player.Hitbox.Height);
                    if (player.CurrentDirection == Player.Direction.Left) { GraphicsState state = canvasGraphics.Save(); canvasGraphics.TranslateTransform(drawRect.X + drawRect.Width, drawRect.Y); canvasGraphics.ScaleTransform(-1, 1); canvasGraphics.DrawImage(playerImage, 0, 0, drawRect.Width, drawRect.Height); canvasGraphics.Restore(state); }
                    else canvasGraphics.DrawImage(playerImage, drawRect);
                }
                else canvasGraphics.FillRectangle(Brushes.Black, player.Hitbox);

                if (player.IsAttacking && !player.AttackHitbox.IsEmpty)
                {
                    float progress = (float)player.AttackTimer / player.AttackDuration; int alpha = (int)(255 * (1.0f - progress)); if (alpha < 0) alpha = 0;
                    int arcSize = (int)(110 + (progress * 20)); Rectangle arcBounds; float startAngle, sweepAngle; int arcY = player.Hitbox.Y + 10; int facing = (player.CurrentDirection == Player.Direction.Right) ? 1 : -1;
                    if (facing == 1) { int arcX = player.Hitbox.Right - 35; arcBounds = new Rectangle(arcX, arcY, arcSize, arcSize); startAngle = -90 + (progress * 25); sweepAngle = 180; } else { int arcX = player.Hitbox.X - arcSize + 35; arcBounds = new Rectangle(arcX, arcY, arcSize, arcSize); startAngle = 270 - (progress * 25); sweepAngle = -180; }
                    using (Pen neonPen = new Pen(Color.FromArgb((int)(alpha * 0.8f), 0, 235, 255), 3f)) { neonPen.StartCap = LineCap.Round; neonPen.EndCap = LineCap.Round; canvasGraphics.DrawArc(neonPen, arcBounds, startAngle, sweepAngle); }
                    using (Pen corePen = new Pen(Color.FromArgb(alpha, 255, 255, 255), 1.5f)) { corePen.StartCap = LineCap.Round; corePen.EndCap = LineCap.Round; canvasGraphics.DrawArc(corePen, arcBounds, startAngle, sweepAngle); }
                }

                HUDRenderer.Draw(canvasGraphics, roomManager.CurrentRoom, player.MaxHealth, player.CurrentHealth, totalGold, kalpDolu, kalpBos);
                if (player.IsInvincible && (player.InvincibilityTimer % 4 == 0)) { using (SolidBrush damageFilter = new SolidBrush(Color.FromArgb(100, Color.Red))) canvasGraphics.FillRectangle(damageFilter, player.Hitbox); }
                if (currentGameState == GameState.Paused) PauseMenuUI.Draw(canvasGraphics, pauseSelection, resumeButton, settingsButton, mainMenuButton);
                if (isGameOver) GameOverUI.Draw(canvasGraphics);
            }

            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor; e.Graphics.DrawImage(virtualCanvas, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
        }

        void ResetGame() { isGameOver = false; totalGold = 0; GameMap.ResetWorld(); roomManager.Reset(); player.Reset(300, 750); SyncLoadRoom(); }
        void SetScreenMode(bool fullscreen) { if (fullscreen) { this.FormBorderStyle = FormBorderStyle.None; this.WindowState = FormWindowState.Maximized; } else { this.FormBorderStyle = FormBorderStyle.FixedSingle; this.WindowState = FormWindowState.Normal; this.ClientSize = new Size(1280, 720); this.StartPosition = FormStartPosition.CenterScreen; } this.Refresh(); }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { bool handled = inputManager.HandleProcessCmdKey(keyData, ref pauseSelection, ref currentGameState, () => { currentGameState = GameState.Playing; this.Focus(); }, () => { currentGameState = GameState.MainMenu; menuSelection = 0; this.Focus(); }); if (handled) return true; return base.ProcessCmdKey(ref msg, keyData); }

        public Form1()
        {
            InitializeComponent(); GameMap.InitializeWorld(); virtualCanvas = new Bitmap(targetWidth, targetHeight); canvasGraphics = Graphics.FromImage(virtualCanvas); SetScreenMode(true);
            player = new Player(300, 750, 130, 130); physicsEngine = new PhysicsEngine(); attackSystem = new AttackSystem(); roomManager = new RoomManager(targetWidth, targetHeight); inputManager = new InputManager();
            this.DoubleBuffered = true; this.MaximizeBox = false; this.KeyPreview = true; this.Focus(); SyncLoadRoom();
        }
    }
}