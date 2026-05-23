using System;
using System.Collections.Generic;
using System.Drawing;
using KeserKnight.Entity;

namespace KeserKnight.Core
{
    public class PhysicsEngine
    {
        public void Update(Player player, List<Rectangle> platforms)
        {
            // 1. Hızları ve Yerçekimini Güncelle usta
            player.VerticalVelocity += player.Gravity;
            if (player.VerticalVelocity > 25) player.VerticalVelocity = 25; // Terminal Velocity

            // 2. Karakterin gitmek istediği bir sonraki X ve Y koordinatını hesapla
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

            int nextY = player.Y + player.VerticalVelocity;

            // 3. Karakteri doğrudan hedef koordinatına taşı usta (Çift adımdan kurtulduk)
            player.X = nextX;
            player.Y = nextY;

            // Havada kalma durumunu başta aktif et, eğer zemine basarsa aşağıda false yapacağız
            player.IsJumping = true;

            // 4. ÇARPIŞMA ÇÖZÜMLEME MOTORU (RESOLVE COLLISION)
            // Karakter platformun içine girdiyse, onu ışınlamadan milimetrik geri iteceğiz
            foreach (var platform in platforms)
            {
                if (player.Hitbox.IntersectsWith(platform))
                {
                    // Çakışan alanın (Overlap) genişliğini ve yüksekliğini buluyoruz
                    Rectangle overlap = Rectangle.Intersect(player.Hitbox, platform);

                    // Eğer çakışma dikeyde daha sığsa (Y ekseninden düzeltme yap usta)
                    if (overlap.Width > overlap.Height)
                    {
                        if (player.VerticalVelocity >= 0 && player.Y < platform.Top) // Zemine basma
                        {
                            player.Y = platform.Top - player.Height;
                            player.VerticalVelocity = 0;
                            player.IsJumping = false;
                        }
                        else if (player.VerticalVelocity < 0 && player.Y > platform.Top) // Kafayı tavana vurma
                        {
                            player.Y = platform.Bottom + 1;
                            player.VerticalVelocity = 0;
                        }
                    }
                    // Eğer çakışma yatayda daha sığsa (X ekseninden duvara it usta - IŞINLANMAYI BİTİREN KISIM)
                    else
                    {
                        if (player.X < platform.X) // Soldan duvara çarpma
                        {
                            player.X = platform.Left - player.Width - 1;
                        }
                        else // Sağdan duvara çarpma
                        {
                            player.X = platform.Right + 1;
                        }
                    }
                }
            }

            // 5. EKRANIN ÜST SINIR LİMİT KORUMASI
            ApplyScreenLimits(player);
        }

        private void ApplyScreenLimits(Player player)
        {
            if (player.Y < 110)
            {
                player.Y = 110;
                player.VerticalVelocity = 0;
            }
        }
    }
}