using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeserKnight
{
    public partial class Form1 : Form
    {
        // --- SANAL ARABELLEK (VIRTUAL CANVAS) DEĞİŞKENLERİ ---
        private Bitmap virtualCanvas; // Tüm oyunun çizileceği 1920x1080 boyutundaki hayali tuval
        private Graphics canvasGraphics; // Hayali tuvalin çizim motoru
        private int targetWidth = 1920;
        private int targetHeight = 1080;

        // --- OYUN NESNELERİ VE LİSTELER ---
        List<Rectangle> platforms = new List<Rectangle>();
        Rectangle player = new Rectangle(300, 750, 100, 100);
        int currentRoom = 1;
        bool isGameOver = false;
        List<Enemy> enemies = new List<Enemy>();
        int totalGold = 0;
        List<Gold> roomGolds = new List<Gold>();
        DateTime lastF11Time = DateTime.MinValue;
        int menuSelection = 0; // Ana menü seçimi (0: Başla, 1: Çıkış)

        // --- YENİ: ESC MENÜSÜ SEÇİM DEĞİŞKENİ ---
        int pauseSelection = 0; // 0: Devam Et, 1: Ayarlar, 2: Ana Menü

        // --- OYUN DURUMLARI (STATE MACHINE) ---
        public enum GameState { MainMenu, Playing, Paused }
        GameState currentGameState = GameState.MainMenu;

        // --- MENÜ BUTON SINIRLARI (1920x1080 Dünyasında Sabit) ---
        Rectangle startButton = new Rectangle(810, 500, 300, 60);
        Rectangle exitButton = new Rectangle(810, 600, 300, 60);

        // --- YENİ: ESC DURAKLATMA MENÜSÜ BUTON SINIRLARI ---
        Rectangle resumeButton = new Rectangle(810, 450, 300, 60);
        Rectangle settingsButton = new Rectangle(810, 540, 300, 60);
        Rectangle mainMenuButton = new Rectangle(810, 630, 300, 60);

        // --- CAN (HP) SİSTEMİ ---
        int maxHealth = 3;
        int currentHealth = 3;
        bool isInvincible = false;
        int invincibilityTimer = 0;
        int invincibilityDuration = 40;

        // --- HAREKET VE SALDIRI DEĞİŞKENLERİ ---
        bool moveLeft = false;
        bool moveRight = false;
        bool isAttacking = false;
        int attackTimer = 0;
        int attackDuration = 10;
        Rectangle attackHitbox;

        public enum Direction { Left, Right }
        Direction currentDirection = Direction.Right;

        // FİZİK MOTORU AYARLARI
        int playerSpeed = 14;
        int verticalVelocity = 0;
        int gravity = 3;
        int jumpPower = -38;
        bool isJumping = false;

        // GÖRSEL ASSET TANIMLAMALARI
        Image playerImage = Properties.Resources.shovel_knight;
        Image kalpDolu = Properties.Resources.kalp_dolu;
        Image kalpBos = Properties.Resources.kalp_bos;

        void LoadRoom()
        {
            platforms.Clear();
            enemies.Clear();
            roomGolds.Clear();

            if (currentRoom == 1)
            {
                platforms.Add(new Rectangle(0, 850, 550, 230));
                platforms.Add(new Rectangle(650, 750, 200, 40));
                platforms.Add(new Rectangle(950, 650, 200, 40));
                platforms.Add(new Rectangle(1400, 550, 520, 530));

                enemies.Add(new Enemy(1500, 490, 60, 60, 80));
                roomGolds.Add(new Gold(750, 700, 10, Color.Gold));
                roomGolds.Add(new Gold(1050, 600, 10, Color.Gold));
                roomGolds.Add(new Gold(1450, 500, 50, Color.Cyan));
            }
            else if (currentRoom == 2)
            {
                platforms.Add(new Rectangle(0, 550, 400, 530));
                platforms.Add(new Rectangle(550, 750, 800, 50));
                platforms.Add(new Rectangle(1500, 650, 420, 630));

                enemies.Add(new Enemy(700, 690, 60, 60, 120));
                enemies.Add(new Enemy(1100, 690, 60, 60, 100));
                roomGolds.Add(new Gold(750, 700, 10, Color.Gold));
                roomGolds.Add(new Gold(950, 700, 50, Color.Cyan));
                roomGolds.Add(new Gold(1150, 700, 10, Color.Gold));
            }
            else if (currentRoom == 3)
            {
                platforms.Add(new Rectangle(0, 450, 400, 630));
                platforms.Add(new Rectangle(400, 1000, 1520, 80));
                platforms.Add(new Rectangle(500, 800, 400, 40));
                platforms.Add(new Rectangle(1200, 800, 500, 40));
                platforms.Add(new Rectangle(600, 550, 700, 40));
                platforms.Add(new Rectangle(1400, 300, 520, 780));

                enemies.Add(new Enemy(600, 740, 60, 60, 80));
                enemies.Add(new Enemy(900, 490, 60, 60, 150));
                roomGolds.Add(new Gold(1450, 750, 10, Color.Gold));
                roomGolds.Add(new Gold(950, 490, 50, Color.Cyan));
                roomGolds.Add(new Gold(1600, 240, 50, Color.Cyan));
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // F11 Ekran Büyütme/Küçültme Kilidi
            if (e.KeyCode == Keys.F11)
            {
                if ((DateTime.Now - lastF11Time).TotalMilliseconds < 800) return;
                lastF11Time = DateTime.Now;

                if (this.FormBorderStyle == FormBorderStyle.None) SetScreenMode(false);
                else SetScreenMode(true);
                return;
            }

            // ESC TUŞU: Oyunu duraklatır veya duraklatmayı iptal eder
            if (e.KeyCode == Keys.Escape)
            {
                if (currentGameState == GameState.Playing)
                {
                    currentGameState = GameState.Paused;
                    pauseSelection = 0; // ESC basınca ilk buton (Devam Et) seçili gelsin
                    this.Invalidate();
                    return;
                }
                else if (currentGameState == GameState.Paused)
                {
                    currentGameState = GameState.Playing;
                    return;
                }
            }

            // --- ANA MENÜDEYKEN KLAVYE SEÇİMLERİ ---
            if (currentGameState == GameState.MainMenu)
            {
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) { menuSelection = 1; this.Invalidate(); }
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) { menuSelection = 0; this.Invalidate(); }

                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    if (menuSelection == 0)
                    {
                        ResetGame();
                        currentRoom = 1;
                        LoadRoom();
                        currentGameState = GameState.Playing;

                        // --- BU SATIRI EKLE: Oyuna girerken klavye odağını forma kilitler ---
                        this.Focus();
                    }
                    else if (menuSelection == 1) Application.Exit();
                }
                return;
            }

            // --- YENİ KISIM: ESC (PAUSE) MENÜSÜNDEYKEN KLAVYE SEÇİMLERİ ---
            if (currentGameState == GameState.Paused)
            {
                // S veya Aşağı Yön Tuşu: Seçimi aşağı kaydırır (Maksimum 2 olabilir)
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
                {
                    pauseSelection++;
                    if (pauseSelection > 2) pauseSelection = 0; // Sınırı aşarsa en başa sar usta
                    this.Invalidate();
                }
                // W veya Yukarı Yön Tuşu: Seçimi yukarı kaydırır
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
                {
                    pauseSelection--;
                    if (pauseSelection < 0) pauseSelection = 2; // Sıfırın altına düşerse en alta sar
                    this.Invalidate();
                }

                // ENTER veya SPACE basınca seçilen ESC menü butonunu ateşle!
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    if (pauseSelection == 0) // DEVAM ET
                    {
                        currentGameState = GameState.Playing;
                    }
                    else if (pauseSelection == 1) // AYARLAR
                    {
                        MessageBox.Show("Ayarlar Menüsü Yakında Eklenecek Usta!", "KeserKnight Ayarlar");
                    }
                    else if (pauseSelection == 2) // ANA MENÜYE DÖN
                    {
                        currentGameState = GameState.MainMenu;
                        menuSelection = 0; // Ana menüde ilk butona sabitle
                    }
                }
                return; // Menü aktifken oyuncu hareket kodlarına geçmesini engeller usta
            }

            if (currentGameState != GameState.Playing) return;

            if (isGameOver && e.KeyCode == Keys.R) { ResetGame(); return; }
            if (isGameOver) return;

            if (e.KeyCode == Keys.A) moveLeft = true;
            if (e.KeyCode == Keys.D) moveRight = true;

            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.W)
            {
                if (!isJumping) { verticalVelocity = jumpPower; isJumping = true; }
            }

            if (e.KeyCode == Keys.L && !isAttacking) { isAttacking = true; attackTimer = 0; }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A) moveLeft = false;
            if (e.KeyCode == Keys.D) moveRight = false;
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            // Sanal arabellek oran matematiği (Mükemmel eşitleme)
            float scaleX = (float)this.ClientSize.Width / targetWidth;
            float scaleY = (float)this.ClientSize.Height / targetHeight;
            int virtualX = (int)(e.X / scaleX);
            int virtualY = (int)(e.Y / scaleY);
            Point virtualClickPoint = new Point(virtualX, virtualY);

            // Ana menü fare tıklama algılayıcısı
            if (currentGameState == GameState.MainMenu)
            {
                if (startButton.Contains(virtualClickPoint))
                {
                    ResetGame();
                    currentRoom = 1;
                    LoadRoom();
                    currentGameState = GameState.Playing;

                    this.Focus();
                }
                else if (exitButton.Contains(virtualClickPoint)) Application.Exit();
            }
            // --- YENİ KISIM: ESC MENÜSÜ FARE TIKLAMA ALGILAYICISI ---
            else if (currentGameState == GameState.Paused)
            {
                if (resumeButton.Contains(virtualClickPoint))
                {
                    currentGameState = GameState.Playing;
                }
                else if (settingsButton.Contains(virtualClickPoint))
                {
                    MessageBox.Show("Ayarlar Menüsü Yakında Eklenecek Usta!", "KeserKnight Ayarlar");
                }
                else if (mainMenuButton.Contains(virtualClickPoint))
                {
                    currentGameState = GameState.MainMenu;
                    menuSelection = 0;
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (currentGameState != GameState.Playing) { this.Invalidate(); return; }
            if (isGameOver) return;

            // --- ADIM 1: YATAY HAREKET HESABI (X EKSENİ) ---
            int nextX = player.X;

            if (moveLeft && !moveRight) { nextX -= playerSpeed; currentDirection = Direction.Left; }
            else if (moveRight && !moveLeft) { nextX += playerSpeed; currentDirection = Direction.Right; }
            else if (moveLeft && moveRight)
            {
                if (currentDirection == Direction.Left) nextX -= playerSpeed;
                else nextX += playerSpeed;
            }

            Rectangle futureX = new Rectangle(nextX, player.Y, player.Width, player.Height);
            bool collidesX = false;
            Rectangle hitPlatformX = Rectangle.Empty;

            foreach (var platform in platforms)
            {
                if (futureX.IntersectsWith(platform)) { collidesX = true; hitPlatformX = platform; break; }
            }

            if (!collidesX) player.X = nextX;
            else
            {
                if (nextX > player.X) player.X = hitPlatformX.Left - player.Width;
                else if (nextX < player.X) player.X = hitPlatformX.Right;
            }

            // --- ADIM 2: DİKEY HAREKET VE YERÇEKİMİ HESABI (Y EKSENİ) ---
            verticalVelocity += gravity;
            int nextY = player.Y + verticalVelocity;

            Rectangle futureY = new Rectangle(player.X, nextY, player.Width, player.Height);
            bool landed = false;

            foreach (var platform in platforms)
            {
                if (futureY.IntersectsWith(platform))
                {
                    if (verticalVelocity > 0)
                    {
                        player.Y = platform.Top - player.Height;
                        verticalVelocity = 0;
                        isJumping = false;
                        landed = true;
                        break;
                    }
                    else if (verticalVelocity < 0)
                    {
                        player.Y = platform.Bottom;
                        verticalVelocity = 0;
                        break;
                    }
                }
            }

            if (!landed && verticalVelocity != 0) { player.Y = nextY; isJumping = true; }
            else if (landed) isJumping = false;

            if (player.Y < 110) { player.Y = 110; verticalVelocity = 0; }
            if (player.Y > 1080) isGameOver = true;

            // --- ADIM 3: ODA GEÇİŞ KONTROLLERİ ---
            if (player.X > 1920)
            {
                currentRoom++;
                player.X = 1;
                LoadRoom();
            }
            else if (player.X + player.Width < 0)
            {
                if (currentRoom > 1) { currentRoom--; player.X = 1920 - player.Width - 1; LoadRoom(); }
                else player.X = 0;
            }

            // --- ADIM 4: KÜREK SALDIRI ALANI ---
            if (isAttacking)
            {
                attackTimer++;
                if (currentDirection == Direction.Left) attackHitbox = new Rectangle(player.X - 80, player.Y + 10, 80, player.Height - 20);
                else attackHitbox = new Rectangle(player.Right, player.Y + 10, 80, player.Height - 20);

                if (attackTimer >= attackDuration) { isAttacking = false; attackHitbox = Rectangle.Empty; }
            }

            if (isInvincible) { invincibilityTimer++; if (invincibilityTimer >= invincibilityDuration) isInvincible = false; }

            // --- ADIM 5: DÜŞMAN ÇARPIŞMALARI ---
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                enemy.Update();

                if (player.IntersectsWith(enemy.Hitbox))
                {
                    if (!isInvincible)
                    {
                        currentHealth--;
                        if (currentHealth <= 0) isGameOver = true;
                        else
                        {
                            isInvincible = true; invincibilityTimer = 0;
                            verticalVelocity = -15;
                            if (currentDirection == Direction.Left) player.X += 40; else player.X -= 40;
                        }
                    }
                    break;
                }

                if (isAttacking && !attackHitbox.IsEmpty && attackHitbox.IntersectsWith(enemy.Hitbox))
                {
                    enemies.RemoveAt(i);
                    isAttacking = false; attackHitbox = Rectangle.Empty;
                    break;
                }
            }

            // --- ADIM 6: ALTIN TOPLAMA ---
            for (int i = roomGolds.Count - 1; i >= 0; i--)
            {
                if (player.IntersectsWith(roomGolds[i].Hitbox))
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

            // =================================================================
            // SAHNE 1: ANA MENÜ ÇİZİMİ
            // =================================================================
            if (currentGameState == GameState.MainMenu)
            {
                canvasGraphics.Clear(Color.FromArgb(11, 11, 26));

                Font titleFont = new Font("Impact", 60, FontStyle.Bold);
                string titleText = "SHOVEL KNIGHT";
                int titleX = (1920 - TextRenderer.MeasureText(titleText, titleFont).Width) / 2;
                canvasGraphics.DrawString(titleText, titleFont, Brushes.Gold, titleX, 200);

                Font subTitleFont = new Font("Arial", 20, FontStyle.Italic);
                string subTitleText = "C# REMAKE";
                int subTitleX = (1920 - TextRenderer.MeasureText(subTitleText, subTitleFont).Width) / 2;
                canvasGraphics.DrawString(subTitleText, subTitleFont, Brushes.White, subTitleX, 300);

                canvasGraphics.FillRectangle(Brushes.DarkBlue, startButton);
                if (menuSelection == 0) canvasGraphics.DrawRectangle(new Pen(Color.White, 5), startButton);
                else canvasGraphics.DrawRectangle(Pens.Cyan, startButton);
                canvasGraphics.DrawString("OYUNA BAŞLA", new Font("Arial", 18, FontStyle.Bold), Brushes.White, startButton.X + 60, startButton.Y + 15);

                canvasGraphics.FillRectangle(Brushes.DarkRed, exitButton);
                if (menuSelection == 1) canvasGraphics.DrawRectangle(new Pen(Color.White, 5), exitButton);
                else canvasGraphics.DrawRectangle(Pens.Red, exitButton);
                canvasGraphics.DrawString("ÇIKIŞ YAP", new Font("Arial", 18, FontStyle.Bold), Brushes.White, exitButton.X + 85, exitButton.Y + 15);
            }
            // =================================================================
            // SAHNE 2: OYUN İÇİ VE HUD PANEL ÇİZİMLERİ
            // =================================================================
            else
            {
                canvasGraphics.Clear(Color.FromArgb(20, 24, 43)); // Uyumlu Zindan Laciverti

                foreach (var platform in platforms) canvasGraphics.FillRectangle(Brushes.LightSlateGray, platform);
                foreach (var enemy in enemies) enemy.Draw(canvasGraphics);
                foreach (var gold in roomGolds) gold.Draw(canvasGraphics);

                if (playerImage != null) canvasGraphics.DrawImage(playerImage, player);
                else canvasGraphics.FillRectangle(Brushes.Black, player);

                if (isAttacking && !attackHitbox.IsEmpty)
                {
                    using (SolidBrush attackBrush = new SolidBrush(Color.FromArgb(150, Color.Yellow))) canvasGraphics.FillRectangle(attackBrush, attackHitbox);
                    canvasGraphics.DrawRectangle(Pens.Red, attackHitbox);
                }

                // SİYAH HUD BAR
                canvasGraphics.FillRectangle(Brushes.Black, 0, 0, 1920, 110);
                canvasGraphics.DrawLine(new Pen(Color.FromArgb(50, 50, 60), 4), 0, 110, 1920, 110);

                int startX = 50; int startY = 15; int boxSize = 80; int gap = 12;
                for (int i = 0; i < maxHealth; i++)
                {
                    int currentHeartX = startX + (i * (boxSize + gap));
                    if (i < currentHealth)
                    {
                        if (kalpDolu != null) canvasGraphics.DrawImage(kalpDolu, currentHeartX, startY, boxSize, boxSize);
                        else canvasGraphics.FillRectangle(Brushes.Crimson, currentHeartX, startY, boxSize, boxSize);
                    }
                    else
                    {
                        if (kalpBos != null) canvasGraphics.DrawImage(kalpBos, currentHeartX, startY, boxSize, boxSize);
                        else canvasGraphics.FillRectangle(Brushes.DimGray, currentHeartX, startY, boxSize, boxSize);
                    }
                }

                int goldX = 850;
                canvasGraphics.FillEllipse(Brushes.Gold, goldX, startY + 22, 40, 40);
                canvasGraphics.DrawEllipse(new Pen(Color.White, 2), goldX, startY + 22, 40, 40);
                canvasGraphics.DrawString("GOLD: " + totalGold, new Font("Impact", 28, FontStyle.Regular), Brushes.Gold, goldX + 60, startY + 17);

                string roomText = "STAGE: 0" + currentRoom;
                canvasGraphics.DrawString(roomText, new Font("Impact", 28, FontStyle.Regular), Brushes.White, 1650, startY + 17);

                if (isInvincible && (invincibilityTimer % 4 == 0))
                {
                    using (SolidBrush damageFilter = new SolidBrush(Color.FromArgb(100, Color.Red))) canvasGraphics.FillRectangle(damageFilter, player);
                }

                // =================================================================
                // YENİ ŞIK SAHNE: PRO RETRO ESC (PAUSE) MENÜSÜ ÇİZİMİ
                // =================================================================
                if (currentGameState == GameState.Paused)
                {
                    // Arka planı hafif karartmak için transparan perde
                    using (SolidBrush pauseOverlay = new SolidBrush(Color.FromArgb(180, Color.Black)))
                    {
                        canvasGraphics.FillRectangle(pauseOverlay, 0, 0, 1920, 1080);
                    }

                    // Büyük "OYUN DURAKLATILDI" Başlığı
                    Font pauseTitleFont = new Font("Impact", 55, FontStyle.Bold);
                    string pTitleText = "OYUN DURAKLATILDI";
                    int pTitleX = (1920 - TextRenderer.MeasureText(pTitleText, pauseTitleFont).Width) / 2;
                    canvasGraphics.DrawString(pTitleText, pauseTitleFont, Brushes.Gold, pTitleX, 280);

                    // --- 1. BUTON: DEVAM ET ---
                    canvasGraphics.FillRectangle(Brushes.DarkBlue, resumeButton);
                    if (pauseSelection == 0) canvasGraphics.DrawRectangle(new Pen(Color.White, 5), resumeButton);
                    else canvasGraphics.DrawRectangle(Pens.Cyan, resumeButton);
                    canvasGraphics.DrawString("DEVAM ET", new Font("Arial", 18, FontStyle.Bold), Brushes.White, resumeButton.X + 85, resumeButton.Y + 15);

                    // --- 2. BUTON: AYARLAR ---
                    canvasGraphics.FillRectangle(Brushes.DarkSlateGray, settingsButton);
                    if (pauseSelection == 1) canvasGraphics.DrawRectangle(new Pen(Color.White, 5), settingsButton);
                    else canvasGraphics.DrawRectangle(Pens.LightGray, settingsButton);
                    canvasGraphics.DrawString("AYARLAR", new Font("Arial", 18, FontStyle.Bold), Brushes.White, settingsButton.X + 90, settingsButton.Y + 15);

                    // --- 3. BUTON: ANA MENÜYE DÖN ---
                    canvasGraphics.FillRectangle(Brushes.DarkRed, mainMenuButton);
                    if (pauseSelection == 2) canvasGraphics.DrawRectangle(new Pen(Color.White, 5), mainMenuButton);
                    else canvasGraphics.DrawRectangle(Pens.Red, mainMenuButton);
                    canvasGraphics.DrawString("ANA MENÜ", new Font("Arial", 18, FontStyle.Bold), Brushes.White, mainMenuButton.X + 85, mainMenuButton.Y + 15);
                }

                // GAME OVER EKRANI
                if (isGameOver)
                {
                    using (SolidBrush alphaBrush = new SolidBrush(Color.FromArgb(180, Color.Black))) canvasGraphics.FillRectangle(alphaBrush, 0, 0, 1920, 1080);
                    Font gameOverFont = new Font("Arial", 60, FontStyle.Bold);
                    canvasGraphics.DrawString("GAME OVER", gameOverFont, Brushes.Red, (1920 - TextRenderer.MeasureText("GAME OVER", gameOverFont).Width) / 2, 400);
                    Font subFont = new Font("Arial", 25, FontStyle.Regular);
                    canvasGraphics.DrawString("Yeniden Başlamak İçin 'R' Tuşuna Basın", subFont, Brushes.White, (1920 - TextRenderer.MeasureText("Yeniden Başlamak İçin 'R' Tuşuna Basın", subFont).Width) / 2, 550);
                }
            }

            // Çizilen sanal resmi ekrana tam uyumlu olarak basıyoruz
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.DrawImage(virtualCanvas, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
        }

        void ResetGame()
        {
            currentHealth = maxHealth;
            isInvincible = false;
            currentRoom = 1;
            isGameOver = false;
            totalGold = 0;
            player.X = 300;
            player.Y = 750;
            verticalVelocity = 0;
            isJumping = false;
            LoadRoom();
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

        public Form1()
        {
            InitializeComponent();

            virtualCanvas = new Bitmap(targetWidth, targetHeight);
            canvasGraphics = Graphics.FromImage(virtualCanvas);

            SetScreenMode(true);

            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
            this.KeyUp += new KeyEventHandler(Form1_KeyUp);
            this.MouseClick += new MouseEventHandler(Form1_MouseClick);
            this.Paint += new PaintEventHandler(Form1_Paint);

            this.DoubleBuffered = true;
            this.MaximizeBox = false;

            this.KeyPreview = true;
            this.Focus();

            LoadRoom();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Eğer basılan tuş ESC ise ve oyun oynanıyorsa veya duraklatılmışsa
            if (keyData == Keys.Escape)
            {
                if (currentGameState == GameState.Playing)
                {
                    currentGameState = GameState.Paused;
                    pauseSelection = 0; // İlk buton seçili gelsin
                    this.Invalidate(); // Ekranı hemen yenile
                    return true; // Windows'a "Ben bu tuşu işledim, sen karışma" diyoruz
                }
                else if (currentGameState == GameState.Paused)
                {
                    currentGameState = GameState.Playing;
                    this.Invalidate();
                    return true;
                }
            }

            // --- PAUSE MENÜSÜNDEYKEN YÖN TUŞLARININ KİLİTLENMESİNİ ENGELLER ---
            if (currentGameState == GameState.Paused)
            {
                if (keyData == Keys.Down || keyData == Keys.S)
                {
                    pauseSelection++;
                    if (pauseSelection > 2) pauseSelection = 0;
                    this.Invalidate();
                    return true;
                }
                if (keyData == Keys.Up || keyData == Keys.W)
                {
                    pauseSelection--;
                    if (pauseSelection < 0) pauseSelection = 2;
                    this.Invalidate();
                    return true;
                }
                if (keyData == Keys.Enter || keyData == Keys.Space)
                {
                    // Enter basınca tuşun işlevini tetikle usta
                    if (pauseSelection == 0) currentGameState = GameState.Playing;
                    else if (pauseSelection == 1) MessageBox.Show("Ayarlar Menüsü Yakında Eklenecek Usta!", "KeserKnight Ayarlar");
                    else if (pauseSelection == 2) { currentGameState = GameState.MainMenu; menuSelection = 0; }
                    this.Invalidate();
                    return true;
                }
            }

            // Eğer basılan tuş ESC veya duraklatma tuşları değilse, normal klavye sistemine devam et
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}