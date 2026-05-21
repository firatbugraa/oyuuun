using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeserKnight
{
    public partial class Form1 : Form
    {
        // Ana Değişkenler
        List<Rectangle> platforms = new List<Rectangle>(); // Platform Listesi
        Rectangle player = new Rectangle(300, 750, 100, 100);
        int currentRoom = 1;
        int targetWidth = 1920;
        int targetHeight = 1080;
        bool isGameOver = false;
        List<Enemy> enemies = new List<Enemy>(); // Düşman Listesi
        int totalGold = 0; // Oyuncunun topladığı toplam altın miktarı
        List<Gold> roomGolds = new List<Gold>(); // O anki odada bulunan altınların listesi
        DateTime lastF11Time = DateTime.MinValue;
        int menuSelection = 0; // 0: Oyuna Başla, 1: Çıkış Yap

        // --- OYUN DURUMU (STATE) SİSTEMİ ---
        public enum GameState { MainMenu, Playing, Paused }
        GameState currentGameState = GameState.MainMenu; // Oyun ana menüde başlasın

        // Menüdeki butonlar için hayali kutular (Tıklama algılamak için)
        Rectangle startButton = new Rectangle(810, 500, 300, 60);
        Rectangle exitButton = new Rectangle(810, 600, 300, 60);

        // CAN DEĞİŞKENLERİ
        int maxHealth = 3;  // Toplamda kaç can kutumuz olacak (Örn: 4 can kutusu)
        int currentHealth = 3; // Oyuncunun şu an kalan canı
        bool isInvincible = false; // Hasar yedikten sonraki geçici ölümsüzlük süresi (Blink efekti için)
        int invincibilityTimer = 0; // Ölümsüzlük sayacı
        int invincibilityDuration = 40; // Hasar sonrası yarım saniye (40 kare) ölümsüz olsun ki tek saniyede erimesin canı

        // --- SALDIRI VE YÖN KESİN ÇÖZÜM SETİ ---
        bool moveLeft = false;    // Sol tuş basılı mı?
        bool moveRight = false;   // Sağ tuş basılı mı?
        bool isAttacking = false; // Şu an saldırıyor mu?
        int attackTimer = 0;      // Saldırı süresi sayacı
        int attackDuration = 10;  // Saldırı kaç kare sürecek?
        Rectangle attackHitbox;   // Küreğin vuracağı alan

        public enum Direction { Left, Right }
        Direction currentDirection = Direction.Right; // Default sağa baksın

        // Fizik Değişkenleri
        int playerSpeed = 14; // Yatay Hız
        int verticalVelocity = 0; // Dikey hız (Zıplama ve düşme için)
        int gravity = 3;          // Yerçekimi kuvveti
        int jumpPower = -38;      // Zıplama gücü (Eksi çünkü yukarı gidiyoruz)
        bool isJumping = false;   // Karakter havada mı?

        Image playerImage = Properties.Resources.shovel_knight;

        void LoadRoom()//Odaları Yükleme Fonksiyonu
        {
            // Her oda geçişinde ekranı temizliyoruz usta
            platforms.Clear();
            enemies.Clear();
            roomGolds.Clear();

            if (currentRoom == 1)
            {
                // =================================================================
                // ODA 1: BAŞLANGIÇ PARKURU (Çıkış Platformu Yüksekte)
                // =================================================================
                // Ana Güvenli Başlangıç Zemini (Genişlettik)
                platforms.Add(new Rectangle(0, 850, 550, 230));

                // Havada asılı parkur basamakları
                platforms.Add(new Rectangle(650, 750, 200, 40));
                platforms.Add(new Rectangle(950, 650, 200, 40));

                // !!! ODA 1 ÇIKIŞ BALKONU (Y = 550'de bitiyor, 420 piksel genişlik) !!!
                platforms.Add(new Rectangle(1400, 550, 520, 530));

                // DÜŞMANLAR VE ALTINLAR
                enemies.Add(new Enemy(1500, 490, 60, 60, 80));
                roomGolds.Add(new Gold(750, 700, 10, Color.Gold));
                roomGolds.Add(new Gold(1050, 600, 10, Color.Gold));
                roomGolds.Add(new Gold(1450, 500, 50, Color.Cyan));
            }
            else if (currentRoom == 2)
            {
                // =================================================================
                // ODA 2: KÖPRÜ PARKURU (Giriş ve Çıkış Yükseklikleri Eşitlendi)
                // =================================================================
                // !!! ODA 2 GİRİŞ BALKONU: Oda 1'in çıkışıyla milimetrik aynı (Y = 550) !!!
                // Genişliğini 400 yaptık ki oyuncu odaya girdiğinde ayağının altında kesin zemin olsun
                platforms.Add(new Rectangle(0, 550, 400, 530));

                // Ortadaki asılı tehlikeli uzun köprü
                platforms.Add(new Rectangle(550, 750, 800, 50));

                // !!! ODA 2 ÇIKIŞ BALKONU: Bir sonraki odaya güvenli geçiş için Y = 450'ye çektik !!!
                platforms.Add(new Rectangle(1500, 650, 420, 630));

                // DÜŞMANLAR VE ALTINLAR
                enemies.Add(new Enemy(700, 690, 60, 60, 120));
                enemies.Add(new Enemy(1100, 690, 60, 60, 100));
                roomGolds.Add(new Gold(750, 700, 10, Color.Gold));
                roomGolds.Add(new Gold(950, 700, 50, Color.Cyan));
                roomGolds.Add(new Gold(1150, 700, 10, Color.Gold));
            }
            else if (currentRoom == 3)
            {
                // =================================================================
                // ODA 3: TIRMANIŞ PARKURU (Giriş Yüksekliği Oda 2 Çıkışına Eşit)
                // =================================================================
                // !!! ODA 3 GİRİŞ BALKONU: Oda 2'nin çıkışıyla tam uyuşuyor (Y = 450) !!!
                platforms.Add(new Rectangle(0, 450, 400, 630));

                // Alt taban zemin (Düşenler için kurtarma alanı)
                platforms.Add(new Rectangle(400, 1000, 1520, 80));

                // Labirentimsi dikey katlar ve tırmanış basamakları
                platforms.Add(new Rectangle(500, 800, 400, 40));
                platforms.Add(new Rectangle(1200, 800, 500, 40));
                platforms.Add(new Rectangle(600, 550, 700, 40));
                platforms.Add(new Rectangle(1400, 300, 520, 780)); // Final Çıkış Noktası

                // DÜŞMANLAR VE ALTINLAR
                enemies.Add(new Enemy(600, 740, 60, 60, 80));
                enemies.Add(new Enemy(900, 490, 60, 60, 150));
                roomGolds.Add(new Gold(1450, 750, 10, Color.Gold));
                roomGolds.Add(new Gold(950, 490, 50, Color.Cyan));
                roomGolds.Add(new Gold(1600, 240, 50, Color.Cyan));
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // F11 ve ESC kontrolleri her durumda (Menü dahil) çalışsın
            if (e.KeyCode == Keys.F11)
            {
                if ((DateTime.Now - lastF11Time).TotalMilliseconds < 800) return;
                lastF11Time = DateTime.Now;

                if (this.FormBorderStyle == FormBorderStyle.None) SetScreenMode(false);
                else SetScreenMode(true);
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                if (currentGameState == GameState.Playing)
                {
                    currentGameState = GameState.Paused;
                    this.Invalidate();
                    return;
                }
                else if (currentGameState == GameState.Paused)
                {
                    currentGameState = GameState.Playing;
                    return;
                }
            }

            // =================================================================
            // KESİN ÇÖZÜM: ANA MENÜDEYKEN KLAVYE KONTROLLERİ
            // =================================================================
            if (currentGameState == GameState.MainMenu)
            {
                // Aşağı oka veya S tuşuna basınca seçimi Çıkış Yap'a (1) çek
                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
                {
                    menuSelection = 1;
                    this.Invalidate(); // Ekranı tazele ki seçilen buton parlasın
                }
                // Yukarı oka veya W tuşuna basınca seçimi Oyuna Başla'ya (0) çek
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
                {
                    menuSelection = 0;
                    this.Invalidate();
                }

                // Enter veya Space tuşuna basınca seçili olan butonu ateşle!
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    if (menuSelection == 0)
                    {
                        // OYUNA BAŞLA
                        ResetGame();
                        currentRoom = 1;
                        LoadRoom();
                        currentGameState = GameState.Playing;
                    }
                    else if (menuSelection == 1)
                    {
                        // ÇIKIŞ YAP
                        Application.Exit();
                    }
                }
                return; // Menüdeysek hareket kodlarına geçme
            }

            // Oyun oynanırken çalışan normal tuşlar (Aynen kalıyor usta)
            if (currentGameState != GameState.Playing) return;

            if (isGameOver && e.KeyCode == Keys.R)
            {
                ResetGame();
                return;
            }

            if (isGameOver) return;

            if (e.KeyCode == Keys.A) moveLeft = true;
            if (e.KeyCode == Keys.D) moveRight = true;

            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.W)
            {
                if (!isJumping)
                {
                    verticalVelocity = jumpPower;
                    isJumping = true;
                }
            }

            if (e.KeyCode == Keys.L && !isAttacking)
            {
                isAttacking = true;
                attackTimer = 0;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A) moveLeft = false;
            if (e.KeyCode == Keys.D) moveRight = false;

            // F11 DÖNGÜ KIRICI KESİN ÇÖZÜM: Tetiklemeyi saniyede birle sınırlıyoruz
            if (e.KeyCode == Keys.F11)
            {
                if ((DateTime.Now - lastF11Time).TotalMilliseconds < 800) return;
                lastF11Time = DateTime.Now;

                if (this.FormBorderStyle == FormBorderStyle.None)
                {
                    SetScreenMode(false); // Küçült
                }
                else
                {
                    SetScreenMode(true); // Büyüt
                }
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (currentGameState == GameState.MainMenu)
            {
                // Çizim yaparken kullandığımız net iç alan oranlarını buraya da alıyoruz
                float scaleX = (float)this.ClientSize.Width / targetWidth;
                float scaleY = (float)this.ClientSize.Height / targetHeight;

                // Farenin tıkladığı gerçek pikseli, sanal 1920 dünyasındaki yerine tercüme ediyoruz
                int virtualX = (int)(e.X / scaleX);
                int virtualY = (int)(e.Y / scaleY);
                Point virtualClickPoint = new Point(virtualX, virtualY);

                // Artık butonlar ekranda nereye ölçeklenirse ölsün, fare tam üstündeyken tıklamayı algılayacak
                if (startButton.Contains(virtualClickPoint))
                {
                    ResetGame();
                    currentRoom = 1;
                    LoadRoom();
                    currentGameState = GameState.Playing;
                }
                else if (exitButton.Contains(virtualClickPoint))
                {
                    Application.Exit();
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // Eğer ana menüdeysek veya duraklatıldıysa sadece ekranı tazele
            if (currentGameState != GameState.Playing)
            {
                this.Invalidate();
                return;
            }

            if (isGameOver) return;

            // --- 1. YATAY HAREKET VE AKILLI YÖN TAKİBİ ---
            int nextX = player.X;

            if (moveLeft && !moveRight)
            {
                nextX -= playerSpeed;
                currentDirection = Direction.Left;
            }
            else if (moveRight && !moveLeft)
            {
                nextX += playerSpeed;
                currentDirection = Direction.Right;
            }
            else if (moveLeft && moveRight)
            {
                if (currentDirection == Direction.Left)
                {
                    nextX -= playerSpeed;
                }
                else
                {
                    nextX += playerSpeed;
                }
            }

            // Yatayda hareket edeceğimiz hayali kutu
            Rectangle futurePlayerX = new Rectangle(nextX + 2, player.Y, player.Width - 4, player.Height);
            bool canMoveX = true;

            foreach (var platform in platforms)
            {
                if (futurePlayerX.IntersectsWith(platform))
                {
                    if (player.Bottom > platform.Top + 5)
                    {
                        canMoveX = false;

                        if (moveRight)
                            player.X = platform.Left - player.Width;
                        else if (moveLeft)
                            player.X = platform.Right;

                        break;
                    }
                }
            }

            if (canMoveX)
            {
                player.X = nextX;
            }

            // --- 2. DİKEY HAREKET VE FİZİK MOTORU ---
            verticalVelocity += gravity;
            int nextY = player.Y + verticalVelocity;

            bool landed = false;
            Rectangle futurePlayerY = new Rectangle(player.X, nextY, player.Width, player.Height);

            foreach (var platform in platforms)
            {
                if (futurePlayerY.IntersectsWith(platform))
                {
                    if (verticalVelocity > 0 && player.Bottom <= platform.Top + 15)
                    {
                        player.Y = platform.Top - player.Height;
                        verticalVelocity = 0;
                        isJumping = false;
                        landed = true;
                        break;
                    }
                    else if (verticalVelocity < 0 && player.Top >= platform.Bottom - 15)
                    {
                        player.Y = platform.Bottom;
                        verticalVelocity = 0;
                        break;
                    }
                }
            }

            if (!landed)
            {
                player.Y = nextY;
                isJumping = true;
            }
            else
            {
                isJumping = false;
            }

            // --- 3. SONSUZLUĞA DÜŞME KONTROLÜ ---
            if (player.Y > 1080)
            {
                isGameOver = true;
            }

            // --- 4. ODA GEÇİŞLERİ ---
            if (player.X > 1920)
            {
                currentRoom++;
                player.X = 1;
                LoadRoom();
            }
            else if (player.X + player.Width < 0)
            {
                if (currentRoom > 1)
                {
                    currentRoom--;
                    player.X = 1920 - player.Width - 1;
                    LoadRoom();
                }
                else
                {
                    player.X = 0;
                }
            }

            // --- 5. SALDIRI ALANI HESAPLAMA ---
            if (isAttacking)
            {
                attackTimer++;

                if (currentDirection == Direction.Left)
                {
                    attackHitbox = new Rectangle(player.X - 80, player.Y + 10, 80, player.Height - 20);
                }
                else
                {
                    attackHitbox = new Rectangle(player.Right, player.Y + 10, 80, player.Height - 20);
                }

                if (attackTimer >= attackDuration)
                {
                    isAttacking = false;
                    attackHitbox = Rectangle.Empty;
                }
            }

            // --- 6. DÜŞMAN ÇARPIŞMALARI ---
            if (isInvincible)
            {
                invincibilityTimer++;
                if (invincibilityTimer >= invincibilityDuration)
                {
                    isInvincible = false;
                }
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                enemy.Update();

                if (player.IntersectsWith(enemy.Hitbox))
                {
                    if (!isInvincible)
                    {
                        currentHealth--;

                        if (currentHealth <= 0)
                        {
                            isGameOver = true;
                        }
                        else
                        {
                            isInvincible = true;
                            invincibilityTimer = 0;

                            verticalVelocity = -15;
                            if (currentDirection == Direction.Left) player.X += 40;
                            else player.X -= 40;
                        }
                    }
                    break;
                }

                if (isAttacking && !attackHitbox.IsEmpty && attackHitbox.IntersectsWith(enemy.Hitbox))
                {
                    enemies.RemoveAt(i);
                    isAttacking = false;
                    attackHitbox = Rectangle.Empty;
                    break;
                }
            }

            // --- 7. ALTIN TOPLAMA KONTROLÜ ---
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
            Graphics g = e.Graphics;

            // Piksel oyunlarında netlik için hayati ayarlar
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            // Formun dış boyutunu değil, sadece içindeki NET ÇİZİM ALANINI (ClientSize) 
            // hedefimiz olan 1920x1080'e oranlıyoruz. Kaymayı bitiren formül budur.
            float scaleX = (float)this.ClientSize.Width / targetWidth;
            float scaleY = (float)this.ClientSize.Height / targetHeight;
            g.ScaleTransform(scaleX, scaleY);

            // =================================================================
            // DURUM 1: ANA MENÜ EKRANI
            // =================================================================
            if (currentGameState == GameState.MainMenu)
            {
                g.Clear(Color.FromArgb(11, 11, 26));

                Font titleFont = new Font("Impact", 60, FontStyle.Bold);
                string titleText = "SHOVEL KNIGHT";
                Size titleSize = TextRenderer.MeasureText(titleText, titleFont);
                int titleX = (1920 - titleSize.Width) / 2;
                g.DrawString(titleText, titleFont, Brushes.Gold, titleX, 200);

                Font subTitleFont = new Font("Arial", 20, FontStyle.Italic);
                string subTitleText = "C# REMAKE";
                Size subTitleSize = TextRenderer.MeasureText(subTitleText, subTitleFont);
                int subTitleX = (1920 - subTitleSize.Width) / 2;
                g.DrawString(subTitleText, subTitleFont, Brushes.White, subTitleX, 300);

                // --- OYUNA BAŞLA BUTONU ---
                g.FillRectangle(Brushes.DarkBlue, startButton);
                // Eğer menuSelection 0 ise (yani bu buton seçiliyse) kalın beyaz çizgi çek, değilse sönük cyan çek
                if (menuSelection == 0) g.DrawRectangle(new Pen(Color.White, 5), startButton);
                else g.DrawRectangle(Pens.Cyan, startButton);
                g.DrawString("OYUNA BAŞLA", new Font("Arial", 18, FontStyle.Bold), Brushes.White, startButton.X + 60, startButton.Y + 15);

                // --- ÇIKIŞ YAP BUTONU ---
                g.FillRectangle(Brushes.DarkRed, exitButton);
                // Eğer menuSelection 1 ise (yani çıkış seçiliyse) kalın beyaz çizgi çek, değilse sönük kırmızı çek
                if (menuSelection == 1) g.DrawRectangle(new Pen(Color.White, 5), exitButton);
                else g.DrawRectangle(Pens.Red, exitButton);
                g.DrawString("ÇIKIŞ YAP", new Font("Arial", 18, FontStyle.Bold), Brushes.White, exitButton.X + 85, exitButton.Y + 15);

                return;
            }

            // =================================================================
            // DURUM 2 & 3: OYUN İÇİ ÇİZİMLER
            // =================================================================
            g.Clear(currentRoom % 2 == 0 ? Color.CadetBlue : Color.Chocolate);

            foreach (var platform in platforms)
            {
                g.FillRectangle(Brushes.DarkSlateGray, platform);
            }

            foreach (var enemy in enemies)
            {
                enemy.Draw(g);
            }

            foreach (var gold in roomGolds)
            {
                gold.Draw(g);
            }

            if (playerImage != null)
            {
                g.DrawImage(playerImage, player);
            }
            else
            {
                g.FillRectangle(Brushes.Black, player);
            }

            g.DrawString("Oda: " + currentRoom, new Font("Arial", 25, FontStyle.Bold), Brushes.White, 30, 20);

            int startX = 30;
            int startY = 80;
            int boxSize = 30;
            int gap = 15;

            for (int i = 0; i < maxHealth; i++)
            {
                if (i < currentHealth)
                {
                    g.FillRectangle(Brushes.Crimson, startX + (i * (boxSize + gap)), startY, boxSize, boxSize);
                    g.DrawRectangle(Pens.White, startX + (i * (boxSize + gap)), startY, boxSize, boxSize);
                }
                else
                {
                    g.FillRectangle(Brushes.Black, startX + (i * (boxSize + gap)), startY, boxSize, boxSize);
                    g.DrawRectangle(Pens.DimGray, startX + (i * (boxSize + gap)), startY, boxSize, boxSize);
                }
            }

            int goldX = startX + (maxHealth * (boxSize + gap)) + 20;
            g.FillEllipse(Brushes.Gold, goldX, startY + 5, 20, 20);
            g.DrawEllipse(Pens.White, goldX, startY + 5, 20, 20);
            g.DrawString("G: " + totalGold, new Font("Arial", 18, FontStyle.Bold), Brushes.Gold, goldX + 25, startY);

            if (isInvincible && (invincibilityTimer % 4 == 0))
            {
                using (SolidBrush damageFilter = new SolidBrush(Color.FromArgb(100, Color.Red)))
                {
                    g.FillRectangle(damageFilter, player);
                }
            }

            if (isAttacking && !attackHitbox.IsEmpty)
            {
                using (SolidBrush attackBrush = new SolidBrush(Color.FromArgb(150, Color.Yellow)))
                {
                    g.FillRectangle(attackBrush, attackHitbox);
                }
                g.DrawRectangle(Pens.Red, attackHitbox);
            }

            if (currentGameState == GameState.Paused)
            {
                using (SolidBrush pauseOverlay = new SolidBrush(Color.FromArgb(180, Color.Black)))
                {
                    g.FillRectangle(pauseOverlay, 0, 0, 1920, 1080);
                }

                g.DrawString("OYUN DURAKLATILDI", new Font("Impact", 50, FontStyle.Bold), Brushes.White, 620, 400);
                g.DrawString("Devam etmek için tekrar 'ESC' tuşuna basın.", new Font("Arial", 18, FontStyle.Regular), Brushes.LightGray, 690, 520);
            }

            if (isGameOver)
            {
                using (SolidBrush alphaBrush = new SolidBrush(Color.FromArgb(180, Color.Black)))
                {
                    g.FillRectangle(alphaBrush, 0, 0, 1920, 1080);
                }

                Font gameOverFont = new Font("Arial", 60, FontStyle.Bold);
                string text1 = "GAME OVER";
                Size size1 = TextRenderer.MeasureText(text1, gameOverFont);
                g.DrawString(text1, gameOverFont, Brushes.Red, (1920 - size1.Width) / 2, 400);

                Font subFont = new Font("Arial", 25, FontStyle.Regular);
                string text2 = "Yeniden Başlamak İçin 'R' Tuşuna Basın";
                Size size2 = TextRenderer.MeasureText(text2, subFont);
                g.DrawString(text2, subFont, Brushes.White, (1920 - size2.Width) / 2, 550);
            }
        }

        void ResetGame()
        {
            currentHealth = maxHealth;
            isInvincible = false;
            currentRoom = 1;
            isGameOver = false;

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

                // ÖNEMLİ: Windows'un pencere kenarlıklarını hesaba katmadan 
                // net çizim alanını tam 16:9 oranında (1280x720) kilitliyoruz.
                this.ClientSize = new Size(1280, 720);
                this.StartPosition = FormStartPosition.CenterScreen;
            }

            // Ekran modunu değiştirdikten sonra Windows'un eski önbelleğini temizle
            this.Refresh();
        }
        public Form1()
        {
            InitializeComponent();

            SetScreenMode(true); // Tam ekran başlasın usta

            this.KeyDown += new KeyEventHandler(Form1_KeyDown);
            this.KeyUp += new KeyEventHandler(Form1_KeyUp);
            this.MouseClick += new MouseEventHandler(Form1_MouseClick);
            this.Paint += new PaintEventHandler(Form1_Paint);

            this.DoubleBuffered = true;
            this.MaximizeBox = false;

            LoadRoom();
        }
    }
}