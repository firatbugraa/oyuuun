using System;
using System.Collections.Generic;
using System.Drawing;
using KeserKnight.Entity;

namespace KeserKnight.Combat
{
    public class AttackSystem
    {
        // 1. Saldırı Girdisini Dinleyip Saldırıyı Başlatma
        public void HandleAttackInput(Player player)
        {
            if (!player.IsAttacking)
            {
                player.IsAttacking = true;
                player.AttackTimer = 0;
            }
        }

        // 2. Aktif Saldırının Hitbox Alanını Baktığı Yöne Göre Hesaplama
        public void UpdateAttackHitbox(Player player)
        {
            if (!player.IsAttacking)
            {
                player.AttackHitbox = Rectangle.Empty;
                return;
            }

            // Karakterin baktığı yöne göre 80 piksel genişliğinde bir kürek menzili oluşturuyoruz
            if (player.CurrentDirection == Player.Direction.Left)
            {
                player.AttackHitbox = new Rectangle(player.X - 80, player.Y + 10, 80, player.Height - 20);
            }
            else
            {
                player.AttackHitbox = new Rectangle(player.Right, player.Y + 10, 80, player.Height - 20);
            }
        }

        // 3. Düşmanların Vurulma Durumunu ve Can Kayıplarını Kontrol Etme
        public void CheckEnemyCollisions(Player player, List<Enemy> enemies)
        {
            // Eğer oyuncu saldırmıyorsa veya hitbox henüz oluşmamışsa kontrol etme
            if (!player.IsAttacking || player.AttackHitbox.IsEmpty) return;

            // Listeden eleman silinebileceği için döngüyü tersten (Count - 1 down to 0) işletiyoruz
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];

                if (player.AttackHitbox.IntersectsWith(enemy.Hitbox))
                {
                    // Düşman darbe aldı: Listeden kaldır
                    enemies.RemoveAt(i);

                    // Başarılı vuruş sonrası Shovel Knight geleneği olarak saldırı durumunu kapatıyoruz
                    player.IsAttacking = false;
                    player.AttackHitbox = Rectangle.Empty;
                    break; // Tek karede yalnızca bir düşmana vurma sınırı (isteğe bağlı)
                }
            }
        }
    }
}