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
        public static bool logChestOpen = false;
        private Dictionary<(int, int), ChestState> chestStates = new Dictionary<(int, int), ChestState>();
        private readonly object chestLock = new object(); // Objeto para sincronização
        private HashSet<(int, int)> chestsInUse = new HashSet<(int, int)>(); // Baús em uso

        public override string Name => "IndividualLootChests";
        public override Version Version => new Version(2, 0);
        public override string Author => "Matheus Coelho, Adriel Cunha";
        public override string Description => "Baús com loot individual para cada jogador.";

        public LootChestPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            ChestDatabase.InitializeDatabase();
            GetDataHandlers.ChestOpen += OnChestOpen;
            GetDataHandlers.PlaceChest += playerPlaceChest;
            GetDataHandlers.PlaceChest += protectLootChest;
            Commands.ChatCommands.Add(new Command(Comandos.AddChestCommand, "addchest"));
            Commands.ChatCommands.Add(new Command(Comandos.RemoveChestCommand, "remchest"));
            Commands.ChatCommands.Add(new Command(Comandos.ToggleLogChestOpen, "togglelogchest"));

        }

        private void playerPlaceChest(object? sender, PlaceChestEventArgs args)
        {
            // Lógica para quando um baú é colocado ou removido
            TSPlayer player = args.Player;
            int x = args.TileX;
            int y = args.TileY - 1;
            if (args.Flag == 0)
            {
                TShock.Log.Info($"Salvando baú em: X={x}, Y={y}");
                ChestDatabase.SavePlacedChest(x, y);
            }
            else if (args.Flag == 1)
            {
                TShock.Log.Info($"Baú removido em: X={x}, Y={y}");
                ChestDatabase.RemovePlacedChest(x, y);
            }
        }

        void protectLootChest(object? sender, PlaceChestEventArgs args)
        {
            var player = TShock.Players[args.Player.Index];
            bool isProtected = ChestDatabase.IsPlacedByPlayer(args.TileX, args.TileY) ||
                               ChestDatabase.IsPlacedByPlayer(args.TileX + 1, args.TileY) ||
                               ChestDatabase.IsPlacedByPlayer(args.TileX, args.TileY + 1) ||
                               ChestDatabase.IsPlacedByPlayer(args.TileX + 1, args.TileY + 1);

            if (args.Flag == 1 && !isProtected)
            {
                player.SendErrorMessage("Proibido quebrar baús de loot");
                args.Handled = true; // Bloqueia a remoção do baú
            }
        }

        // Verifica se o jogador está perto do baú
        private bool IsPlayerCloseToChest(TSPlayer player, Chest chest)
        {
            float distance = Vector2.Distance(player.TPlayer.position, new Vector2(chest.x * 16, chest.y * 16));
            return distance < 256; // 16 tiles = 256 pixels
        }

        // Verifica se o baú está vazio
        private bool IsChestEmpty(Chest chest)
        {
            return chest.item.All(item => item.IsAir);
        }

        // Monitora a distância do jogador em relação ao baú
        private async Task MonitorPlayerDistance(TSPlayer player, Chest chest)
        {
            try
            {
                while (player.Active && IsPlayerCloseToChest(player, chest))
                {
                    if (IsChestEmpty(chest))
                    {
                        player.SendErrorMessage("Você esvaziou o baú. Permissão bloqueada.");
                        // Bloqueia a permissão do jogador aqui
                        RestoreChest(chest);
                        return;
                    }
                    await Task.Delay(500); // Verifica a cada 0.5 segundo
                }

                // Quando o jogador se afasta, restaure o baú
                RestoreChest(chest);

               
                if (IsChestEmpty(chest))
                {
                    CloseChest(chest);
                }
            }
            catch (Exception ex)
            {
                TShock.Log.Error($"Erro ao monitorar distância do jogador: {ex.Message}");
            }
        }

        private void CloseChest(Chest chest)
        {
            chest.item = new Item[chest.item.Length];
            NetMessage.SendData((int)PacketTypes.ChestItem, -1, -1, null, chest.x, chest.y);
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
            if(logChestOpen)
            TShock.Log.ConsoleInfo($" {args.Player.Name} abriu um baú em X={args.X}, Y={args.Y}.");

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
                GetDataHandlers.PlaceChest -= playerPlaceChest;
                GetDataHandlers.PlaceChest -= protectLootChest;
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