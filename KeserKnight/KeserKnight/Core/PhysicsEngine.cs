using System;
using System.Collections.Generic;
using System.Drawing;
using KeserKnight.Entity;

namespace KeserKnight.Core
{
    public class PhysicsEngine
    {
        // Form1'in sadece bu fonksiyonu çağırması yeterlidir usta
        public void Update(Player player, List<Rectangle> platforms)
        {
            UpdateHorizontalPhysics(player, platforms);
            UpdateVerticalPhysics(player, platforms);
            ApplyScreenLimits(player);
        }

        // 1. YATAY ÇARPIŞMA VE HAREKET HESABI (X EKSENİ)
        private void UpdateHorizontalPhysics(Player player, List<Rectangle> platforms)
        {
            int nextX = player.X;

            if (player.MoveLeft && !player.MoveRight)
            {
                nextX -= player.Speed;
                player.CurrentDirection = Player.Direction.Left;
            }
            else if (player.MoveRight && !player.MoveLeft)
            {
                nextX += player.Speed;
                player.CurrentDirection = Player.Direction.Right;
            }
            else if (player.MoveLeft && player.MoveRight)
            {
                if (player.CurrentDirection == Player.Direction.Left) nextX -= player.Speed;
                else nextX += player.Speed;
            }

            Rectangle futureX = new Rectangle(nextX, player.Y, player.Width, player.Height);
            bool collidesX = false;
            Rectangle hitPlatformX = Rectangle.Empty;

            foreach (var platform in platforms)
            {
                if (futureX.IntersectsWith(platform))
                {
                    collidesX = true;
                    hitPlatformX = platform;
                    break;
                }
            }

            if (!collidesX)
            {
                player.X = nextX;
            }
            else
            {
                // Duvara toslama anında pürüzsüz kilitlenme hizalaması
                if (nextX > player.X) player.X = hitPlatformX.Left - player.Width;
                else if (nextX < player.X) player.X = hitPlatformX.Right;
            }
        }

        // 2. DİKEY ÇARPIŞMA, ZIPLAMA VE YERÇEKİMİ HESABI (Y EKSENİ)
        private void UpdateVerticalPhysics(Player player, List<Rectangle> platforms)
        {
            // Yerçekimi ivmesini uyguluyoruz
            player.VerticalVelocity += player.Gravity;
            int nextY = player.Y + player.VerticalVelocity;

            Rectangle futureY = new Rectangle(player.X, nextY, player.Width, player.Height);
            bool landed = false;

            foreach (var platform in platforms)
            {
                if (futureY.IntersectsWith(platform))
                {
                    // Aşağı düşerken zemine basma kontrolü
                    if (player.VerticalVelocity > 0)
                    {
                        player.Y = platform.Top - player.Height;
                        player.VerticalVelocity = 0;
                        player.IsJumping = false;
                        landed = true;
                        break;
                    }
                    // Yukarı zıplarken kafayı tavan bloğuna vurma kontrolü
                    else if (player.VerticalVelocity < 0)
                    {
                        player.Y = platform.Bottom;
                        player.VerticalVelocity = 0;
                        break;
                    }
                }
            }

            // Eğer havadaysa ve bir yere basmıyorsa zıplama/düşme durumunu aktifleştir
            if (!landed)
            {
                player.Y = nextY;
                player.IsJumping = true;
            }
            else
            {
                player.IsJumping = false;
            }
        }

        // 3. EKRANIN ÜST VE ALT SINIR LIMIT KORUMALARI
        private void ApplyScreenLimits(Player player)
        {
            // Siyah HUD barın (Tavanın) üstüne çıkmasını engelleme kilidi aktiftir
            if (player.Y < 110)
            {
                player.Y = 110;
                player.VerticalVelocity = 0;
            }

            // DİKKAT: Alt sınır korumasını (Y > 1080 kısıtlamasını) sildik! 
            // Böylece oyuncu boşluktan aşağı düşebilecek ve Form1'deki ölüm tetiklenecek.
        }
    }
}
