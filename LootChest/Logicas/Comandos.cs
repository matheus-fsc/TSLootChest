using System;
using Terraria;
using TShockAPI;
using Terraria.ID;

namespace LootChest.Logicas
{
    public class Comandos
    {
        // Método para adicionar um baú
        public static void AddChestCommand(CommandArgs args)
        {
            var player = args.Player;

            if (args.Parameters.Count < 2 ||
                !int.TryParse(args.Parameters[0], out int x) ||
                !int.TryParse(args.Parameters[1], out int y))
            {
                player.SendErrorMessage("Uso: /addchest <X> <Y>");
                return;
            }

            // Verifica se há um baú nas coordenadas
            if (!TileID.Sets.BasicChest[Main.tile[x, y].type])
            {
                player.SendErrorMessage("Não há um baú nessas coordenadas.");
                return;
            }

            if (Main.chest.Any(c => c != null && c.x == x && c.y == y))
            {
                ChestDatabase.SavePlacedChest(x, y);
                player.SendSuccessMessage($"Baú adicionado com sucesso nas coordenadas ({x}, {y}).");
            }
            else
            {
                player.SendErrorMessage("Nenhum baú encontrado nessas coordenadas.");
            }
        }
        public static void ToggleLogChestOpen(CommandArgs args)
        {
            if (args.Parameters.Count != 1 || !bool.TryParse(args.Parameters[0], out bool enable))
            {
                args.Player.SendErrorMessage("Uso: /togglelogchest <true|false>");
                return;
            }

            LootChestPlugin.logChestOpen = enable;
            args.Player.SendSuccessMessage($"Log de abertura de baús {(enable ? "ativado" : "desativado")}.");
        }

        // Método para remover um baú
        public static void RemoveChestCommand(CommandArgs args)
        {
            var player = args.Player;

            if (args.Parameters.Count < 2 ||
                !int.TryParse(args.Parameters[0], out int x) ||
                !int.TryParse(args.Parameters[1], out int y))
            {
                player.SendErrorMessage("Uso: /remchest <X> <Y>");
                return;
            }

            if (ChestDatabase.RemovePlacedChest(x, y))
            {
                player.SendSuccessMessage($"Baú removido com sucesso nas coordenadas ({x}, {y}).");
            }
            else
            {
                player.SendErrorMessage("Nenhum baú encontrado nessas coordenadas ou o baú não foi colocado por um jogador.");
            }
        }
    }
}
