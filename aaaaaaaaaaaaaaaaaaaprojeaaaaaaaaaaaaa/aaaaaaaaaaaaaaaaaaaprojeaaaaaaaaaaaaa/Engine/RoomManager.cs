using System;
using System.Collections.Generic;
using System.Drawing;
using oyun1.Entities;
using oyun1.Levels;

namespace oyun1.Engine
{
    public enum RoomState { Idle, Locked, Cleared }

    public class CombatRoom
    {
        public int RoomID { get; set; }
        public RectangleF TriggerBounds { get; set; }
        public List<Point> GateTiles { get; set; }
        public RoomState State { get; set; } = RoomState.Idle;
        public List<Enemy> TrackedEnemies { get; set; } = new List<Enemy>();
    }

    public class RoomManager
    {
        private readonly List<CombatRoom> _activeRooms = new List<CombatRoom>();
        private readonly ParticleSystem _pSystem;
        private TileMap _tileMap;

        public RoomManager(ParticleSystem pSystem)
        {
            _pSystem = pSystem;
        }

        public void SetupRoomsForArea(TileMap tileMap, List<Enemy> areaEnemies)
        {
            _tileMap = tileMap;
            _activeRooms.Clear();

            if (tileMap.AreaID == 1)
            {
                // AREA 1: 25. ve 42. sütunlar arasındaki Crystal Arena
                CombatRoom crystalArena = new CombatRoom
                {
                    RoomID = 1,
                    // Oyuncu 25. sütuna adım attığı an kapılar mühürlenir
                    TriggerBounds = new RectangleF(25 * TileMap.TileSize, 1 * TileMap.TileSize, 17 * TileMap.TileSize, 16 * TileMap.TileSize),
                    GateTiles = new List<Point>
                    {
                        // Giriş Kapısı Duvarı (24. Sütun - Yukarıdan Aşağıya Tam Boy Mühür)
                        new Point(24, 11), new Point(24, 12), new Point(24, 13), new Point(24, 14),
                        // Çıkış Kapısı Duvarı (43. Sütun - Yukarıdan Aşağıya Tam Boy Mühür)
                        new Point(43, 10), new Point(43, 11), new Point(43, 12), new Point(43, 13)
                    }
                };

                RegisterEnemiesToRoom(crystalArena, areaEnemies);
                _activeRooms.Add(crystalArena);
            }
            else if (tileMap.AreaID == 2)
            {
                // AREA 2: 32. ve 48. sütunlar arasındaki Kale Kışlası
                CombatRoom ruinGarrison = new CombatRoom
                {
                    RoomID = 2,
                    TriggerBounds = new RectangleF(32 * TileMap.TileSize, 1 * TileMap.TileSize, 16 * TileMap.TileSize, 16 * TileMap.TileSize),
                    GateTiles = new List<Point>
                    {
                        // Sol Giriş Kapısı Tam Boy Duvar Kilidi
                        new Point(31, 11), new Point(31, 12), new Point(31, 13), new Point(31, 14),
                        // Sağ Çıkış Kapısı Tam Boy Duvar Kilidi
                        new Point(49, 11), new Point(49, 12), new Point(49, 13), new Point(49, 14)
                    }
                };

                RegisterEnemiesToRoom(ruinGarrison, areaEnemies);
                _activeRooms.Add(ruinGarrison);
            }
        }

        private void RegisterEnemiesToRoom(CombatRoom room, List<Enemy> areaEnemies)
        {
            foreach (var enemy in areaEnemies)
            {
                // Düşmanın merkez noktasının oda sınırları içinde olup olmadığını kontrol et
                PointF enemyCenter = new PointF(enemy.Position.X + enemy.Size.Width / 2, enemy.Position.Y + enemy.Size.Height / 2);
                if (room.TriggerBounds.Contains(enemyCenter))
                {
                    room.TrackedEnemies.Add(enemy);
                }
            }
        }

        public void Update(RectangleF playerBounds)
        {
            foreach (var room in _activeRooms)
            {
                if (room.State == RoomState.Cleared) continue;

                // 1. ODAYA GİRİŞ: Oyuncu alana girdiğinde kapıları fiziksel olarak kapat
                if (room.State == RoomState.Idle && playerBounds.IntersectsWith(room.TriggerBounds))
                {
                    LockRoom(room);
                }

                // 2. DÜŞMAN KONTROLÜ: Odadaki tüm düşmanlar öldü mü?
                if (room.State == RoomState.Locked)
                {
                    room.TrackedEnemies.RemoveAll(e => e.CurrentState == EnemyState.Dead);

                    if (room.TrackedEnemies.Count == 0)
                    {
                        UnlockRoom(room);
                    }
                }
            }
        }

        private void LockRoom(CombatRoom room)
        {
            room.State = RoomState.Locked;

            // Kapı koordinatlarını katı duvara (1) dönüştürerek geçişi fiziksel olarak engelle
            foreach (var tile in room.GateTiles)
            {
                _tileMap.SetTileCollision(tile.X, tile.Y, 1);
            }
        }

        private void UnlockRoom(CombatRoom room)
        {
            room.State = RoomState.Cleared;

            // Tüm düşmanlar öldüğünde kapıları aç (0: Hava yap)
            foreach (var tile in room.GateTiles)
            {
                _tileMap.SetTileCollision(tile.X, tile.Y, 0);

                // Görsel geri bildirim şok dalgası efektleri
                _pSystem.SpawnCombatBurst(tile.X * TileMap.TileSize + 16, tile.Y * TileMap.TileSize + 16, 1, 1, Color.Cyan, ParticleType.Shockwave);
                _pSystem.SpawnCombatBurst(tile.X * TileMap.TileSize + 16, tile.Y * TileMap.TileSize + 16, -1, 2, Color.White, ParticleType.Shard);
            }
        }
    }
}