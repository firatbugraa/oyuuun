using System;
using System.Drawing;
using oyun1.Levels;

namespace oyun1.Engine
{
    public class WorldManager
    {
        public TileMap CurrentMap { get; private set; }

        // Kayıt noktası kalıcılığı (Kritik Gereksinim)
        public int ActiveCheckpointAreaID { get; set; } = 1;
        public PointF GlobalCheckpointPosition { get; set; }
        private bool _hasSavedCheckpoint = false;

        public WorldManager()
        {
            // İlk açılışta Area 1 dünyasını yükle
            LoadArea(1);
        }

        public void LoadArea(int areaID)
        {
            CurrentMap = new TileMap(areaID);

            // Eğer bu haritada ilk kez doğuluyorsa ve global kayıt yoksa haritanın kendi spawn noktasını baz al
            if (!_hasSavedCheckpoint && areaID == 1)
            {
                GlobalCheckpointPosition = CurrentMap.PlayerSpawnPoint;
                ActiveCheckpointAreaID = 1;
                _hasSavedCheckpoint = true;
            }
        }

        // Harita sınır taşmalarını izleyen ve geçiş kapılarını tetikleyen motor
        public int CheckMapTransitions(PointF playerPos, out PointF targetSpawnPos)
        {
            targetSpawnPos = PointF.Empty;

            // KAPILARIN MAP BOUNDARY MANEVRASI:
            // Oyuncu 60. sütundan sağa taşarsa (WidthInPixels) sonraki haritaya geç
            if (playerPos.X > CurrentMap.WidthInPixels - 16f)
            {
                if (CurrentMap.AreaID == 1) // Area 1 -> Area 2 Geçişi
                {
                    targetSpawnPos = new PointF(32f, playerPos.Y); // Area 2'nin en solunda doğsun
                    return 2;
                }
                else if (CurrentMap.AreaID == 2) // Area 2 -> Area 3 Geçişi
                {
                    targetSpawnPos = new PointF(32f, playerPos.Y);
                    return 3;
                }
            }
            // Oyuncu 0. sütundan sola taşarsa önceki haritaya geri dön
            else if (playerPos.X < 8f)
            {
                if (CurrentMap.AreaID == 3) // Area 3 -> Area 2 Dönüşü
                {
                    targetSpawnPos = new PointF(CurrentMap.WidthInPixels - 64f, playerPos.Y);
                    return 2;
                }
                else if (CurrentMap.AreaID == 2) // Area 2 -> Area 1 Dönüşü
                {
                    targetSpawnPos = new PointF(CurrentMap.WidthInPixels - 64f, playerPos.Y);
                    return 1;
                }
            }

            return -1; // Geçiş tetiklenmedi
        }
    }
}