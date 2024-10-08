using System;
using Microsoft.Data.Sqlite;
using TShockAPI;

namespace LootChest.Logicas
{
    public class ChestDatabase
    {
        private static string dbPath = "chests.sqlite";

        public static void InitializeDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS PlayerChests (
                            ChestID INTEGER PRIMARY KEY AUTOINCREMENT,
                            PlayerID TEXT,
                            ChestX INT,
                            ChestY INT,
                            UNIQUE(PlayerID, ChestX, ChestY)
                        );
                    ";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS PlacedChests (
                            ChestX INT,
                            ChestY INT,
                            PRIMARY KEY(ChestX, ChestY)
                        );
                    ";
                    command.ExecuteNonQuery();

                    TShock.Log.ConsoleInfo("Banco de dados ainda não existe, criando um!");
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Erro ao inicializar o banco de dados: {ex.Message}");
            }
        }

        public static void SavePlacedChest(int x, int y)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT OR IGNORE INTO PlacedChests (ChestX, ChestY) VALUES (@x, @y);";
                    command.Parameters.AddWithValue("@x", x);
                    command.Parameters.AddWithValue("@y", y);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Erro ao salvar o baú: {ex.Message}");
            }
        }

        public static bool IsPlacedByPlayer(int x, int y)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM PlacedChests WHERE ChestX = @x AND ChestY = @y;";
                    command.Parameters.AddWithValue("@x", x);
                    command.Parameters.AddWithValue("@y", y);
                    return (long)command.ExecuteScalar() > 0;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Erro ao verificar se o baú foi colocado pelo jogador: {ex.Message}");
                return false;
            }
        }

        public static bool HasPlayerOpenedChest(string playerId, int x, int y)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM PlayerChests WHERE PlayerID = @playerId AND ChestX = @x AND ChestY = @y;";
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.Parameters.AddWithValue("@x", x);
                    command.Parameters.AddWithValue("@y", y);
                    return (long)command.ExecuteScalar() > 0;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Erro ao verificar se o jogador abriu o baú: {ex.Message}");
                return false;
            }
        }

        public static void MarkChestOpenedByPlayer(string playerId, int x, int y)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT OR IGNORE INTO PlayerChests (PlayerID, ChestX, ChestY) VALUES (@playerId, @x, @y);";
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.Parameters.AddWithValue("@x", x);
                    command.Parameters.AddWithValue("@y", y);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Erro ao marcar o baú como aberto pelo jogador: {ex.Message}");
            }
        }

        public static bool RemovePlacedChest(int x, int y)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={dbPath};"))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM PlacedChests WHERE ChestX = @x AND ChestY = @y;";
                    command.Parameters.AddWithValue("@x", x);
                    command.Parameters.AddWithValue("@y", y);
                    int rowsAffected = command.ExecuteNonQuery();

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError($"Erro ao remover o baú: {ex.Message}");
                return false;
            }
        }
    }
}
