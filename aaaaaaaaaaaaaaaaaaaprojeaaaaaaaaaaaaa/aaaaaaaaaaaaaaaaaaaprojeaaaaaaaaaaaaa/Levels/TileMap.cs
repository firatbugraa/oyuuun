using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace oyun1.Levels
{
    // Katana Zero Dinamizmini Sağlayan Reusable Kompakt Oda Tipleri
    public enum RoomType
    {
        SmallCombatArena,    // 2-3 Düşman, Düz hat, tam hızlı akış
        VerticalPogoArena,   // Yarasalar üzerinden zincirleme dikey tırmanış
        DashCorridor,        // Alçak tavan, agresif koşu yolu düşmanları
        PrecisionPlatform,   // Tabanı uçurum, tamamen momentum kilitli adacıklar
        CombatChallenge      // En yoğun Slime + Yarasa refleks odası
    }

    public class TileMap
    {
        public const int TileSize = 32;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public float WidthInPixels => Width * TileSize;
        public float HeightInPixels => Height * TileSize;

        public int AreaID { get; private set; }

        // Optimize Hücre Katman Matrisleri
        private int[,] _collisionLayer;  // 1: Katı Blok, 0: Hava
        private int[,] _backWallLayer;   // 3: Siber-Aksiyon Arka Duvarı, 5: Dekoratif Işıklar
        private int[,] _ruinRiftLayer;   // 2: Destek Kolonları
        private int[,] _foregroundLayer; // 4: Ön Plan Maskeleri / Sinematik Gölgeler

        // Temiz Varlık Doğuş Listeleri
        public PointF PlayerSpawnPoint { get; private set; }
        public List<PointF> SlimeSpawnPoints { get; private set; } = new List<PointF>();
        public List<PointF> BatSpawnPoints { get; private set; } = new List<PointF>();
        public List<PointF> Checkpoints { get; private set; } = new List<PointF>();

        public TileMap(int areaID)
        {
            this.AreaID = areaID;
            BuildKatanaTraversalWorld();
        }

        private void BuildKatanaTraversalWorld()
        {
            // Katana Zero stili kilitli frame: 18 Satır Yükseklik, 60 Sütun Genişlik
            Height = 18;
            Width = 60;

            _collisionLayer = new int[Height, Width];
            _backWallLayer = new int[Height, Width];
            _ruinRiftLayer = new int[Height, Width];
            _foregroundLayer = new int[Height, Width];

            // Mutlak Tavan ve Taban Sınır Koruması (Harita dışına taşmaları engeller)
            for (int col = 0; col < Width; col++)
            {
                _collisionLayer[0, col] = 1;
                _collisionLayer[Height - 1, col] = 1;
            }

            // =========================================================================
            // LİNEER AGRESİF ODA DİZİLİMİ (Her Oda Tam 15 Sütun Genişliktedir)
            // =========================================================================
            if (AreaID == 1)
            {
                // AREA 1: Isınma -> Hızlı Koridor -> Ağır Çatışma -> Çıkış Kapısı
                InjectCombatCell(RoomType.SmallCombatArena, 0);   // Sütun 0 - 15 (Oyuncu Burada Doğar)
                InjectCombatCell(RoomType.DashCorridor, 15);      // Sütun 15 - 30
                InjectCombatCell(RoomType.CombatChallenge, 30);   // Sütun 30 - 45 (RoomManager Kilit Odası)
                InjectCombatCell(RoomType.PrecisionPlatform, 45);  // Sütun 45 - 60 (Checkpoint & Çıkış)

                // Oyuncu başlangıç ve checkpoint noktalarını tam hücre tabanlarına çiviliyoruz
                _collisionLayer[13, 3] = 100;
                _collisionLayer[13, 47] = 300;
            }
            else if (AreaID == 2)
            {
                // AREA 2: Kaleye Giriş -> Dikey Pogo Sınavı -> Koridor -> Çatışma Meydanı
                InjectCombatCell(RoomType.SmallCombatArena, 0);
                InjectCombatCell(RoomType.VerticalPogoArena, 15);
                InjectCombatCell(RoomType.DashCorridor, 30);
                InjectCombatCell(RoomType.CombatChallenge, 45);

                _collisionLayer[13, 32] = 300; // Sahanlık Checkpoint bayrağı
            }
            else if (AreaID == 3)
            {
                // AREA 3: Pogo Uçurumu -> Dikey Geçiş -> Dev Sınır Kilitli Fallen Knight Arenası
                InjectCombatCell(RoomType.PrecisionPlatform, 0);
                InjectCombatCell(RoomType.VerticalPogoArena, 15);

                // 30 - 60 Sütunlar Arası: Jilet gibi düz, engelsiz, saniyede 500 frame stabil Boss Arenası
                for (int col = 30; col < 60; col++) _collisionLayer[14, col] = 1;
                for (int r = 1; r < 14; r++) for (int c = 30; c < 60; c++) _backWallLayer[r, c] = 3;

                _collisionLayer[13, 32] = 300; // Boss kapısı önü Checkpoint'i
                for (int r = 1; r <= 13; r++) _foregroundLayer[r, 34] = 4; // Boss odası sis geçişi
            }

            ExtractMarkers();
        }

        // =========================================================================
        // MİKRO KOMBAT CELL ENJEKTÖRÜ (Kompakt Şablon Matrisi)
        // =========================================================================
        private void InjectCombatCell(RoomType type, int startCol)
        {
            int baseRow = 14; // Tüm hızlı odaların ana zemin hizası satır 14'e kilitli

            // Hücre içi siber arka duvar döşemesi
            for (int c = startCol; c < startCol + 15; c++)
            {
                for (int r = 1; r <= baseRow; r++) _backWallLayer[r, c] = 3;
            }

            switch (type)
            {
                case RoomType.SmallCombatArena:
                    // Geniş, düz ve kesintisiz zemin hattı. Oyuncu hız kesmeden yardırabilir.
                    for (int c = startCol; c < startCol + 15; c++) _collisionLayer[baseRow, c] = 1;

                    // Erişilebilir Düşman Düğümleri
                    _collisionLayer[baseRow - 1, startCol + 6] = 200;  // Düzlük Slime 1
                    _collisionLayer[baseRow - 1, startCol + 11] = 200; // Düzlük Slime 2
                    _collisionLayer[baseRow - 3, startCol + 8] = 201;  // Tam kılıç menzili hizasında Yarasa
                    break;

                case RoomType.VerticalPogoArena:
                    // Duvar tırmanışı yok! Oyuncu yarasaların kafasına basarak (Pogo) yukarı tırmanır
                    for (int c = startCol; c < startCol + 15; c++) _collisionLayer[baseRow, c] = 1;

                    // Havada asılı duran, pogo zamanlamasını ölçen küçük basamaklar
                    _collisionLayer[baseRow - 3, startCol + 3] = 1; _collisionLayer[baseRow - 3, startCol + 4] = 1;
                    _collisionLayer[baseRow - 6, startCol + 7] = 1; _collisionLayer[baseRow - 6, startCol + 8] = 1;
                    _collisionLayer[baseRow - 3, startCol + 11] = 1; _collisionLayer[baseRow - 3, startCol + 12] = 1;

                    // Sıçramayı ve dikey ivmeyi bağlayan erişilebilir yarasalar
                    _collisionLayer[baseRow - 5, startCol + 5] = 201;
                    _collisionLayer[baseRow - 5, startCol + 10] = 201;
                    break;

                case RoomType.DashCorridor:
                    // Alçak tavanlı dar dehliz, refleks atılma (Dash) ve agresif koşu alanı
                    for (int c = startCol; c < startCol + 15; c++)
                    {
                        _collisionLayer[baseRow, c] = 1;
                        _collisionLayer[baseRow - 5, c] = 1; // Başın hemen üstünde katı tavan baskısı
                    }

                    _collisionLayer[baseRow - 1, startCol + 7] = 200;  // Koridor ortası Slime
                    _collisionLayer[baseRow - 2, startCol + 12] = 201; // Dash atarken biçilecek Yarasa
                    break;

                case RoomType.PrecisionPlatform:
                    // Taban tamamen uçurum. Oyuncunun hızı (momentum) sıfırlanırsa doğrudan düşer.
                    _collisionLayer[baseRow, startCol] = 1; _collisionLayer[baseRow, startCol + 1] = 1;
                    _collisionLayer[baseRow, startCol + 13] = 1; _collisionLayer[baseRow, startCol + 14] = 1;

                    // Dash ve zıplama mesafesine (3 tile boşluk) tam uyan tekil akıcı duraklar
                    _collisionLayer[baseRow - 1, startCol + 4] = 1;
                    _collisionLayer[baseRow - 2, startCol + 7] = 1;
                    _collisionLayer[baseRow - 1, startCol + 10] = 1;

                    _collisionLayer[baseRow - 4, startCol + 7] = 201; // Adacık koruyucusu uyanık yarasa
                    break;

                case RoomType.CombatChallenge:
                    // En yoğun refleks odası. Slime ve Yarasaların çapraz ateş hattı.
                    for (int c = startCol; c < startCol + 15; c++) _collisionLayer[baseRow, c] = 1;

                    // Taktiksel bir orta kat iskelesi
                    for (int c = startCol + 4; c <= startCol + 10; c++) _collisionLayer[baseRow - 4, c] = 1;

                    _collisionLayer[baseRow - 1, startCol + 3] = 200;  // Alt sol Slime
                    _collisionLayer[baseRow - 1, startCol + 11] = 200; // Alt sağ Slime
                    _collisionLayer[baseRow - 5, startCol + 7] = 200;  // Üst kat platform Slime'ı
                    _collisionLayer[baseRow - 2, startCol + 5] = 201;  // Hareketi kesmeye çalışan tuzak yarasalar
                    _collisionLayer[baseRow - 2, startCol + 9] = 201;
                    break;
            }
        }

        // ROOM MANAGER ENTEGRASYONU: Odaların dinamik mühürlenmesini sağlar
        public void SetTileCollision(int matrixX, int matrixY, int collisionValue)
        {
            if (matrixX >= 0 && matrixX < Width && matrixY >= 0 && matrixY < Height)
            {
                _collisionLayer[matrixY, matrixX] = collisionValue;
            }
        }

        private void ExtractMarkers()
        {
            // Yeni harita yüklenirken eski koordinat hafızasını jilet gibi temizle
            SlimeSpawnPoints.Clear();
            BatSpawnPoints.Clear();
            Checkpoints.Clear();

            for (int r = 0; r < Height; r++)
            {
                for (int c = 0; c < Width; c++)
                {
                    int token = _collisionLayer[r, c];
                    if (token == 100)
                    {
                        PlayerSpawnPoint = new PointF(c * TileSize, r * TileSize - 8);
                        _collisionLayer[r, c] = 0;
                    }
                    else if (token == 200)
                    {
                        SlimeSpawnPoints.Add(new PointF(c * TileSize, r * TileSize - 16));
                        _collisionLayer[r, c] = 0;
                    }
                    else if (token == 201)
                    {
                        BatSpawnPoints.Add(new PointF(c * TileSize, r * TileSize));
                        _collisionLayer[r, c] = 0;
                    }
                    else if (token == 300)
                    {
                        Checkpoints.Add(new PointF(c * TileSize, r * TileSize - 12));
                        _collisionLayer[r, c] = 0;
                    }
                }
            }
        }

        public bool HasCollision(RectangleF bounds)
        {
            int startX = Math.Max(0, (int)(bounds.Left / TileSize));
            int endX = Math.Min(Width - 1, (int)(bounds.Right / TileSize));
            int startY = Math.Max(0, (int)(bounds.Top / TileSize));
            int endY = Math.Min(Height - 1, (int)(bounds.Bottom / TileSize));

            for (int r = startY; r <= endY; r++)
            {
                for (int c = startX; c <= endX; c++)
                {
                    if (_collisionLayer[r, c] == 1) return true;
                }
            }
            return false;
        }

        public void Render(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.None;

            for (int r = 0; r < Height; r++)
            {
                for (int c = 0; c < Width; c++)
                {
                    float px = c * TileSize;
                    float py = r * TileSize;

                    // Katman 0: Siber-Aksiyon Loş Mağara Duvar Arka Planı
                    if (_backWallLayer[r, c] == 3)
                    {
                        Color backColor = (AreaID == 3) ? Color.FromArgb(10, 8, 14) : ((AreaID == 2) ? Color.FromArgb(14, 12, 18) : Color.FromArgb(15, 16, 22));
                        Color lineColor = (AreaID == 3) ? Color.FromArgb(14, 12, 20) : ((AreaID == 2) ? Color.FromArgb(18, 16, 24) : Color.FromArgb(18, 20, 28));
                        using (var b = new SolidBrush(backColor))
                        using (var p = new Pen(lineColor))
                        {
                            g.FillRectangle(b, px, py, TileSize, TileSize);
                            g.DrawRectangle(p, px, py, TileSize, TileSize);
                        }
                    }

                    // Katman 1: Katı Çarpışma Zeminleri (Keskin Kenarlıklı Jilet Gibi Okunabilir Yapı)
                    if (_collisionLayer[r, c] == 1)
                    {
                        Color stoneColor = (AreaID == 3) ? Color.FromArgb(24, 20, 32) : ((AreaID == 2) ? Color.FromArgb(32, 30, 38) : Color.FromArgb(34, 38, 46));
                        Color edgeColor = (AreaID == 3) ? Color.FromArgb(44, 34, 58) : ((AreaID == 2) ? Color.FromArgb(50, 46, 58) : Color.FromArgb(52, 58, 70));

                        using (var b = new SolidBrush(stoneColor))
                        using (var p = new Pen(edgeColor))
                        {
                            g.FillRectangle(b, px, py, TileSize, TileSize);
                            g.DrawRectangle(p, px, py, TileSize, TileSize);
                        }
                    }

                    // Katman 2: Ön Plan Estetik Gölgeleri (Sinematik Hücre Sınırları)
                    if (_foregroundLayer[r, c] == 4)
                    {
                        using (var b = new SolidBrush(Color.FromArgb(6, 4, 10)))
                        {
                            g.FillRectangle(b, px, py, TileSize, TileSize);
                        }
                    }
                }
            }
        }
    }
}