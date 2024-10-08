using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static TShockAPI.GetDataHandlers;

namespace LootChest.Logicas
{
    [ApiVersion(2, 1)]
    public class LootChestPlugin : TerrariaPlugin
    {
        public override string Name => "IndividualLootChests";
        public override Version Version => new Version(1, 2);
        public override string Author => "Matheus Coelho";
        public override string Description => "Baús com loot individual para cada jogador.";

        private Dictionary<(int, int), ChestState> chestStates = new Dictionary<(int, int), ChestState>();
        private readonly object chestLock = new object(); // Objeto para sincronização
        private HashSet<(int, int)> chestsInUse = new HashSet<(int, int)>(); // Baús em uso

        public LootChestPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ChestDatabase.InitializeDatabase();
            GetDataHandlers.ChestOpen += OnChestOpen;
            GetDataHandlers.PlaceChest += HandlePlaceChest;
            Commands.ChatCommands.Add(new Command(AddChestCommand, "addchest"));
            Commands.ChatCommands.Add(new Command(RemoveChestCommand, "remchest"));

        }

        


        private void HandlePlaceChest(object? sender, PlaceChestEventArgs args)
        {
            // Lógica para quando um baú é colocado ou removido
            TSPlayer player = args.Player;
            int x = args.TileX;
            int y = args.TileY - 1;
            int flag = args.Flag;
            TShock.Log.Info($"Flag: {flag}");
            if (flag == 0)
            {
                TShock.Log.Info($"Baú colocado em: X={x}, Y={y}");
                ChestDatabase.SavePlacedChest(x, y);
            }
            else if (flag == 1)
            {
                TShock.Log.Info($"Baú removido em: X={x}, Y={y}");
                ChestDatabase.RemovePlacedChest(x, y);
            }
        }

        // Método para adicionar um baú
        public void AddChestCommand(CommandArgs args)
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

        // Método para remover um baú
        public void RemoveChestCommand(CommandArgs args)
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

        // Verifica se o jogador está perto do baú
        private bool IsPlayerCloseToChest(TSPlayer player, Chest chest)
        {
            float distance = Vector2.Distance(player.TPlayer.position, new Vector2(chest.x * 16, chest.y * 16));
            return distance < 256; // 16 tiles = 256 pixels
        }

        // Monitora a distância do jogador em relação ao baú
        private async Task MonitorPlayerDistance(TSPlayer player, Chest chest)
        {
            try
            {
                while (player.Active && IsPlayerCloseToChest(player, chest))
                {
                    await Task.Delay(1000); // Verifica a cada 1 segundo
                }

                // Quando o jogador se afasta, restaure o baú
                RestoreChest(chest);
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Erro ao monitorar distância do jogador: {ex.Message}");
            }
        }

        public class ChestState
        {
            public int[] OriginalItemIDs; // IDs originais dos itens do baú
            public int[] OriginalItemQuantities; // Quantidades originais dos itens do baú

            public ChestState(Item[] items)
            {
                OriginalItemIDs = items.Select(item => item.type).ToArray(); // Armazena os IDs dos itens
                OriginalItemQuantities = items.Select(item => item.stack).ToArray(); // Armazena as quantidades dos itens
            }
        }

        private void OnChestOpen(object? sender, GetDataHandlers.ChestOpenEventArgs args)
        {
            TShock.Log.Info($"Baú aberto em: X={args.X}, Y={args.Y}");

            var player = TShock.Players[args.Player.Index];

            if (player == null || !player.Active)
            {
                return;
            }

            var chest = Main.chest.FirstOrDefault(c => c != null && c.x == args.X && c.y == args.Y);

            if (chest == null)
            {
                return;
            }

            // Verifica se o baú foi colocado pelo jogador
            if (ChestDatabase.IsPlacedByPlayer(chest.x, chest.y))
            {
                return;
            }

            lock (chestLock)
            {
                // Verifica se o baú está em uso
                if (chestsInUse.Contains((chest.x, chest.y)))
                {
                    player.SendErrorMessage("Este baú está sendo usado por outro jogador.");
                    args.Handled = true;
                    return;
                }

                // Marca o baú como em uso
                chestsInUse.Add((chest.x, chest.y));
            }

            // Verifica se o jogador já abriu o baú
            if (ChestDatabase.HasPlayerOpenedChest(player.Name, chest.x, chest.y))
            {
                player.SendErrorMessage("Você já abriu este baú.");
                args.Handled = true;
                lock (chestLock)
                {
                    chestsInUse.Remove((chest.x, chest.y));
                }
                return;
            }

            // Armazenar o estado original do baú
            lock (chestLock)
            {
                if (!chestStates.ContainsKey((chest.x, chest.y)))
                {
                    chestStates[(chest.x, chest.y)] = new ChestState(chest.item);
                }
            }

            player.SendSuccessMessage("Você abriu o baú de loot! Não coloque nada no baú, pois tudo será deletado e perdido.");
            ChestDatabase.MarkChestOpenedByPlayer(player.Name, chest.x, chest.y);

            _ = MonitorPlayerDistance(player, chest);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GetDataHandlers.ChestOpen -= OnChestOpen;
                GetDataHandlers.PlaceChest -= HandlePlaceChest;

            }
            base.Dispose(disposing);
        }

        private void RestoreChest(Chest chest)
        {
            lock (chestLock)
            {
                if (chestStates.TryGetValue((chest.x, chest.y), out ChestState? chestState))
                {
                    for (int i = 0; i < chest.item.Length; i++)
                        chest.item[i] = new Item();

                    for (int i = 0; i < chestState.OriginalItemIDs.Length; i++)
                    {
                        Item newItem = new Item();
                        newItem.SetDefaults(chestState.OriginalItemIDs[i]);
                        newItem.stack = chestState.OriginalItemQuantities[i];
                        chest.item[i] = newItem;
                    }

                    chestStates.Remove((chest.x, chest.y));
                    chestsInUse.Remove((chest.x, chest.y)); // Marca o baú como não mais em uso
                }
            }
        }
    }
}
