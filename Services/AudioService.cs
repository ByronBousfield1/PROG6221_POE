using System;
using System.IO;
using System.Media;

namespace CyberAwarenessBot.Services
{
    public static class AudioService
    {
        public static void PlayGreeting(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                using var player = new SoundPlayer(filePath);
                player.Play();
            }
            catch (Exception)
            {
            }
        }
    }
}
